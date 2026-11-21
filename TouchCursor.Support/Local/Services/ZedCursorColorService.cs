using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Threading;

namespace TouchCursor.Support.Local.Services;

/// <summary>
/// Zed 가 앞에 있을 때 한/영 상태에 따라 Zed 커서 색을 바꾼다.
/// Zed 는 IME 상태로 커서 색을 바꾸는 기능이 없어서, settings.json 의
/// "experimental.theme_overrides" → players[0].cursor 를 고쳐 쓰고 Zed 가 다시 읽게 한다.
/// 상태가 바뀔 때만 쓴다. settings.json 이 심볼릭 링크(dotfiles 동기화)이거나 주석이 있으면 건드리지 않는다.
/// </summary>
public sealed class ZedCursorColorService : IDisposable
{
    private const string OverridesKey = "experimental.theme_overrides";
    private const string ZedProcessName = "zed";

    private static readonly JsonDocumentOptions ReadOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly Func<bool> _isKoreanIme;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private IntPtr _lastForeground;
    private bool _lastForegroundIsZed;
    private bool? _lastKorean;

    public ZedCursorColorService(Func<bool> isKoreanIme)
    {
        _isKoreanIme = isKoreanIme;
        _timer.Tick += (_, _) => Poll();
    }

    public string EnglishColor { get; set; } = "#7ee787";
    public string KoreanColor { get; set; } = "#ff9696";

    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zed", "settings.json");

    public void Start()
    {
        _lastKorean = null; // 다시 켜면 처음 한 번은 꼭 쓴다.
        _timer.Start();
    }

    /// <summary>멈추고 우리가 넣은 커서 색을 settings.json 에서 뺀다.</summary>
    public void Stop()
    {
        _timer.Stop();
        _lastKorean = null;
        TryEditSettings(RemoveCursorColor);
    }

    public void Dispose() => _timer.Stop();

    private void Poll()
    {
        if (!IsZedForeground())
            return;

        var korean = _isKoreanIme();
        if (korean == _lastKorean)
            return;

        var color = korean ? KoreanColor : EnglishColor;
        if (TryEditSettings(root => SetCursorColor(root, color)))
            _lastKorean = korean;
    }

    private bool IsZedForeground()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == _lastForeground)
            return _lastForegroundIsZed;

        _lastForeground = hwnd;
        _lastForegroundIsZed = false;
        try
        {
            GetWindowThreadProcessId(hwnd, out var processId);
            if (processId != 0)
            {
                using var process = Process.GetProcessById((int)processId);
                _lastForegroundIsZed = string.Equals(process.ProcessName, ZedProcessName, StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // 프로세스를 못 읽으면 Zed 가 아닌 것으로 본다.
        }
        return _lastForegroundIsZed;
    }

    // ── settings.json 고치기 ─────────────────────────────

    /// <summary>바꿨거나 바꿀 게 없으면 true. 건드리면 안 되는 파일이거나 실패하면 false.</summary>
    private static bool TryEditSettings(Func<JsonObject, bool> edit)
    {
        try
        {
            var path = SettingsPath;
            var file = new FileInfo(path);
            if (file.LinkTarget is not null)
                return false; // dotfiles 로 링크된 파일은 바꿀 때마다 동기화되므로 손대지 않는다.

            var text = file.Exists ? File.ReadAllText(path) : "";
            if (string.IsNullOrWhiteSpace(text))
                text = "{}";
            if (HasComments(text))
                return false; // 다시 쓰면 주석이 사라진다.

            if (JsonNode.Parse(text, documentOptions: ReadOptions) is not JsonObject root)
                return false;

            if (!edit(root))
                return true;

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, root.ToJsonString(WriteOptions) + Environment.NewLine, new UTF8Encoding(false));
            return true;
        }
        catch
        {
            return false; // Zed 가 파일을 잡고 있거나 JSON 이 깨졌으면 다음 기회에 다시 한다.
        }
    }

    private static bool SetCursorColor(JsonObject root, string color)
    {
        if (root[OverridesKey] is not JsonObject overrides)
        {
            overrides = new JsonObject();
            root[OverridesKey] = overrides;
        }
        if (overrides["players"] is not JsonArray players)
        {
            players = new JsonArray();
            overrides["players"] = players;
        }
        if (players.Count == 0 || players[0] is not JsonObject first)
        {
            first = new JsonObject();
            if (players.Count == 0) players.Add(first);
            else players[0] = first;
        }

        if (first["cursor"] is JsonValue current && current.TryGetValue<string>(out var existing)
            && string.Equals(existing, color, StringComparison.OrdinalIgnoreCase))
            return false;

        first["cursor"] = color;
        return true;
    }

    private static bool RemoveCursorColor(JsonObject root)
    {
        if (root[OverridesKey] is not JsonObject overrides
            || overrides["players"] is not JsonArray players
            || players.Count == 0
            || players[0] is not JsonObject first
            || !first.Remove("cursor"))
            return false;

        // 우리가 만든 빈 껍데기는 같이 치운다.
        if (first.Count == 0 && players.Count == 1)
            overrides.Remove("players");
        if (overrides.Count == 0)
            root.Remove(OverridesKey);
        return true;
    }

    private static bool HasComments(string text)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(text), new JsonReaderOptions
        {
            CommentHandling = JsonCommentHandling.Allow,
            AllowTrailingCommas = true,
        });
        try
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Comment)
                    return true;
            }
            return false;
        }
        catch (JsonException)
        {
            return true; // 못 읽는 파일도 건드리지 않는다.
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
