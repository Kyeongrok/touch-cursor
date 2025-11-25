using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using touch_cursor.Models;
using touch_cursor.Services;
using MessageBox = System.Windows.MessageBox;

namespace touch_cursor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly TouchCursorOptions _options;
    private readonly KeyboardHookService _hookService;
    private readonly KeyMappingService _mappingService;
    private NotifyIcon? _notifyIcon;
    private bool _isClosing = false;

    public MainWindow()
    {
        InitializeComponent();

        // Load options
        _options = TouchCursorOptions.Load(TouchCursorOptions.GetDefaultConfigPath());

        // Load language
        LocalizationManager.Instance.LoadLanguage(_options.Language);
        LocalizationManager.Instance.LanguageChanged += UpdateUI;

        // Initialize services
        _mappingService = new KeyMappingService(_options);
        _hookService = new KeyboardHookService(_mappingService, _options);

        // Wire up the SendKey event
        _mappingService.SendKeyRequested += _hookService.SendKey;

        // Setup system tray
        SetupNotifyIcon();

        // Load UI state from options
        LoadOptionsToUI();

        // Update UI text
        UpdateUI();

        // Start keyboard hook if enabled
        if (_options.Enabled)
        {
            _hookService.StartHook();
        }

        // Hide window on startup - only show in tray
        this.Visibility = Visibility.Hidden;
    }

    private void SetupNotifyIcon()
    {
        // Load custom icon if available, otherwise use system icon
        System.Drawing.Icon? appIcon = null;
        try
        {
            var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (System.IO.File.Exists(iconPath))
            {
                appIcon = new System.Drawing.Icon(iconPath);
            }
        }
        catch
        {
            // Fallback to system icon if custom icon fails to load
        }

        _notifyIcon = new NotifyIcon
        {
            Icon = appIcon ?? System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "TouchCursor"
        };

        _notifyIcon.DoubleClick += (s, e) =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Show", null, (s, e) =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        });
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Enable/Disable", null, (s, e) =>
        {
            _options.Enabled = !_options.Enabled;
            EnabledCheckBox.IsChecked = _options.Enabled;
            if (_options.Enabled)
                _hookService.StartHook();
            else
                _hookService.StopHook();
            _options.Save(TouchCursorOptions.GetDefaultConfigPath());
        });
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, (s, e) =>
        {
            _isClosing = true;
            Close();
        });

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void LoadOptionsToUI()
    {
        EnabledCheckBox.IsChecked = _options.Enabled;
        ModSwitchCheckBox.IsChecked = _options.ModSwitchEnabled;
        TrainingModeCheckBox.IsChecked = _options.TrainingMode;
        RunAtStartupCheckBox.IsChecked = _options.RunAtStartup;
    }

    private void UpdateUI()
    {
        var loc = LocalizationManager.Instance;

        // Update window title
        Title = loc.GetString("MainWindow.Title");
        TitleTextBlock.Text = loc.GetString("AppTitle");

        // Update checkboxes
        EnabledCheckBox.Content = loc.GetString("MainWindow.EnableTouchCursor");
        ModSwitchCheckBox.Content = loc.GetString("MainWindow.EnableModSwitch");
        TrainingModeCheckBox.Content = loc.GetString("MainWindow.TrainingMode");
        RunAtStartupCheckBox.Content = loc.GetString("MainWindow.RunAtStartup");

        // Update group box
        QuickInfoGroupBox.Header = loc.GetString("MainWindow.QuickInfo");

        // Update key mapping info
        DefaultKeyMappingsTextBlock.Text = loc.GetString("MainWindow.DefaultKeyMappings");
        ArrowKeysTextBlock.Text = "• " + loc.GetString("MainWindow.ArrowKeys");
        HomeEndTextBlock.Text = "• " + loc.GetString("MainWindow.HomeEnd");
        PageUpDownTextBlock.Text = "• " + loc.GetString("MainWindow.PageUpDown");
        BackspaceDeleteTextBlock.Text = "• " + loc.GetString("MainWindow.BackspaceDelete");
        WordNavigationTextBlock.Text = "• " + loc.GetString("MainWindow.WordNavigation");
        ModSwitchTextBlock.Text = "• " + loc.GetString("MainWindow.ModSwitch");

        // Update buttons
        SettingsButton.Content = loc.GetString("MainWindow.Settings");
        MinimizeButton.Content = loc.GetString("MainWindow.MinimizeToTray");
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_options == null || _hookService == null) return;

        _options.Enabled = EnabledCheckBox.IsChecked == true;
        if (_options.Enabled)
            _hookService.StartHook();
        else
            _hookService.StopHook();
        _options.Save(TouchCursorOptions.GetDefaultConfigPath());
    }

    private void ModSwitchCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_options == null) return;

        _options.ModSwitchEnabled = ModSwitchCheckBox.IsChecked == true;
        _options.Save(TouchCursorOptions.GetDefaultConfigPath());

        // Mod Switch가 비활성화되면 현재 토글 상태도 리셋
        if (!_options.ModSwitchEnabled && _mappingService != null)
        {
            _mappingService.Reset();
        }
    }

    private void TrainingModeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_options == null) return;

        _options.TrainingMode = TrainingModeCheckBox.IsChecked == true;
        _options.BeepForMistakes = _options.TrainingMode;
        _options.Save(TouchCursorOptions.GetDefaultConfigPath());
    }

    private void RunAtStartupCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_options == null) return;

        _options.RunAtStartup = RunAtStartupCheckBox.IsChecked == true;
        SetStartupRegistry(_options.RunAtStartup);
        _options.Save(TouchCursorOptions.GetDefaultConfigPath());
    }

    private void SetStartupRegistry(bool enable)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            if (enable)
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (exePath != null)
                    key?.SetValue("TouchCursor", exePath);
            }
            else
            {
                key?.DeleteValue("TouchCursor", false);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to update startup settings: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // MainWindow is no longer used - SettingsWindow is now the main window
        // This code is kept for compatibility but should not be called
        MessageBox.Show("SettingsWindow is now the main window. This button should not be visible.",
            "Information", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Position window at bottom-right of screen
        var workingArea = SystemParameters.WorkArea;
        Left = workingArea.Right - Width - 20;
        Top = workingArea.Bottom - Height - 20;
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (!_isClosing)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            _hookService?.Dispose();
            _notifyIcon?.Dispose();
        }
    }
}