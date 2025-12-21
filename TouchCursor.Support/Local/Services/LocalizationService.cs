using System.IO;
using System.Text.Json;

namespace TouchCursor.Support.Local.Services;

public class LocalizationService
{
    private static LocalizationService? _instance;
    private Dictionary<string, string> _strings = new();
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

        LanguageChanged?.Invoke();
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
        return defaultValue ?? key;
    }

    public string this[string key] => Get(key);
}
