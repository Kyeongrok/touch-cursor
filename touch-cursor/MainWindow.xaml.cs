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

        // Initialize services
        _mappingService = new KeyMappingService(_options);
        _hookService = new KeyboardHookService(_mappingService);

        // Wire up the SendKey event
        _mappingService.SendKeyRequested += _hookService.SendKey;

        // Setup system tray
        SetupNotifyIcon();

        // Load UI state from options
        LoadOptionsToUI();

        // Start keyboard hook if enabled
        if (_options.Enabled)
        {
            _hookService.StartHook();
        }
    }

    private void SetupNotifyIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
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
        TrainingModeCheckBox.IsChecked = _options.TrainingMode;
        RunAtStartupCheckBox.IsChecked = _options.RunAtStartup;
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
        MessageBox.Show("Settings window will be implemented in the next phase.", "Info",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
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