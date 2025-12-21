namespace TouchCursor.Support.Local.Helpers;

public interface ITouchCursorOptions
{
    // 일반 설정
    bool Enabled { get; set; }
    bool TrainingMode { get; set; }
    bool BeepForMistakes { get; set; }
    bool RunAtStartup { get; set; }
    bool ShowInNotificationArea { get; set; }
    bool CheckForUpdates { get; set; }
    bool AutoSwitchToEnglishOnNonConsonant { get; set; }

    // 타이밍 설정
    int ActivationKeyHoldDelayMs { get; set; }
    int RolloverThresholdMs { get; set; }

    // Mod Switch 설정
    bool ModSwitchEnabled { get; set; }
    int ModSwitchToggleKey { get; set; }
    int ModSwitchToggleModifiers { get; set; }

    // 키 매핑
    Dictionary<int, Dictionary<int, int>> ActivationKeyProfiles { get; set; }
    Dictionary<int, HashSet<int>> RolloverExceptionKeys { get; set; }

    // 언어
    string Language { get; set; }

    // 업데이트
    DateTime LastUpdateCheck { get; set; }

    // 메서드
    void Save(string filePath);
    bool ShouldCheckForUpdate();
}
