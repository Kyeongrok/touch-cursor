using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;
using Prism.Commands;
using Prism.Mvvm;
using TouchCursor.Forms.UI.Views;
using TouchCursor.Main.UI.Views;
using TouchCursor.Main.ViewModels;
using TouchCursor.Support.Local.Helpers;
using TouchCursor.Support.Local.Services;

namespace TouchCursor.Forms.ViewModels;

public class TouchCursorWindowViewModel : BindableBase
{
    private readonly ITouchCursorOptions _options;
    private readonly KeyboardHookService _hookService;
    private readonly IKeyMappingService _mappingService;
    private readonly SettingsWindowViewModel _settingsViewModel;
    private TaskbarIcon? _taskbarIcon;
    private ActivationOverlayWindow? _overlayWindow;
    private bool _isClosing = false;
    private System.Threading.Timer? _activationTimer;

    public SettingsWindowViewModel SettingsViewModel => _settingsViewModel;

    public event Action? CloseRequested;
    public event Action? HideRequested;
    public event Action? ShowRequested;

    public TouchCursorWindowViewModel(
        ITouchCursorOptions options,
        KeyboardHookService hookService,
        IKeyMappingService mappingService)
    {
        _options = options;
        _hookService = hookService;
        _mappingService = mappingService;
        _settingsViewModel = new SettingsWindowViewModel();

        // Create overlay window
        _overlayWindow = new ActivationOverlayWindow();
        _overlayWindow.SetPosition(options.OverlayPosition);
        _mappingService.ActivationKeyPressed += OnActivationKeyPressed;
        _mappingService.ActivationStateChanged += OnActivationStateChanged;

        LoadOptionsToViewModel();

        _settingsViewModel.SaveRequested += OnSaveRequested;
        _settingsViewModel.CancelRequested += OnCancelRequested;
        _settingsViewModel.AboutRequested += OnAboutRequested;
        _settingsViewModel.GeneralSettings.EnabledChanged += OnEnabledChanged;
        _settingsViewModel.GeneralSettings.LanguageChanged += OnLanguageChanged;
        _settingsViewModel.GeneralSettings.OverlayPositionChanged += OnOverlayPositionChanged;
        _settingsViewModel.GeneralSettings.HoldDelayMsChanged += OnHoldDelayMsChanged;
        _settingsViewModel.GeneralSettings.PropertyChanged += OnGeneralSettingsPropertyChanged;
        _settingsViewModel.GeneralSettings.AddActivationKeyRequested += OnAddActivationKeyRequested;
        _settingsViewModel.EditKeyMappingRequested += OnEditKeyMappingRequested;

        if (_options.Enabled)
        {
            _hookService.StartHook();
        }
    }

    public void SetupNotifyIcon(System.Drawing.Icon icon)
    {
        var showMenuItem = new MenuItem { Header = "Show" };
        showMenuItem.Click += (s, e) => ShowRequested?.Invoke();

        var exitMenuItem = new MenuItem { Header = "Exit" };
        exitMenuItem.Click += (s, e) => { _isClosing = true; CloseRequested?.Invoke(); };

        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(showMenuItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(exitMenuItem);

        _taskbarIcon = new TaskbarIcon
        {
            Icon = icon,
            Visibility = _options.ShowInNotificationArea ? Visibility.Visible : Visibility.Collapsed,
            ToolTipText = "TouchCursor",
            ContextMenu = contextMenu
        };

        _taskbarIcon.TrayMouseDoubleClick += (s, e) => ShowRequested?.Invoke();
    }

    public bool HandleClosing()
    {
        if (!_isClosing && _options.ShowInNotificationArea)
        {
            HideRequested?.Invoke();
            return true; // Cancel close
        }

        _taskbarIcon?.Dispose();
        _overlayWindow?.Close();
        _hookService.Dispose();
        return false; // Allow close
    }

    private void OnActivationKeyPressed(int activationKey)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (_overlayWindow != null)
            {
                _overlayWindow.KeyName = GetKeyName(activationKey);
                _overlayWindow.State = ActivationState.Waiting;

                // ActivationKeyHoldDelayMs 후에 파란색으로 변경
                _activationTimer?.Dispose();
                var delay = _options.ActivationKeyHoldDelayMs;
                if (delay > 0)
                {
                    _activationTimer = new System.Threading.Timer(_ =>
                    {
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (_overlayWindow != null && _overlayWindow.State == ActivationState.Waiting)
                            {
                                _overlayWindow.State = ActivationState.Activated;
                            }
                        });
                    }, null, delay, System.Threading.Timeout.Infinite);
                }
            }
        });
    }

    private void OnActivationStateChanged(int activationKey, bool isActive)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            // 타이머 취소
            _activationTimer?.Dispose();
            _activationTimer = null;

            if (_overlayWindow != null)
            {
                _overlayWindow.KeyName = GetKeyName(activationKey);
                _overlayWindow.State = isActive ? ActivationState.Activated : ActivationState.None;
            }
        });
    }

    public bool ShouldHideOnMinimize => _options.ShowInNotificationArea;

    private void LoadOptionsToViewModel()
    {
        var gs = _settingsViewModel.GeneralSettings;

        gs.IsEnabled = _options.Enabled;
        gs.RunAtStartup = _options.RunAtStartup;
        gs.ShowInTray = _options.ShowInNotificationArea;
        gs.BeepForMistakes = _options.BeepForMistakes;
        gs.HoldDelayMs = _options.ActivationKeyHoldDelayMs;
        gs.RolloverEnabled = _options.RolloverEnabled;
        gs.SelectedLanguage = _options.Language;
        gs.OverlayPosition = _options.OverlayPosition;

        // Load activation key profiles
        gs.ActivationKeyProfiles.Clear();
        foreach (var profile in _options.ActivationKeyProfiles)
        {
            gs.ActivationKeyProfiles.Add(new ActivationKeyProfileViewModel
            {
                VkCode = profile.Key,
                KeyName = GetKeyName(profile.Key),
                MappingCount = profile.Value.Count
            });
        }

        // Load key mappings for first profile
        if (gs.ActivationKeyProfiles.Count > 0)
        {
            var firstProfile = gs.ActivationKeyProfiles[0];
            gs.SelectedActivationKeyProfile = firstProfile;
            _settingsViewModel.SelectedActivationKeyForMappings = firstProfile.VkCode;
            LoadKeyMappings(firstProfile.VkCode);
        }

        // Load languages
        gs.AvailableLanguages.Clear();
        gs.AvailableLanguages.Add(new LanguageItem { Code = "en", NativeName = "English" });
        gs.AvailableLanguages.Add(new LanguageItem { Code = "ko", NativeName = "한국어" });
    }

    private void LoadKeyMappings(int activationKey)
    {
        _settingsViewModel.KeyMappings.Clear();

        if (_options.ActivationKeyProfiles.TryGetValue(activationKey, out var mappings))
        {
            foreach (var mapping in mappings)
            {
                var targetVk = mapping.Value & 0xFFFF;
                var modifiers = mapping.Value >> 16;

                _settingsViewModel.KeyMappings.Add(new KeyMappingViewModel
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
        var gs = _settingsViewModel.GeneralSettings;

        _options.Enabled = gs.IsEnabled;
        _options.RunAtStartup = gs.RunAtStartup;
        _options.ShowInNotificationArea = gs.ShowInTray;
        _options.BeepForMistakes = gs.BeepForMistakes;
        _options.ActivationKeyHoldDelayMs = gs.HoldDelayMs;
        _options.RolloverEnabled = gs.RolloverEnabled;
        _options.Language = gs.SelectedLanguage;
        _options.OverlayPosition = gs.OverlayPosition;

        _options.Save(TouchCursorOptions.GetDefaultConfigPath());
    }

    private void OnSaveRequested()
    {
        SaveViewModelToOptions();
        HideRequested?.Invoke();
    }

    private void OnCancelRequested()
    {
        HideRequested?.Invoke();
    }

    private void OnAboutRequested()
    {
        MessageBox.Show(
            "TouchCursor\n" +
            "Version 1.0\n\n" +
            "Based on original TouchCursor by Martin Stone\n" +
            "Licensed under GNU GPL v3",
            "About TouchCursor",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OnEnabledChanged()
    {
        if (_settingsViewModel.GeneralSettings.IsEnabled)
        {
            _hookService.StartHook();
        }
        else
        {
            _hookService.StopHook();
        }
        _options.Enabled = _settingsViewModel.GeneralSettings.IsEnabled;
        _options.Save(TouchCursorOptions.GetDefaultConfigPath());
    }

    private void OnLanguageChanged()
    {
        var language = _settingsViewModel.GeneralSettings.SelectedLanguage;
        LocalizationService.Instance.LoadLanguage(language);
        _options.Language = language;
        _options.Save(TouchCursorOptions.GetDefaultConfigPath());
    }

    private void OnOverlayPositionChanged()
    {
        var position = _settingsViewModel.GeneralSettings.OverlayPosition;
        _overlayWindow?.SetPosition(position);
        _options.OverlayPosition = position;
        _options.Save(TouchCursorOptions.GetDefaultConfigPath());
    }

    private void OnHoldDelayMsChanged()
    {
        _options.ActivationKeyHoldDelayMs = _settingsViewModel.GeneralSettings.HoldDelayMs;
        _options.Save(TouchCursorOptions.GetDefaultConfigPath());
    }

    private void OnGeneralSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GeneralSettingsViewModel.SelectedActivationKeyProfile))
        {
            if (_settingsViewModel.GeneralSettings.SelectedActivationKeyProfile != null)
            {
                LoadKeyMappings(_settingsViewModel.GeneralSettings.SelectedActivationKeyProfile.VkCode);
            }
        }
        else if (e.PropertyName == nameof(GeneralSettingsViewModel.RolloverEnabled))
        {
            _options.RolloverEnabled = _settingsViewModel.GeneralSettings.RolloverEnabled;
            _options.Save(TouchCursorOptions.GetDefaultConfigPath());
        }
        else if (e.PropertyName == nameof(GeneralSettingsViewModel.ShowInTray))
        {
            _options.ShowInNotificationArea = _settingsViewModel.GeneralSettings.ShowInTray;
            _taskbarIcon!.Visibility = _options.ShowInNotificationArea ? Visibility.Visible : Visibility.Collapsed;
            _options.Save(TouchCursorOptions.GetDefaultConfigPath());
        }
        else if (e.PropertyName == nameof(GeneralSettingsViewModel.BeepForMistakes))
        {
            _options.BeepForMistakes = _settingsViewModel.GeneralSettings.BeepForMistakes;
            _options.Save(TouchCursorOptions.GetDefaultConfigPath());
        }
    }

    private ActivationKeyProfileViewModel? OnAddActivationKeyRequested()
    {
        var dialog = new ActivationKeyDialogWindow
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.SelectedKey is { } selected)
        {
            // Check if already exists
            if (_settingsViewModel.GeneralSettings.ActivationKeyProfiles.Any(p => p.VkCode == selected.VkCode))
            {
                MessageBox.Show("이 키는 이미 활성화 키로 등록되어 있습니다.", "중복", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            // Add to options
            if (!_options.ActivationKeyProfiles.ContainsKey(selected.VkCode))
            {
                _options.ActivationKeyProfiles[selected.VkCode] = new Dictionary<int, int>();
            }

            return new ActivationKeyProfileViewModel
            {
                VkCode = selected.VkCode,
                KeyName = GetKeyName(selected.VkCode),
                MappingCount = 0
            };
        }

        return null;
    }

    private KeyMappingViewModel? OnEditKeyMappingRequested(KeyMappingViewModel? existing)
    {
        // TODO: Implement key mapping editor dialog
        // For now, return null (cancel)
        return null;
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
