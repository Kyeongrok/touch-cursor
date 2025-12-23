using System.Windows;
using System.Windows.Controls;

namespace TouchCursor.Support.UI.Units;

/// <summary>
/// Custom maximize/restore button for window title bar
/// </summary>
public class MaximizeButton : Button
{
    static MaximizeButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(MaximizeButton),
            new FrameworkPropertyMetadata(typeof(MaximizeButton)));
    }

    public MaximizeButton()
    {
        // Set default maximize icon (Segoe MDL2 Assets)
        Content = "\uE922";
    }
}
