using System.ComponentModel;
using System.Windows.Forms;
using Prism.Mvvm;
using TouchCursor.Main.ViewModels;
using TouchCursor.Support.Local.Helpers;

namespace TouchCursor.Forms.ViewModels;

public class TouchCursorWindowViewModel : BindableBase
{
    private readonly ITouchCursorOptions _options;
    private readonly KeyboardHookService _hookService;
    private readonly SettingsWindowViewModel _settingsViewModel;
    private NotifyIcon? _notifyIcon;
    private bool _isClosing = false;

    public SettingsWindowViewModel SettingsViewModel => _settingsViewModel;

    public event Action? CloseRequested;
    public event Action? HideRequested;
    public event Action? ShowRequested;

    public TouchCursorWindowViewModel(
        ITouchCursorOptions options,
        KeyboardHookService hookService)
    {
        _options = options;
        _hookService = hookService;
        _settingsViewModel = new SettingsWindowViewModel();

        LoadOptionsToViewModel();

        _settingsViewModel.SaveRequested += OnSaveRequested;
        _settingsViewModel.CancelRequested += OnCancelRequested;
        _settingsViewModel.EnabledChanged += OnEnabledChanged;
        _settingsViewModel.PropertyChanged += OnSettingsPropertyChanged;
        _settingsViewModel.AddActivationKeyRequested += OnAddActivationKeyRequested;
        _settingsViewModel.EditKeyMappingRequested += OnEditKeyMappingRequested;

        if (_options.Enabled)
        {
            _hookService.StartHook();
        }
    }

    public void SetupNotifyIcon(System.Drawing.Icon icon)
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = icon,
            Visible = _options.ShowInNotificationArea,
            Text = "TouchCursor"
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Show", null, (s, e) => ShowRequested?.Invoke());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, (s, e) => { _isClosing = true; CloseRequested?.Invoke(); });

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => ShowRequested?.Invoke();
    }

    public bool HandleClosing()
    {
        if (!_isClosing && _options.ShowInNotificationArea)
        {
            HideRequested?.Invoke();
            return true; // Cancel close
        }

        _notifyIcon?.Dispose();
        _hookService.Dispose();
        return false; // Allow close
    }

    public bool ShouldHideOnMinimize => _options.ShowInNotificationArea;

    private void LoadOptionsToViewModel()
    {
        _settingsViewModel.IsEnabled = _options.Enabled;
        _settingsViewModel.TrainingMode = _options.TrainingMode;
        _settingsViewModel.RunAtStartup = _options.RunAtStartup;
        _settingsViewModel.ShowInTray = _options.ShowInNotificationArea;
        _settingsViewModel.CheckUpdates = _options.CheckForUpdates;
        _settingsViewModel.BeepForMistakes = _options.BeepForMistakes;
        _settingsViewModel.HoldDelayMs = _options.ActivationKeyHoldDelayMs;
        _settingsViewModel.RolloverThresholdMs = _options.RolloverThresholdMs;
        _settingsViewModel.SelectedLanguage = _options.Language;

        // Load activation key profiles
        _settingsViewModel.ActivationKeyProfiles.Clear();
        foreach (var profile in _options.ActivationKeyProfiles)
        {
            _settingsViewModel.ActivationKeyProfiles.Add(new ActivationKeyProfileViewModel
            {
                VkCode = profile.Key,
                KeyName = GetKeyName(profile.Key),
                MappingCount = profile.Value.Count
            });
        }

        // Load key mappings for first profile
        if (_settingsViewModel.ActivationKeyProfiles.Count > 0)
        {
            var firstProfile = _settingsViewModel.ActivationKeyProfiles[0];
            _settingsViewModel.SelectedActivationKeyProfile = firstProfile;
            _settingsViewModel.SelectedActivationKeyForMappings = firstProfile.VkCode;
            LoadKeyMappings(firstProfile.VkCode);
        }

        // Load languages
        _settingsViewModel.AvailableLanguages.Clear();
        _settingsViewModel.AvailableLanguages.Add(new LanguageItem { Code = "en", NativeName = "English" });
        _settingsViewModel.AvailableLanguages.Add(new LanguageItem { Code = "ko", NativeName = "한국어" });
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
        _options.Enabled = _settingsViewModel.IsEnabled;
        _options.TrainingMode = _settingsViewModel.TrainingMode;
        _options.RunAtStartup = _settingsViewModel.RunAtStartup;
        _options.ShowInNotificationArea = _settingsViewModel.ShowInTray;
        _options.CheckForUpdates = _settingsViewModel.CheckUpdates;
        _options.BeepForMistakes = _settingsViewModel.BeepForMistakes;
        _options.ActivationKeyHoldDelayMs = _settingsViewModel.HoldDelayMs;
        _options.RolloverThresholdMs = _settingsViewModel.RolloverThresholdMs;
        _options.Language = _settingsViewModel.SelectedLanguage;

        _options.Save(TouchCursorOptions.GetDefaultConfigPath());
    }

    private void OnSaveRequested()
    {
        SaveViewModelToOptions();
        _isClosing = true;
        CloseRequested?.Invoke();
    }

    private void OnCancelRequested()
    {
        _isClosing = true;
        CloseRequested?.Invoke();
    }

    private void OnEnabledChanged()
    {
        if (_settingsViewModel.IsEnabled)
        {
            _hookService.StartHook();
        }
        else
        {
            _hookService.StopHook();
        }
        _options.Enabled = _settingsViewModel.IsEnabled;
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsWindowViewModel.SelectedActivationKeyProfile))
        {
            if (_settingsViewModel.SelectedActivationKeyProfile != null)
            {
                LoadKeyMappings(_settingsViewModel.SelectedActivationKeyProfile.VkCode);
            }
        }
        else if (e.PropertyName == nameof(SettingsWindowViewModel.SelectedActivationKeyForMappings))
        {
            LoadKeyMappings(_settingsViewModel.SelectedActivationKeyForMappings);
        }
    }

    private ActivationKeyProfileViewModel? OnAddActivationKeyRequested()
    {
        using var form = new Form
        {
            Text = "프로파일 추가...",
            Size = new System.Drawing.Size(350, 180),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            Padding = new Padding(20)
        };

        var label = new Label
        {
            Text = "새 프로파일의 활성화 키를 선택하세요:",
            AutoSize = true,
            Location = new System.Drawing.Point(20, 20)
        };

        var comboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new System.Drawing.Point(20, 50),
            Width = 290
        };

        // 활성화 키 후보 목록
        var keyOptions = new (int vkCode, string name)[]
        {
            (0x20, "스페이스 (기본값)"),
            (0x14, "CapsLock"),
            (0x09, "Tab"),
            (0xA0, "왼쪽 Shift"),
            (0xA1, "오른쪽 Shift"),
            (0xA2, "왼쪽 Ctrl"),
            (0xA3, "오른쪽 Ctrl"),
            (0xA4, "왼쪽 Alt"),
            (0xA5, "오른쪽 Alt"),
            (0x1B, "Escape"),
            (0xC0, "` (백틱)"),
        };

        foreach (var (vkCode, name) in keyOptions)
        {
            comboBox.Items.Add(new KeyComboItem { VkCode = vkCode, DisplayName = name });
        }
        comboBox.DisplayMember = "DisplayName";
        comboBox.SelectedIndex = 0;

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 40,
            Padding = new Padding(0, 5, 15, 5)
        };

        var cancelButton = new Button
        {
            Text = "취소",
            Width = 80,
            Height = 28,
            DialogResult = System.Windows.Forms.DialogResult.Cancel
        };

        var okButton = new Button
        {
            Text = "확인",
            Width = 80,
            Height = 28,
            DialogResult = System.Windows.Forms.DialogResult.OK,
            Margin = new Padding(5, 0, 0, 0)
        };

        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(okButton);

        form.Controls.Add(label);
        form.Controls.Add(comboBox);
        form.Controls.Add(buttonPanel);
        form.AcceptButton = okButton;
        form.CancelButton = cancelButton;

        if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK && comboBox.SelectedItem is KeyComboItem selected)
        {
            // Check if already exists
            if (_settingsViewModel.ActivationKeyProfiles.Any(p => p.VkCode == selected.VkCode))
            {
                MessageBox.Show("이 키는 이미 활성화 키로 등록되어 있습니다.", "중복", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

    private class KeyComboItem
    {
        public int VkCode { get; set; }
        public string DisplayName { get; set; } = "";
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
