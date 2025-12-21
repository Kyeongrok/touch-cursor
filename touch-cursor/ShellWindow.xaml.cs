using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using touch_cursor.Models;
using touch_cursor.Services;
using TouchCursor.Main.ViewModels;
using TouchCursor.Support.Local.Helpers;

namespace touch_cursor;

public partial class ShellWindow : Window
{
    private readonly TouchCursorOptions _options;
    private readonly KeyboardHookService _hookService;
    private readonly SettingsWindowViewModel _viewModel;
    private NotifyIcon? _notifyIcon;
    private bool _isClosing = false;

    public ShellWindow(
        TouchCursorOptions options,
        KeyboardHookService hookService,
        IKeyMappingService mappingService)
    {
        InitializeComponent();

        _options = options;
        _hookService = hookService;
        _viewModel = new SettingsWindowViewModel();

        // Load data into ViewModel
        LoadOptionsToViewModel();

        // Set ViewModel
        SettingsControl.ViewModel = _viewModel;

        // Setup events
        _viewModel.SaveRequested += OnSaveRequested;
        _viewModel.CancelRequested += OnCancelRequested;
        _viewModel.EnabledChanged += OnEnabledChanged;

        // Setup tray icon
        SetupNotifyIcon();

        // Start hook if enabled
        if (_options.Enabled)
        {
            _hookService.StartHook();
        }

        StateChanged += Window_StateChanged;
        Closing += Window_Closing;
    }

    private void LoadOptionsToViewModel()
    {
        _viewModel.IsEnabled = _options.Enabled;
        _viewModel.TrainingMode = _options.TrainingMode;
        _viewModel.RunAtStartup = _options.RunAtStartup;
        _viewModel.ShowInTray = _options.ShowInNotificationArea;
        _viewModel.CheckUpdates = _options.CheckForUpdates;
        _viewModel.BeepForMistakes = _options.BeepForMistakes;
        _viewModel.TypingAnalyticsEnabled = _options.TypingAnalyticsEnabled;
        _viewModel.AutoSwitchToEnglish = _options.AutoSwitchToEnglishOnNonConsonant;
        _viewModel.HoldDelayMs = _options.ActivationKeyHoldDelayMs;
        _viewModel.RolloverThresholdMs = _options.RolloverThresholdMs;
        _viewModel.UseEnableList = _options.UseEnableList;
        _viewModel.SelectedLanguage = _options.Language;

        // Load activation key profiles
        _viewModel.ActivationKeyProfiles.Clear();
        foreach (var profile in _options.ActivationKeyProfiles)
        {
            _viewModel.ActivationKeyProfiles.Add(new ActivationKeyProfileViewModel
            {
                VkCode = profile.Key,
                KeyName = GetKeyName(profile.Key),
                MappingCount = profile.Value.Count
            });
        }

        // Load key mappings for first profile
        if (_viewModel.ActivationKeyProfiles.Count > 0)
        {
            var firstProfile = _viewModel.ActivationKeyProfiles[0];
            _viewModel.SelectedActivationKeyProfile = firstProfile;
            LoadKeyMappings(firstProfile.VkCode);
        }

        // Load program lists
        _viewModel.DisableProgs.Clear();
        foreach (var prog in _options.DisableProgs)
            _viewModel.DisableProgs.Add(prog);

        _viewModel.EnableProgs.Clear();
        foreach (var prog in _options.EnableProgs)
            _viewModel.EnableProgs.Add(prog);

        // Load languages
        _viewModel.AvailableLanguages.Clear();
        _viewModel.AvailableLanguages.Add(new LanguageItem { Code = "en", NativeName = "English" });
        _viewModel.AvailableLanguages.Add(new LanguageItem { Code = "ko", NativeName = "한국어" });
    }

    private void LoadKeyMappings(int activationKey)
    {
        _viewModel.KeyMappings.Clear();

        if (_options.ActivationKeyProfiles.TryGetValue(activationKey, out var mappings))
        {
            foreach (var mapping in mappings)
            {
                var targetVk = mapping.Value & 0xFFFF;
                var modifiers = mapping.Value >> 16;

                _viewModel.KeyMappings.Add(new KeyMappingViewModel
                {
                    SourceVkCode = mapping.Key,
                    SourceKey = GetKeyName(mapping.Key),
                    TargetVkCode = targetVk,
                    Modifiers = modifiers,
                    TargetKey = GetKeyNameWithModifiers(targetVk, modifiers),
                    IgnoreRollover = _options.RolloverExceptionKeys.TryGetValue(activationKey, out var exceptions)
                                     && exceptions.Contains(mapping.Key)
                });
            }
        }
    }

    private void SaveViewModelToOptions()
    {
        _options.Enabled = _viewModel.IsEnabled;
        _options.TrainingMode = _viewModel.TrainingMode;
        _options.RunAtStartup = _viewModel.RunAtStartup;
        _options.ShowInNotificationArea = _viewModel.ShowInTray;
        _options.CheckForUpdates = _viewModel.CheckUpdates;
        _options.BeepForMistakes = _viewModel.BeepForMistakes;
        _options.TypingAnalyticsEnabled = _viewModel.TypingAnalyticsEnabled;
        _options.AutoSwitchToEnglishOnNonConsonant = _viewModel.AutoSwitchToEnglish;
        _options.ActivationKeyHoldDelayMs = _viewModel.HoldDelayMs;
        _options.RolloverThresholdMs = _viewModel.RolloverThresholdMs;
        _options.UseEnableList = _viewModel.UseEnableList;
        _options.Language = _viewModel.SelectedLanguage;

        _options.DisableProgs.Clear();
        _options.DisableProgs.AddRange(_viewModel.DisableProgs);

        _options.EnableProgs.Clear();
        _options.EnableProgs.AddRange(_viewModel.EnableProgs);

        _options.Save(TouchCursorOptions.GetDefaultConfigPath());
    }

    private void OnSaveRequested()
    {
        SaveViewModelToOptions();
        Close();
    }

    private void OnCancelRequested()
    {
        Close();
    }

    private void OnEnabledChanged()
    {
        if (_viewModel.IsEnabled)
        {
            _hookService.StartHook();
        }
        else
        {
            _hookService.StopHook();
        }
        _options.Enabled = _viewModel.IsEnabled;
    }

    private void SetupNotifyIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = new System.Drawing.Icon("app.ico"),
            Visible = _options.ShowInNotificationArea,
            Text = "TouchCursor"
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Show", null, (s, e) => { Show(); WindowState = WindowState.Normal; });
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, (s, e) => { _isClosing = true; Close(); });

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => { Show(); WindowState = WindowState.Normal; };
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && _options.ShowInNotificationArea)
        {
            Hide();
        }
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_isClosing && _options.ShowInNotificationArea)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _notifyIcon?.Dispose();
        _hookService.Dispose();
    }

    private string GetKeyName(int vkCode)
    {
        return vkCode switch
        {
            0x08 => "Backspace",
            0x09 => "Tab",
            0x0D => "Enter",
            0x1B => "Escape",
            0x14 => "CapsLock",
            0x20 => "Space",
            0x21 => "Page Up",
            0x22 => "Page Down",
            0x23 => "End",
            0x24 => "Home",
            0x25 => "Left",
            0x26 => "Up",
            0x27 => "Right",
            0x28 => "Down",
            0x2D => "Insert",
            0x2E => "Delete",
            >= 0x30 and <= 0x39 => ((char)vkCode).ToString(),
            >= 0x41 and <= 0x5A => ((char)vkCode).ToString(),
            0x70 => "F1", 0x71 => "F2", 0x72 => "F3", 0x73 => "F4",
            0x74 => "F5", 0x75 => "F6", 0x76 => "F7", 0x77 => "F8",
            0x78 => "F9", 0x79 => "F10", 0x7A => "F11", 0x7B => "F12",
            0xBA => ";", 0xBB => "=", 0xBC => ",", 0xBD => "-",
            0xBE => ".", 0xBF => "/", 0xC0 => "`",
            _ => $"VK_{vkCode:X2}"
        };
    }

    private string GetKeyNameWithModifiers(int vkCode, int modifiers)
    {
        var result = "";
        if ((modifiers & 0x0001) != 0) result += "Shift+";
        if ((modifiers & 0x0002) != 0) result += "Ctrl+";
        if ((modifiers & 0x0004) != 0) result += "Alt+";
        if ((modifiers & 0x0008) != 0) result += "Win+";
        return result + GetKeyName(vkCode);
    }
}
