namespace TouchCursor.Support.Local.Helpers;

public interface ITouchCursorOptions
{
    bool Enabled { get; set; }
    bool AutoSwitchToEnglishOnNonConsonant { get; set; }
}
