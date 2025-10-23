// Copyright © 2025. Ported to C# from original C++ TouchCursor by Martin Stone.
// Original project licensed under GNU GPL v3.

using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace touch_cursor.Services;

public class LocalizationManager
{
    private static LocalizationManager? _instance;
    private Dictionary<string, object> _strings = new();
    private string _currentLanguage = "en";

    public static LocalizationManager Instance => _instance ??= new LocalizationManager();

    public event Action? LanguageChanged;

    private LocalizationManager()
    {
        // Default to system language or English
        var systemLanguage = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        LoadLanguage(systemLanguage);
    }

    public string CurrentLanguage => _currentLanguage;

    public void LoadLanguage(string languageCode)
    {
        try
        {
            string? json = null;

            // Try to load from embedded resource first
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"touch_cursor.Resources.Strings.{languageCode}.json";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    json = reader.ReadToEnd();
                }
            }
            catch
            {
                // Continue to file system fallback
            }

            // Fallback to file system if embedded resource not found
            if (json == null)
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var resourcePath = Path.Combine(baseDir, "Resources", $"Strings.{languageCode}.json");

                if (File.Exists(resourcePath))
                {
                    json = File.ReadAllText(resourcePath);
                }
                else
                {
                    // If specific language not found, try English
                    if (languageCode != "en")
                    {
                        LoadLanguage("en");
                        return;
                    }

                    // If English also not found, create default
                    CreateDefaultResources();
                    return;
                }
            }

            _strings = JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                       ?? new Dictionary<string, object>();
            _currentLanguage = languageCode;

            LanguageChanged?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load language: {ex.Message}");

            // Fallback to English on error
            if (languageCode != "en")
            {
                LoadLanguage("en");
            }
            else
            {
                CreateDefaultResources();
            }
        }
    }

    private void CreateDefaultResources()
    {
        // Create minimal default strings to prevent app from breaking
        _strings = new Dictionary<string, object>
        {
            ["AppTitle"] = "TouchCursor",
            ["MainWindow"] = new Dictionary<string, object>
            {
                ["Title"] = "TouchCursor",
                ["EnableTouchCursor"] = "Enable TouchCursor",
                ["TrainingMode"] = "Training Mode",
                ["RunAtStartup"] = "Run at Startup",
                ["QuickInfo"] = "Quick Info",
                ["Settings"] = "Settings",
                ["MinimizeToTray"] = "Minimize to Tray"
            }
        };
        _currentLanguage = "en";

        // Try to copy resource files to the output directory
        EnsureResourceFiles();
    }

    private void EnsureResourceFiles()
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var resourceDir = Path.Combine(baseDir, "Resources");

            if (!Directory.Exists(resourceDir))
            {
                Directory.CreateDirectory(resourceDir);
            }

            // Try to extract embedded resources to file system
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = new[] { "en", "ko" };

            foreach (var lang in resourceNames)
            {
                var resourceName = $"touch_cursor.Resources.Strings.{lang}.json";
                var outputPath = Path.Combine(resourceDir, $"Strings.{lang}.json");

                if (!File.Exists(outputPath))
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        using var fileStream = File.Create(outputPath);
                        stream.CopyTo(fileStream);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to ensure resource files: {ex.Message}");
        }
    }

    public string GetString(string key)
    {
        var keys = key.Split('.');
        object? current = _strings;

        foreach (var k in keys)
        {
            if (current is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue(k, out var value))
                {
                    current = value;
                }
                else
                {
                    return $"[{key}]"; // Return key in brackets if not found
                }
            }
            else if (current is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(k, out var prop))
                {
                    current = prop;
                }
                else
                {
                    return $"[{key}]";
                }
            }
            else
            {
                return $"[{key}]";
            }
        }

        if (current is JsonElement jsonElement)
        {
            return jsonElement.GetString() ?? $"[{key}]";
        }

        return current?.ToString() ?? $"[{key}]";
    }

    public List<LanguageInfo> GetAvailableLanguages()
    {
        return new List<LanguageInfo>
        {
            new() { Code = "en", Name = "English", NativeName = "English" },
            new() { Code = "ko", Name = "Korean", NativeName = "한국어" }
        };
    }
}

public class LanguageInfo
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string NativeName { get; set; } = "";
}
