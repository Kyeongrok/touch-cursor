using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TouchCursor.Main.UI.Units;

[TemplatePart(Name = PART_HelpButton, Type = typeof(Button))]
[TemplatePart(Name = PART_MinimizeButton, Type = typeof(Button))]
[TemplatePart(Name = PART_MaximizeButton, Type = typeof(Button))]
[TemplatePart(Name = PART_CloseButton, Type = typeof(Button))]
public class TitleBar : Control
{
    private const string PART_HelpButton = "PART_HelpButton";
    private const string PART_MinimizeButton = "PART_MinimizeButton";
    private const string PART_MaximizeButton = "PART_MaximizeButton";
    private const string PART_CloseButton = "PART_CloseButton";

    private Button? _helpButton;
    private Button? _minimizeButton;
    private Button? _maximizeButton;
    private Button? _closeButton;

    static TitleBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(TitleBar),
            new FrameworkPropertyMetadata(typeof(TitleBar)));
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(TitleBar),
            new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(ImageSource),
            typeof(TitleBar),
            new PropertyMetadata(null));

    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty ShowMinimizeButtonProperty =
        DependencyProperty.Register(
            nameof(ShowMinimizeButton),
            typeof(bool),
            typeof(TitleBar),
            new PropertyMetadata(true));

    public bool ShowMinimizeButton
    {
        get => (bool)GetValue(ShowMinimizeButtonProperty);
        set => SetValue(ShowMinimizeButtonProperty, value);
    }

    public static readonly DependencyProperty ShowMaximizeButtonProperty =
        DependencyProperty.Register(
            nameof(ShowMaximizeButton),
            typeof(bool),
            typeof(TitleBar),
            new PropertyMetadata(true));

    public bool ShowMaximizeButton
    {
        get => (bool)GetValue(ShowMaximizeButtonProperty);
        set => SetValue(ShowMaximizeButtonProperty, value);
    }

    public static readonly DependencyProperty HelpCommandProperty =
        DependencyProperty.Register(
            nameof(HelpCommand),
            typeof(ICommand),
            typeof(TitleBar),
            new PropertyMetadata(null));

    public ICommand? HelpCommand
    {
        get => (ICommand?)GetValue(HelpCommandProperty);
        set => SetValue(HelpCommandProperty, value);
    }

    public static readonly DependencyProperty TabContentProperty =
        DependencyProperty.Register(
            nameof(TabContent),
            typeof(object),
            typeof(TitleBar),
            new PropertyMetadata(null));

    public object? TabContent
    {
        get => GetValue(TabContentProperty);
        set => SetValue(TabContentProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_helpButton != null)
            _helpButton.Click -= OnHelpClick;
        if (_minimizeButton != null)
            _minimizeButton.Click -= OnMinimizeClick;
        if (_maximizeButton != null)
            _maximizeButton.Click -= OnMaximizeClick;
        if (_closeButton != null)
            _closeButton.Click -= OnCloseClick;

        _helpButton = GetTemplateChild(PART_HelpButton) as Button;
        _minimizeButton = GetTemplateChild(PART_MinimizeButton) as Button;
        _maximizeButton = GetTemplateChild(PART_MaximizeButton) as Button;
        _closeButton = GetTemplateChild(PART_CloseButton) as Button;

        if (_helpButton != null)
            _helpButton.Click += OnHelpClick;
        if (_minimizeButton != null)
            _minimizeButton.Click += OnMinimizeClick;
        if (_maximizeButton != null)
            _maximizeButton.Click += OnMaximizeClick;
        if (_closeButton != null)
            _closeButton.Click += OnCloseClick;
    }

    private void OnHelpClick(object sender, RoutedEventArgs e)
    {
        HelpCommand?.Execute(null);
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window != null)
            window.WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        window?.Close();
    }
}
