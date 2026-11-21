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
/// Zed, WezTerm 이 앞에 있을 때 한/영 상태에 따라 커서 색을 바꾼다. 둘 다 IME 상태로 커서 색을 바꾸는 기능이 없다.
/// - Zed: settings.json 의 "experimental.theme_overrides" → players[0].cursor 를 고쳐 쓰면 Zed 가 다시 읽는다.
///   settings.json 이 심볼릭 링크(dotfiles 동기화)이거나 주석이 있으면 건드리지 않는다.
/// - WezTerm: %APPDATA%\TouchCursor\wezterm-cursor.txt 에 색을 쓴다. ~/.wezterm.lua 가 이 파일을 읽어
///   colors.cursor_bg 로 쓰고 감시 목록에 넣어 두므로, 파일이 바뀌면 설정을 다시 읽는다.
/// 앱마다 상태가 바뀔 때만 쓴다.
/// </summary>
public sealed class ImeCursorColorService : IDisposable
{
    private const string OverridesKey = "experimental.theme_overrides";
    private const string ZedProcessName = "zed";
    private const string WezTermProcessName = "wezterm-gui";

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
    private string? _lastForegroundName;
    private bool? _lastZedKorean;
    private bool? _lastWezTermKorean;

    public ImeCursorColorService(Func<bool> isKoreanIme)
    {
        _isKoreanIme = isKoreanIme;
        _timer.Tick += (_, _) => Poll();
    }

    public string EnglishColor { get; set; } = "#7ee787";
    public string KoreanColor { get; set; } = "#ff9696";

    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zed", "settings.json");

    public static string WezTermColorPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TouchCursor", "wezterm-cursor.txt");

    public void Start()
    {
        // 다시 켜면 처음 한 번은 꼭 쓴다.
        _lastZedKorean = null;
        _lastWezTermKorean = null;
        _timer.Start();
    }

    /// <summary>멈추고 우리가 넣은 커서 색을 지운다(Zed settings.json 항목, WezTerm 색 파일).</summary>
    public void Stop()
    {
        _timer.Stop();
        _lastZedKorean = null;
        _lastWezTermKorean = null;
        TryEditSettings(RemoveCursorColor);
        // 지우지 않고 비운다: WezTerm 감시가 끊기지 않고, 빈 파일이면 원래 커서 색으로 돌아간다.
        try { if (File.Exists(WezTermColorPath)) File.WriteAllText(WezTermColorPath, ""); } catch { /* 다음에 켤 때 다시 쓴다 */ }
    }

    public void Dispose() => _timer.Stop();

    private void Poll()
    {
        var name = CachedForegroundName();
        if (string.Equals(name, ZedProcessName, StringComparison.OrdinalIgnoreCase))
        {
            var korean = _isKoreanIme();
            if (korean != _lastZedKorean && TryEditSettings(root => SetCursorColor(root, ColorFor(korean))))
                _lastZedKorean = korean;
        }
        else if (string.Equals(name, WezTermProcessName, StringComparison.OrdinalIgnoreCase))
        {
            var korean = _isKoreanIme();
            if (korean != _lastWezTermKorean && TryWriteWezTermColor(ColorFor(korean)))
                _lastWezTermKorean = korean;
        }
    }

    private string ColorFor(bool korean) => korean ? KoreanColor : EnglishColor;

    private string? CachedForegroundName()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd != _lastForeground)
        {
            _lastForeground = hwnd;
            _lastForegroundName = ProcessNameOf(hwnd);
        }
        return _lastForegroundName;
    }

    /// <summary>지금 앞에 있는 창의 프로세스 이름(확장자 없이). 못 읽으면 null.</summary>
    public static string? ForegroundProcessName() => ProcessNameOf(GetForegroundWindow());

    private static bool TryWriteWezTermColor(string color)
    {
        try
        {
            var path = WezTermColorPath;
            if (File.Exists(path) && string.Equals(File.ReadAllText(path).Trim(), color, StringComparison.OrdinalIgnoreCase))
                return true;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, color, new UTF8Encoding(false));
            return true;
        }
        catch
        {
            return false; // WezTerm 이 읽는 중이면 다음 기회에 다시 쓴다.
        }
    }

    private static string? ProcessNameOf(IntPtr hwnd)
    {
        try
        {
            GetWindowThreadProcessId(hwnd, out var processId);
            if (processId == 0)
                return null;
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch
        {
            return null; // 프로세스를 못 읽으면 대상이 아닌 것으로 본다.
        }
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
