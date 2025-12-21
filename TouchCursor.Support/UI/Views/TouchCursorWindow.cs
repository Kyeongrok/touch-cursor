using System.Windows;
using System.Windows.Controls;

namespace TouchCursor.Support.UI.Views;

[TemplatePart(Name = PART_MinimizeButton, Type = typeof(Button))]
[TemplatePart(Name = PART_MaximizeButton, Type = typeof(Button))]
[TemplatePart(Name = PART_CloseButton, Type = typeof(Button))]
public class TouchCursorWindow : Window
{
    private const string PART_MinimizeButton = "PART_MinimizeButton";
    private const string PART_MaximizeButton = "PART_MaximizeButton";
    private const string PART_CloseButton = "PART_CloseButton";

    private Button? _minimizeButton;
    private Button? _maximizeButton;
    private Button? _closeButton;

    static TouchCursorWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(TouchCursorWindow),
            new FrameworkPropertyMetadata(typeof(TouchCursorWindow)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Unsubscribe old events
        if (_minimizeButton != null)
            _minimizeButton.Click -= OnMinimizeClick;
        if (_maximizeButton != null)
            _maximizeButton.Click -= OnMaximizeClick;
        if (_closeButton != null)
            _closeButton.Click -= OnCloseClick;

        // Get template parts
        _minimizeButton = GetTemplateChild(PART_MinimizeButton) as Button;
        _maximizeButton = GetTemplateChild(PART_MaximizeButton) as Button;
        _closeButton = GetTemplateChild(PART_CloseButton) as Button;

        // Subscribe new events
        if (_minimizeButton != null)
            _minimizeButton.Click += OnMinimizeClick;
        if (_maximizeButton != null)
            _maximizeButton.Click += OnMaximizeClick;
        if (_closeButton != null)
            _closeButton.Click += OnCloseClick;
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
