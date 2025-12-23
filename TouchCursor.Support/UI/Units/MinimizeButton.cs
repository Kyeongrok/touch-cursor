using System.Windows;
using System.Windows.Controls;

namespace TouchCursor.Support.UI.Units;

/// <summary>
/// Custom minimize button for window title bar
/// </summary>
public class MinimizeButton : Button
{
    static MinimizeButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(MinimizeButton),
            new FrameworkPropertyMetadata(typeof(MinimizeButton)));
    }

    public MinimizeButton()
    {
        // Set default minimize icon (Segoe MDL2 Assets)
        Content = "\uE921";
    }
}
