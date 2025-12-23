using System.Windows;
using System.Windows.Controls;

namespace TouchCursor.Support.UI.Units;

/// <summary>
/// Custom close button for window title bar
/// </summary>
public class CloseButton : Button
{
    static CloseButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(CloseButton),
            new FrameworkPropertyMetadata(typeof(CloseButton)));
    }

    public CloseButton()
    {
        // Set default close icon (Segoe MDL2 Assets)
        Content = "\uE8BB";
    }
}