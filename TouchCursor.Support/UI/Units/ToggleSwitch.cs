using System.Windows;
using System.Windows.Controls.Primitives;

namespace TouchCursor.Support.UI.Units;

public class ToggleSwitch : ToggleButton
{
    static ToggleSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ToggleSwitch),
            new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
    }
}
