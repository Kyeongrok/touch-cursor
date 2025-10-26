// Copyright © 2025. Ported to C# from original C++ TouchCursor by Martin Stone.
// Original project licensed under GNU GPL v3.

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace touch_cursor.Models;

public class TouchCursorOptions
{
    private const int MaxKeyCodes = 0x100;

    // General settings
    public bool Enabled { get; set; } = true;
    public bool TrainingMode { get; set; } = false;
    public bool BeepForMistakes { get; set; } = false;
    public bool RunAtStartup { get; set; } = false;
    public bool ShowInNotificationArea { get; set; } = true;
    public bool CheckForUpdates { get; set; } = true;
    public bool TypingAnalyticsEnabled { get; set; } = false;

    // Rollover detection: if a key is pressed within this time after activation key,
    // treat both as normal typing instead of cursor mode
    public int RolloverThresholdMs { get; set; } = 50;

    // Rollover exception keys: keys that ignore rollover detection (per activation key)
    // ActivationKey -> HashSet of source keys that always activate cursor mode
    public Dictionary<int, HashSet<int>> RolloverExceptionKeys { get; set; } = new();

    // Legacy: Single activation key (for backward compatibility)
    [JsonIgnore]
    public int ActivationKey
    {
        get => ActivationKeyProfiles.Keys.FirstOrDefault(0x20);
        set
        {
            if (ActivationKeyProfiles.Count == 0 || ActivationKeyProfiles.ContainsKey(0x20))
            {
                // Migrate legacy single activation key to new structure
                if (ActivationKeyProfiles.ContainsKey(0x20))
                {
                    var oldMappings = ActivationKeyProfiles[0x20];
                    ActivationKeyProfiles.Remove(0x20);
                    ActivationKeyProfiles[value] = oldMappings;
                }
                else
                {
                    ActivationKeyProfiles[value] = new Dictionary<int, int>();
                }
            }
        }
    }

    public string Language { get; set; } = "en"; // Default language

    // Multi-activation key support: ActivationKey -> (SourceKey -> TargetKey|Modifiers)
    public Dictionary<int, Dictionary<int, int>> ActivationKeyProfiles { get; set; } = new();

    // Legacy: Single key mapping (for backward compatibility)
    [JsonIgnore]
    public Dictionary<int, int> KeyMapping
    {
        get => ActivationKeyProfiles.Values.FirstOrDefault() ?? new Dictionary<int, int>();
        set
        {
            var activationKey = ActivationKeyProfiles.Keys.FirstOrDefault(0x20);
            ActivationKeyProfiles[activationKey] = value;
        }
    }

    // Program lists
    public List<string> DisableProgs { get; set; } = new();
    public List<string> EnableProgs { get; set; } = new();
    public List<string> NeverTrainProgs { get; set; } = new();
    public List<string> OnlyTrainProgs { get; set; } = new();
    public bool UseEnableList { get; set; } = false;
    public bool UseOnlyTrainList { get; set; } = false;

    // Last update check timestamp
    public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;

    public TouchCursorOptions()
    {
        InitializeDefaultKeyMappings();
    }

    private void InitializeDefaultKeyMappings()
    {
        // Initialize default Space profile if no profiles exist
        if (ActivationKeyProfiles.Count == 0)
        {
            var spaceMappings = new Dictionary<int, int>();

            // Default TouchCursor mappings (Space + key combinations)
            // Space + IJKL = Arrow keys
            spaceMappings[0x49] = 0x26; // I -> Up Arrow
            spaceMappings[0x4A] = 0x25; // J -> Left Arrow
            spaceMappings[0x4B] = 0x28; // K -> Down Arrow
            spaceMappings[0x4C] = 0x27; // L -> Right Arrow

            // Space + UO = Home/End
            spaceMappings[0x55] = 0x24; // U -> Home
            spaceMappings[0x4F] = 0x23; // O -> End

            // Space + HP = Page Up/Down
            spaceMappings[0x48] = 0x25; // H -> Left
            spaceMappings[0x50] = 0x08; // P -> Backspace

            // Space + M, = Backspace/Delete
            spaceMappings[0x4D] = 0x2E; // M -> Delete

            // Space + N. for word navigation
            spaceMappings[0x4E] = (int)(0x25 | (int)ModifierFlags.Ctrl); // N -> Ctrl+Left
            spaceMappings[0xBE] = (int)(0x27 | (int)ModifierFlags.Ctrl); // . -> Ctrl+Right (VK_OEM_PERIOD)

            ActivationKeyProfiles[0x20] = spaceMappings; // VK_SPACE
        }
    }

    public bool ShouldCheckForUpdate()
    {
        if (!CheckForUpdates) return false;
        return (DateTime.Now - LastUpdateCheck).TotalDays >= 7;
    }

    public void Save(string filePath)
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(filePath, json);
    }

    public static TouchCursorOptions Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            var options = new TouchCursorOptions();
            options.Save(filePath);
            return options;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<TouchCursorOptions>(json)
                   ?? new TouchCursorOptions();
        }
        catch
        {
            return new TouchCursorOptions();
        }
    }

    public static string GetDefaultConfigPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDir = Path.Combine(appData, "TouchCursor");
        Directory.CreateDirectory(configDir);
        return Path.Combine(configDir, "config.json");
    }
}
