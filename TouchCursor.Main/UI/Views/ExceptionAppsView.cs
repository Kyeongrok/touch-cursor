using System.Windows;
using System.Windows.Controls;

namespace TouchCursor.Main.UI.Views;

public class ExceptionAppsView : Control
{
    static ExceptionAppsView()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ExceptionAppsView),
            new FrameworkPropertyMetadata(typeof(ExceptionAppsView)));
    }
}
