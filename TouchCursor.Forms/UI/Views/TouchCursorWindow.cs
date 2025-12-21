using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TouchCursor.Forms.ViewModels;
using TouchCursor.Main.UI.Views;
using BaseTouchCursorWindow = TouchCursor.Support.UI.Views.TouchCursorWindow;

namespace TouchCursor.Forms.UI.Views;

[TemplatePart(Name = PART_ContentRegion, Type = typeof(ContentControl))]
[TemplatePart(Name = PART_GeneralTab, Type = typeof(ToggleButton))]
[TemplatePart(Name = PART_KeyMappingsTab, Type = typeof(ToggleButton))]
public class TouchCursorWindow : BaseTouchCursorWindow
{
    private const string PART_ContentRegion = "PART_ContentRegion";
    private const string PART_GeneralTab = "PART_GeneralTab";
    private const string PART_KeyMappingsTab = "PART_KeyMappingsTab";

    private ContentControl? _contentRegion;
    private ToggleButton? _generalTab;
    private ToggleButton? _keyMappingsTab;

    private GeneralSettingsView? _generalSettingsView;
    private KeyMappingsView? _keyMappingsView;

    static TouchCursorWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TouchCursorWindow),
            new FrameworkPropertyMetadata(typeof(TouchCursorWindow)));
    }

    public TouchCursorWindow(TouchCursorWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        Closing += OnWindowClosing;
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (ViewModel?.HandleClosing() == true)
        {
            e.Cancel = true;
        }
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(TouchCursorWindowViewModel),
            typeof(TouchCursorWindow), new PropertyMetadata(null, OnViewModelChanged));

    public TouchCursorWindowViewModel? ViewModel
    {
        get => (TouchCursorWindowViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Unsubscribe old events
        if (_generalTab != null)
            _generalTab.Checked -= OnGeneralTabChecked;
        if (_keyMappingsTab != null)
            _keyMappingsTab.Checked -= OnKeyMappingsTabChecked;

        // Get template parts
        _contentRegion = GetTemplateChild(PART_ContentRegion) as ContentControl;
        _generalTab = GetTemplateChild(PART_GeneralTab) as ToggleButton;
        _keyMappingsTab = GetTemplateChild(PART_KeyMappingsTab) as ToggleButton;

        // Subscribe new events
        if (_generalTab != null)
            _generalTab.Checked += OnGeneralTabChecked;
        if (_keyMappingsTab != null)
            _keyMappingsTab.Checked += OnKeyMappingsTabChecked;

        // Create views
        _generalSettingsView = new GeneralSettingsView();
        _keyMappingsView = new KeyMappingsView();

        // Set initial content
        if (_generalTab?.IsChecked == true)
        {
            ShowGeneralSettings();
        }
        else if (_keyMappingsTab?.IsChecked == true)
        {
            ShowKeyMappings();
        }
        else
        {
            // Default to General tab
            if (_generalTab != null)
                _generalTab.IsChecked = true;
        }
    }

    private void OnGeneralTabChecked(object sender, RoutedEventArgs e)
    {
        ShowGeneralSettings();
    }

    private void OnKeyMappingsTabChecked(object sender, RoutedEventArgs e)
    {
        ShowKeyMappings();
    }

    private void ShowGeneralSettings()
    {
        if (_contentRegion != null && _generalSettingsView != null && ViewModel != null)
        {
            _generalSettingsView.DataContext = ViewModel.SettingsViewModel.GeneralSettings;
            _contentRegion.Content = _generalSettingsView;
        }
    }

    private void ShowKeyMappings()
    {
        if (_contentRegion != null && _keyMappingsView != null && ViewModel != null)
        {
            _keyMappingsView.DataContext = ViewModel.SettingsViewModel;
            _contentRegion.Content = _keyMappingsView;
        }
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TouchCursorWindow control)
        {
            if (e.NewValue is TouchCursorWindowViewModel viewModel)
            {
                control.DataContext = viewModel.SettingsViewModel;

                // Update view DataContexts if already created
                if (control._generalSettingsView != null)
                    control._generalSettingsView.DataContext = viewModel.SettingsViewModel.GeneralSettings;
                if (control._keyMappingsView != null)
                    control._keyMappingsView.DataContext = viewModel.SettingsViewModel;

                // Setup tray icon
                control.SetupTrayIcon(viewModel);
            }
        }
    }

    private void SetupTrayIcon(TouchCursorWindowViewModel viewModel)
    {
        var icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!);
        if (icon != null)
        {
            viewModel.SetupNotifyIcon(icon);
        }

        viewModel.CloseRequested += () => Close();
        viewModel.HideRequested += () => Hide();
        viewModel.ShowRequested += () =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        };
    }
}
