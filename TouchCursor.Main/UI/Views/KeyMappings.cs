using System.Windows;
using System.Windows.Controls;

namespace TouchCursor.Main.UI.Views;

public class KeyMappings : Control
{
    static KeyMappings()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(KeyMappings),
            new FrameworkPropertyMetadata(typeof(KeyMappings)));
    }
}
