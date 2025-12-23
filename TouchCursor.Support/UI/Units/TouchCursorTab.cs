using System.Windows;
using System.Windows.Controls;

namespace TouchCursor.Support.UI.Units;

/// <summary>
/// Custom tab control based on RadioButton
/// </summary>
public class TouchCursorTab : RadioButton
{
    static TouchCursorTab()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(TouchCursorTab),
            new FrameworkPropertyMetadata(typeof(TouchCursorTab)));
    }

    public TouchCursorTab()
    {
        // Set default group name for all tabs
        if (string.IsNullOrEmpty(GroupName))
        {
            GroupName = "TouchCursorTabs";
        }
    }
}