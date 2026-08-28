using System.ComponentModel;
using System.IO;
using System.Text.Json;

namespace TouchCursor.Support.Local.Services;

public class LocalizationService
{
    private static LocalizationService? _instance;
    private Dictionary<string, string> _strings = new();
    private readonly Dictionary<string, string> _defaults = new();
    private readonly List<WeakReference<LocalizedString>> _bindables = new();
    private string _currentLanguage = "en";

    public static LocalizationService Instance => _instance ??= new LocalizationService();

    public string CurrentLanguage => _currentLanguage;

    public event Action? LanguageChanged;

    private LocalizationService()
    {
        LoadLanguage("en");
    }

    public void LoadLanguage(string languageCode)
    {
        _currentLanguage = languageCode;

        var resourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", $"Strings.{languageCode}.json");

        if (!File.Exists(resourcePath))
        {
            // Fallback to English
            resourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Strings.en.json");
            _currentLanguage = "en";
        }

        if (File.Exists(resourcePath))
        {
            try
            {
                var json = File.ReadAllText(resourcePath);
                using var doc = JsonDocument.Parse(json);
                _strings = FlattenJson(doc.RootElement, "");
            }
            catch
            {
                _strings = new Dictionary<string, string>();
            }
        }

        NotifyBindables();
        LanguageChanged?.Invoke();
    }

    /// <summary>
    /// XAML 바인딩용 문자열 소스를 만든다. 언어가 바뀌면 자동으로 갱신된다.
    /// </summary>
    public LocalizedString CreateBindable(string key, string? defaultValue)
    {
        if (!string.IsNullOrEmpty(defaultValue))
            _defaults[key] = defaultValue!;

        var bindable = new LocalizedString(key, defaultValue);
        _bindables.Add(new WeakReference<LocalizedString>(bindable));
        return bindable;
    }

    /// <summary>살아있는 바인딩 소스만 갱신하고 수거된 것은 목록에서 제거한다.</summary>
    private void NotifyBindables()
    {
        for (var i = _bindables.Count - 1; i >= 0; i--)
        {
            if (_bindables[i].TryGetTarget(out var bindable))
                bindable.Refresh();
            else
                _bindables.RemoveAt(i);
        }
    }

    private Dictionary<string, string> FlattenJson(JsonElement element, string prefix)
    {
        var result = new Dictionary<string, string>();

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

                if (prop.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var nested in FlattenJson(prop.Value, key))
                    {
                        result[nested.Key] = nested.Value;
                    }
                }
                else if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    result[key] = prop.Value.GetString() ?? "";
                }
            }
        }

        return result;
    }

    public string Get(string key, string? defaultValue = null)
    {
        if (_strings.TryGetValue(key, out var value))
        {
            return value;
        }
        if (!string.IsNullOrEmpty(defaultValue))
        {
            return defaultValue!;
        }
        // XAML에서 한 번이라도 Default를 준 적이 있으면 그것을 쓴다.
        return _defaults.TryGetValue(key, out var fallback) ? fallback : key;
    }

    public string this[string key] => Get(key);
}

/// <summary>
/// 하나의 리소스 키에 대한 바인딩 가능한 문자열. 언어 변경 시 <see cref="Value"/>가 갱신된다.
/// </summary>
public sealed class LocalizedString : INotifyPropertyChanged
{
    private readonly string _key;
    private readonly string? _default;

    internal LocalizedString(string key, string? defaultValue)
    {
        _key = key;
        _default = defaultValue;
    }

    public string Value => LocalizationService.Instance.Get(_key, _default);

    internal void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));

    public event PropertyChangedEventHandler? PropertyChanged;
}
