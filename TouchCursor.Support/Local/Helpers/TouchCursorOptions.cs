// Copyright © 2025. Ported to C# from original C++ TouchCursor by Martin Stone.
// Original project licensed under GNU GPL v3.

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TouchCursor.Support.Local.Helpers;

public enum OverlayPosition
{
    BottomRight,
    BottomLeft,
    TopRight,
    TopLeft
}

public class TouchCursorOptions : ITouchCursorOptions
{
    private const int MaxKeyCodes = 0x100;

    // 일반 설정
    public bool Enabled { get; set; } = true;
    public bool TrainingMode { get; set; } = false;
    public bool BeepForMistakes { get; set; } = false;
    public bool RunAtStartup { get; set; } = false;
    public bool ShowInNotificationArea { get; set; } = true;
    public bool CheckForUpdates { get; set; } = true;
    public OverlayPosition OverlayPosition { get; set; } = OverlayPosition.BottomRight;

    // 활성화 키 홀드 딜레이: 활성화 키를 이 시간만큼 눌러야 커서 모드 활성화
    // 매핑된 키를 딜레이 전에 누르면 두 키 모두 일반 타이핑으로 처리됨
    // 0으로 설정하면 비활성화 (즉시 활성화)
    public int ActivationKeyHoldDelayMs { get; set; } = 0;

    // 롤오버 감지: 활성화 키 누른 후 이 시간 내에 키를 누르면
    // 두 키 모두 일반 타이핑으로 처리
    public int RolloverThresholdMs { get; set; } = 50;

    // Mod Switch 기능: 단축키로 활성화 키를 토글 (누르고 있는 것처럼 유지)
    public bool ModSwitchEnabled { get; set; } = true;

    // Mod Switch 토글 단축키 (기본: Alt + Space)
    // 수정자 키 플래그 (상위 2바이트) + 키 코드 (하위 2바이트)
    public int ModSwitchToggleKey { get; set; } = 0x20; // Space (VK_SPACE)
    public int ModSwitchToggleModifiers { get; set; } = (int)ModifierFlags.Alt; // Alt 키

    // 롤오버 예외 키: 롤오버 감지를 무시하는 키 (활성화 키별)
    // ActivationKey -> 항상 커서 모드를 활성화하는 소스 키 HashSet
    public Dictionary<int, HashSet<int>> RolloverExceptionKeys { get; set; } = new();

    // 레거시: 단일 활성화 키 (하위 호환성)
    [JsonIgnore]
    public int ActivationKey
    {
        get => ActivationKeyProfiles.Keys.FirstOrDefault(0x20);
        set
        {
            if (ActivationKeyProfiles.Count == 0 || ActivationKeyProfiles.ContainsKey(0x20))
            {
                // 레거시 단일 활성화 키를 새 구조로 마이그레이션
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

    public string Language { get; set; } = "en"; // 기본 언어

    // 다중 활성화 키 지원: ActivationKey -> (SourceKey -> TargetKey|Modifiers)
    public Dictionary<int, Dictionary<int, int>> ActivationKeyProfiles { get; set; } = new();

    // 레거시: 단일 키 매핑 (하위 호환성)
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

    // 마지막 업데이트 확인 타임스탬프
    public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;

    public TouchCursorOptions()
    {
        InitializeDefaultKeyMappings();
    }

    private void InitializeDefaultKeyMappings()
    {
        // 프로파일이 없을 경우 기본 Space 프로파일 초기화
        if (ActivationKeyProfiles.Count == 0)
        {
            var spaceMappings = new Dictionary<int, int>();

            // 기본 TouchCursor 매핑 (Space + 키 조합)
            // Space + IJKL = 방향키
            spaceMappings[0x49] = 0x26; // I -> 위 방향키
            spaceMappings[0x4A] = 0x25; // J -> 왼쪽 방향키
            spaceMappings[0x4B] = 0x28; // K -> 아래 방향키
            spaceMappings[0x4C] = 0x27; // L -> 오른쪽 방향키

            // Space + UO = Home/End
            spaceMappings[0x55] = 0x24; // U -> Home
            spaceMappings[0x4F] = 0x23; // O -> End

            // Space + HP = 좌/백스페이스
            spaceMappings[0x48] = 0x25; // H -> 왼쪽
            spaceMappings[0x50] = 0x08; // P -> Backspace

            // Space + M = Delete
            spaceMappings[0x4D] = 0x2E; // M -> Delete

            // Space + N. = 단어 단위 이동
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
