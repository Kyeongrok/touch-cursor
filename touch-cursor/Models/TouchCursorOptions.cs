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
    public int ActivationKey { get; set; } = 0x20; // VK_SPACE

    // Key mapping (VK_CODE -> VK_CODE or VK_CODE | ModifierFlags)
    public Dictionary<int, int> KeyMapping { get; set; } = new();

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
        // Default TouchCursor mappings (Space + key combinations)
        // Space + IJKL = Arrow keys
        KeyMapping[0x49] = 0x26; // I -> Up Arrow
        KeyMapping[0x4A] = 0x25; // J -> Left Arrow
        KeyMapping[0x4B] = 0x28; // K -> Down Arrow
        KeyMapping[0x4C] = 0x27; // L -> Right Arrow

        // Space + UO = Home/End
        KeyMapping[0x55] = 0x24; // U -> Home
        KeyMapping[0x4F] = 0x23; // O -> End

        // Space + HP = Page Up/Down
        KeyMapping[0x48] = 0x21; // H -> Page Up
        KeyMapping[0x50] = 0x22; // P -> Page Down

        // Space + M, = Backspace/Delete
        KeyMapping[0x4D] = 0x08; // M -> Backspace
        KeyMapping[0xBC] = 0x2E; // , -> Delete (VK_OEM_COMMA)

        // Space + NM for word navigation
        KeyMapping[0x4E] = (int)(0x25 | (int)ModifierFlags.Ctrl); // N -> Ctrl+Left
        KeyMapping[0xBE] = (int)(0x27 | (int)ModifierFlags.Ctrl); // . -> Ctrl+Right (VK_OEM_PERIOD)
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
