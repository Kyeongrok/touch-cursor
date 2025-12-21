using System.Windows;
using System.Windows.Controls;

namespace TouchCursor.Main.UI.Views;

public class KeyMappingsView : Control
{
    static KeyMappingsView()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(KeyMappingsView),
            new FrameworkPropertyMetadata(typeof(KeyMappingsView)));
    }
}
