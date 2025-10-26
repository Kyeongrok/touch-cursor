using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using touch_cursor.Models;
using touch_cursor.Services;
using MessageBox = System.Windows.MessageBox;

namespace touch_cursor;

public partial class SettingsWindow : Window
{
    private readonly TouchCursorOptions _options;
    private readonly TouchCursorOptions _originalOptions;
    private readonly KeyboardHookService _hookService;
    private readonly KeyMappingService _mappingService;
    private NotifyIcon? _notifyIcon;
    private bool _isClosing = false;
    private bool _hasChanges = false;
    private int _selectedActivationKey = 0; // Currently selected activation key for editing mappings

    public SettingsWindow()
    {
        InitializeComponent();

        // Load options
        _options = TouchCursorOptions.Load(TouchCursorOptions.GetDefaultConfigPath());

        // Create a backup of original options
        _originalOptions = LoadBackup(_options);

        // Load language
        LocalizationManager.Instance.LoadLanguage(_options.Language);
        LocalizationManager.Instance.LanguageChanged += UpdateUI;

        // Initialize services
        _mappingService = new KeyMappingService(_options);
        _hookService = new KeyboardHookService(_mappingService);

        // Wire up the SendKey event
        _mappingService.SendKeyRequested += _hookService.SendKey;

        // Setup system tray
        SetupNotifyIcon();

        LoadOptionsToUI();
        UpdateUI();

        // Start keyboard hook if enabled
        if (_options.Enabled)
        {
            _hookService.StartHook();
        }
    }

    private TouchCursorOptions LoadBackup(TouchCursorOptions options)
    {
        var backup = new TouchCursorOptions
        {
            Enabled = options.Enabled,
            TrainingMode = options.TrainingMode,
            BeepForMistakes = options.BeepForMistakes,
            RunAtStartup = options.RunAtStartup,
            ShowInNotificationArea = options.ShowInNotificationArea,
            CheckForUpdates = options.CheckForUpdates,
            RolloverThresholdMs = options.RolloverThresholdMs,
            ActivationKey = options.ActivationKey,
            UseEnableList = options.UseEnableList,
            DisableProgs = new List<string>(options.DisableProgs),
            EnableProgs = new List<string>(options.EnableProgs),
            NeverTrainProgs = new List<string>(options.NeverTrainProgs),
            OnlyTrainProgs = new List<string>(options.OnlyTrainProgs)
        };
        return backup;
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
        contextMenu.Items.Add("Show Settings", null, (s, e) =>
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
        // General Tab - Behavior
        EnabledCheckBox.IsChecked = _options.Enabled;
        TrainingModeCheckBox.IsChecked = _options.TrainingMode;
        RunAtStartupCheckBox.IsChecked = _options.RunAtStartup;
        ShowInTrayCheckBox.IsChecked = _options.ShowInNotificationArea;
        CheckUpdatesCheckBox.IsChecked = _options.CheckForUpdates;
        BeepForMistakesCheckBox.IsChecked = _options.BeepForMistakes;
        RolloverThresholdSlider.Value = _options.RolloverThresholdMs;
        RolloverThresholdValueText.Text = $"{_options.RolloverThresholdMs} ms";

        // General Tab - Activation Key Profiles
        LoadActivationKeyProfiles();

        // Language
        LanguageComboBox.ItemsSource = LocalizationManager.Instance.GetAvailableLanguages();
        LanguageComboBox.SelectedValue = _options.Language;

        // Key Mappings Tab - Load activation key selector
        LoadKeyMappingActivationKeys();

        // Program Lists Tab
        UseEnableListCheckBox.IsChecked = _options.UseEnableList;
        DisableProgsListBox.ItemsSource = new ObservableCollection<string>(_options.DisableProgs);
        EnableProgsListBox.ItemsSource = new ObservableCollection<string>(_options.EnableProgs);
    }

    private void LoadActivationKeyProfiles()
    {
        var profiles = new ObservableCollection<ActivationKeyProfileDisplay>();

        foreach (var kvp in _options.ActivationKeyProfiles)
        {
            var vkCode = kvp.Key;
            var mappings = kvp.Value;

            profiles.Add(new ActivationKeyProfileDisplay
            {
                VkCode = vkCode,
                KeyName = GetKeyName(vkCode),
                MappingCount = $"({mappings.Count} mappings)"
            });
        }

        ActivationKeyProfilesListBox.ItemsSource = profiles;
    }

    private void UpdateUI()
    {
        var loc = LocalizationManager.Instance;

        // Update window title and header
        Title = loc.GetString("SettingsWindow.Title");
        HeaderTextBlock.Text = loc.GetString("SettingsWindow.Header");

        // Update tab headers
        GeneralTab.Header = loc.GetString("SettingsWindow.TabGeneral");
        KeyMappingsTab.Header = loc.GetString("SettingsWindow.TabKeyMappings");
        ProgramListsTab.Header = loc.GetString("SettingsWindow.TabProgramLists");

        // General Tab
        ActivationKeyProfilesGroupBox.Header = loc.GetString("SettingsWindow.ActivationKeyProfiles");
        ActivationKeyProfilesDescriptionTextBlock.Text = loc.GetString("SettingsWindow.ActivationKeyProfilesDescription");
        AddActivationKeyButton.Content = loc.GetString("SettingsWindow.AddProfile");
        RemoveActivationKeyButton.Content = loc.GetString("SettingsWindow.RemoveProfile");

        BehaviorGroupBox.Header = loc.GetString("SettingsWindow.Behavior");
        EnabledCheckBox.Content = loc.GetString("SettingsWindow.EnableTouchCursor");
        TrainingModeCheckBox.Content = loc.GetString("SettingsWindow.TrainingMode");
        RunAtStartupCheckBox.Content = loc.GetString("SettingsWindow.RunAtStartup");
        RolloverThresholdLabel.Text = loc.GetString("SettingsWindow.RolloverThreshold");
        RolloverThresholdDescription.Text = loc.GetString("SettingsWindow.RolloverThresholdDescription");
        ShowInTrayCheckBox.Content = loc.GetString("SettingsWindow.ShowInTray");
        CheckUpdatesCheckBox.Content = loc.GetString("SettingsWindow.CheckUpdates");
        BeepForMistakesCheckBox.Content = loc.GetString("SettingsWindow.BeepForMistakes");

        LanguageGroupBox.Header = loc.GetString("SettingsWindow.Language");
        LanguageDescriptionTextBlock.Text = loc.GetString("SettingsWindow.LanguageDescription");

        AboutGroupBox.Header = loc.GetString("SettingsWindow.About");
        AboutTitleTextBlock.Text = loc.GetString("SettingsWindow.AboutTitle");
        // Get version from assembly
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        AboutVersionTextBlock.Text = $"{loc.GetString("SettingsWindow.AboutVersion").Replace("2.0.0", $"{version?.Major}.{version?.Minor}.{version?.Build}")}";
        AboutOriginalTextBlock.Text = loc.GetString("SettingsWindow.AboutOriginal");
        AboutLicenseTextBlock.Text = loc.GetString("SettingsWindow.AboutLicense");

        // Key Mappings Tab
        CurrentKeyMappingsTextBlock.Text = loc.GetString("SettingsWindow.CurrentKeyMappings");
        SelectActivationKeyTextBlock.Text = loc.GetString("SettingsWindow.SelectActivationKey");
        KeyMappingsNoteTextBlock.Text = loc.GetString("SettingsWindow.KeyMappingsNote");
        AddMappingButton.Content = loc.GetString("SettingsWindow.AddMapping");
        EditMappingButton.Content = loc.GetString("SettingsWindow.EditMapping");
        RemoveMappingButton.Content = loc.GetString("SettingsWindow.RemoveMapping");

        // DataGrid columns
        ColumnActivationKey.Header = loc.GetString("SettingsWindow.ColumnActivationKey");
        ColumnMapsTo.Header = loc.GetString("SettingsWindow.ColumnMapsTo");
        ColumnIgnoreRollover.Header = loc.GetString("SettingsWindow.ColumnIgnoreRollover");
        ColumnDescription.Header = loc.GetString("SettingsWindow.ColumnDescription");

        // Program Lists Tab
        ProgramSpecificTextBlock.Text = loc.GetString("SettingsWindow.ProgramSpecificSettings");
        ProgramSpecificNoteTextBlock.Text = loc.GetString("SettingsWindow.ProgramSpecificNote");

        DisableProgsGroupBox.Header = loc.GetString("SettingsWindow.DisableInPrograms");
        EnableProgsGroupBox.Header = loc.GetString("SettingsWindow.EnableOnlyInPrograms");
        UseEnableListCheckBox.Content = loc.GetString("SettingsWindow.UseEnableList");

        AddDisableProgButton.Content = loc.GetString("SettingsWindow.Add");
        RemoveDisableProgButton.Content = loc.GetString("SettingsWindow.Remove");
        AddEnableProgButton.Content = loc.GetString("SettingsWindow.Add");
        RemoveEnableProgButton.Content = loc.GetString("SettingsWindow.Remove");

        // Bottom buttons
        ResetButton.Content = loc.GetString("SettingsWindow.ResetToDefaults");
        OkButton.Content = loc.GetString("SettingsWindow.OK");
        CancelButton.Content = loc.GetString("SettingsWindow.Cancel");

        // Reload key mappings with new language
        LoadKeyMappings();
    }

    // Legacy method - no longer used with multi-activation key support
    //private void SetActivationKeySelection(int vkCode)
    //{
    //    // Removed - using ActivationKeyProfiles instead
    //}

    private void LoadKeyMappingActivationKeys()
    {
        var profiles = new ObservableCollection<ActivationKeyProfileDisplay>();

        foreach (var kvp in _options.ActivationKeyProfiles)
        {
            var vkCode = kvp.Key;
            var mappings = kvp.Value;

            profiles.Add(new ActivationKeyProfileDisplay
            {
                VkCode = vkCode,
                KeyName = GetKeyName(vkCode),
                MappingCount = $"({mappings.Count} mappings)"
            });
        }

        KeyMappingActivationKeyComboBox.ItemsSource = profiles;

        // Select the first activation key by default
        if (profiles.Count > 0)
        {
            KeyMappingActivationKeyComboBox.SelectedIndex = 0;
        }
    }

    private void KeyMappingActivationKeyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (KeyMappingActivationKeyComboBox.SelectedItem is ActivationKeyProfileDisplay selected)
        {
            _selectedActivationKey = selected.VkCode;
            LoadKeyMappings();
        }
    }

    private void LoadKeyMappings()
    {
        var mappings = new ObservableCollection<KeyMappingDisplay>();

        // Get the mappings for the currently selected activation key
        if (_selectedActivationKey == 0 || !_options.ActivationKeyProfiles.ContainsKey(_selectedActivationKey))
        {
            KeyMappingsDataGrid.ItemsSource = mappings;
            return;
        }

        var keyMappings = _options.ActivationKeyProfiles[_selectedActivationKey];

        // Get rollover exception keys for this activation key
        _options.RolloverExceptionKeys.TryGetValue(_selectedActivationKey, out var exceptionKeys);

        // Convert key mappings to display format
        foreach (var kvp in keyMappings)
        {
            var sourceKey = GetKeyName(kvp.Key);
            var targetVk = kvp.Value & 0xFFFF;
            var modifiers = (int)(kvp.Value & 0xFFFF0000);
            var targetKey = GetKeyNameWithModifiers(targetVk, modifiers);
            var description = GetMappingDescription(kvp.Key, targetVk, modifiers);

            var display = new KeyMappingDisplay
            {
                SourceVkCode = kvp.Key,
                SourceKey = sourceKey,
                TargetKey = targetKey,
                Description = description,
                IgnoreRollover = exceptionKeys?.Contains(kvp.Key) ?? false
            };

            // Subscribe to property changes to track modifications
            display.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(KeyMappingDisplay.IgnoreRollover))
                {
                    _hasChanges = true;
                    UpdateRolloverExceptionKeys();
                }
            };

            mappings.Add(display);
        }

        KeyMappingsDataGrid.ItemsSource = mappings;
    }

    private void UpdateRolloverExceptionKeys()
    {
        if (KeyMappingsDataGrid.ItemsSource is ObservableCollection<KeyMappingDisplay> mappings)
        {
            // Clear existing exception keys for this activation key
            if (!_options.RolloverExceptionKeys.ContainsKey(_selectedActivationKey))
            {
                _options.RolloverExceptionKeys[_selectedActivationKey] = new HashSet<int>();
            }

            var exceptionKeys = _options.RolloverExceptionKeys[_selectedActivationKey];
            exceptionKeys.Clear();

            // Add checked keys to exception list
            foreach (var mapping in mappings)
            {
                if (mapping.IgnoreRollover)
                {
                    exceptionKeys.Add(mapping.SourceVkCode);
                }
            }

            // Remove empty sets to keep config clean
            if (exceptionKeys.Count == 0)
            {
                _options.RolloverExceptionKeys.Remove(_selectedActivationKey);
            }
        }
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
            0x30 => "0", 0x31 => "1", 0x32 => "2", 0x33 => "3", 0x34 => "4",
            0x35 => "5", 0x36 => "6", 0x37 => "7", 0x38 => "8", 0x39 => "9",
            0x41 => "A", 0x42 => "B", 0x43 => "C", 0x44 => "D", 0x45 => "E",
            0x46 => "F", 0x47 => "G", 0x48 => "H", 0x49 => "I", 0x4A => "J",
            0x4B => "K", 0x4C => "L", 0x4D => "M", 0x4E => "N", 0x4F => "O",
            0x50 => "P", 0x51 => "Q", 0x52 => "R", 0x53 => "S", 0x54 => "T",
            0x55 => "U", 0x56 => "V", 0x57 => "W", 0x58 => "X", 0x59 => "Y",
            0x5A => "Z",
            0x70 => "F1", 0x71 => "F2", 0x72 => "F3", 0x73 => "F4",
            0x74 => "F5", 0x75 => "F6", 0x76 => "F7", 0x77 => "F8",
            0x78 => "F9", 0x79 => "F10", 0x7A => "F11", 0x7B => "F12",
            0xA2 => "Left Ctrl",
            0xA3 => "Right Ctrl",
            0xBA => ";", 0xBB => "=", 0xBC => ",", 0xBD => "-",
            0xBE => ".", 0xBF => "/", 0xC0 => "`", 0xDB => "[",
            0xDC => "\\", 0xDD => "]", 0xDE => "'",
            0xFF => "Fn",
            _ => $"VK_{vkCode:X2}"
        };
    }

    private string GetKeyNameWithModifiers(int vkCode, int modifiers)
    {
        var loc = LocalizationManager.Instance;
        var modifierStr = "";
        if ((modifiers & (int)ModifierFlags.Ctrl) != 0) modifierStr += "Ctrl+";
        if ((modifiers & (int)ModifierFlags.Shift) != 0) modifierStr += "Shift+";
        if ((modifiers & (int)ModifierFlags.Alt) != 0) modifierStr += "Alt+";
        if ((modifiers & (int)ModifierFlags.Win) != 0) modifierStr += "Win+";

        var keyName = vkCode switch
        {
            0x26 => loc.GetString("KeyMappings.Up"),
            0x28 => loc.GetString("KeyMappings.Down"),
            0x25 => loc.GetString("KeyMappings.Left"),
            0x27 => loc.GetString("KeyMappings.Right"),
            0x24 => loc.GetString("KeyMappings.Home"),
            0x23 => loc.GetString("KeyMappings.End"),
            0x21 => loc.GetString("KeyMappings.PageUp"),
            0x22 => loc.GetString("KeyMappings.PageDown"),
            0x08 => loc.GetString("KeyMappings.Backspace"),
            0x2E => loc.GetString("KeyMappings.Delete"),
            0x2D => loc.GetString("KeyMappings.Insert"),
            _ => GetKeyName(vkCode) // Use GetKeyName for all other keys
        };

        return modifierStr + keyName;
    }

    private string GetMappingDescription(int sourceVk, int targetVk, int modifiers)
    {
        var loc = LocalizationManager.Instance;
        return (sourceVk, targetVk) switch
        {
            (0x49, 0x26) => loc.GetString("KeyMappings.MoveCursorUp"),
            (0x4B, 0x28) => loc.GetString("KeyMappings.MoveCursorDown"),
            (0x4A, 0x25) => loc.GetString("KeyMappings.MoveCursorLeft"),
            (0x4C, 0x27) => loc.GetString("KeyMappings.MoveCursorRight"),
            (0x55, 0x24) => loc.GetString("KeyMappings.JumpToLineStart"),
            (0x4F, 0x23) => loc.GetString("KeyMappings.JumpToLineEnd"),
            (0x48, 0x21) => loc.GetString("KeyMappings.ScrollPageUp"),
            (0x50, 0x22) => loc.GetString("KeyMappings.ScrollPageDown"),
            (0x4D, 0x08) => loc.GetString("KeyMappings.DeleteBefore"),
            (0xBC, 0x2E) => loc.GetString("KeyMappings.DeleteAfter"),
            (0x4E, 0x25) when (modifiers & (int)ModifierFlags.Ctrl) != 0 => loc.GetString("KeyMappings.MoveWordLeft"),
            (0xBE, 0x27) when (modifiers & (int)ModifierFlags.Ctrl) != 0 => loc.GetString("KeyMappings.MoveWordRight"),
            _ => loc.GetString("KeyMappings.CustomMapping")
        };
    }

    // Legacy method - no longer used with multi-activation key support
    //private void ActivationKeyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    //{
    //    // Removed - using ActivationKeyProfiles instead
    //}

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

    private void ShowInTrayCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_options == null) return;
        _options.ShowInNotificationArea = ShowInTrayCheckBox.IsChecked == true;
        _hasChanges = true;
    }

    private void CheckUpdatesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_options == null) return;
        _options.CheckForUpdates = CheckUpdatesCheckBox.IsChecked == true;
        _hasChanges = true;
    }

    private void BeepForMistakesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_options == null) return;
        _options.BeepForMistakes = BeepForMistakesCheckBox.IsChecked == true;
        _hasChanges = true;
    }

    private void RolloverThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_options == null || RolloverThresholdValueText == null) return;

        var value = (int)RolloverThresholdSlider.Value;
        _options.RolloverThresholdMs = value;
        RolloverThresholdValueText.Text = $"{value} ms";
        _hasChanges = true;
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_options == null || LanguageComboBox.SelectedValue == null) return;

        var selectedLanguage = LanguageComboBox.SelectedValue.ToString();
        if (selectedLanguage != null && selectedLanguage != _options.Language)
        {
            _options.Language = selectedLanguage;
            LocalizationManager.Instance.LoadLanguage(selectedLanguage);
            _hasChanges = true;
        }
    }

    private void UseEnableListCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_options == null) return;
        _options.UseEnableList = UseEnableListCheckBox.IsChecked == true;
        _hasChanges = true;
    }

    private void AddDisableProg_Click(object sender, RoutedEventArgs e)
    {
        var loc = LocalizationManager.Instance;
        var dialog = new InputDialog(
            loc.GetString("InputDialog.AddProgramPrompt"),
            loc.GetString("InputDialog.AddProgramTitle"));
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResponseText))
        {
            var programName = dialog.ResponseText.Trim();
            if (!_options.DisableProgs.Contains(programName))
            {
                _options.DisableProgs.Add(programName);
                DisableProgsListBox.ItemsSource = new ObservableCollection<string>(_options.DisableProgs);
                _hasChanges = true;
            }
        }
    }

    private void RemoveDisableProg_Click(object sender, RoutedEventArgs e)
    {
        if (DisableProgsListBox.SelectedItem is string selectedProg)
        {
            _options.DisableProgs.Remove(selectedProg);
            DisableProgsListBox.ItemsSource = new ObservableCollection<string>(_options.DisableProgs);
            _hasChanges = true;
        }
    }

    private void AddEnableProg_Click(object sender, RoutedEventArgs e)
    {
        var loc = LocalizationManager.Instance;
        var dialog = new InputDialog(
            loc.GetString("InputDialog.AddProgramPrompt"),
            loc.GetString("InputDialog.AddProgramTitle"));
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResponseText))
        {
            var programName = dialog.ResponseText.Trim();
            if (!_options.EnableProgs.Contains(programName))
            {
                _options.EnableProgs.Add(programName);
                EnableProgsListBox.ItemsSource = new ObservableCollection<string>(_options.EnableProgs);
                _hasChanges = true;
            }
        }
    }

    private void RemoveEnableProg_Click(object sender, RoutedEventArgs e)
    {
        if (EnableProgsListBox.SelectedItem is string selectedProg)
        {
            _options.EnableProgs.Remove(selectedProg);
            EnableProgsListBox.ItemsSource = new ObservableCollection<string>(_options.EnableProgs);
            _hasChanges = true;
        }
    }

    private void KeyMappingsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool hasSelection = KeyMappingsDataGrid.SelectedItem != null;
        EditMappingButton.IsEnabled = hasSelection;
        RemoveMappingButton.IsEnabled = hasSelection;
    }

    private void KeyMappingsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Check if a row is actually selected
        if (KeyMappingsDataGrid.SelectedItem != null)
        {
            // Call the same method as Edit button
            EditMappingButton_Click(sender, e);
        }
    }

    private void AddMappingButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedActivationKey == 0 || !_options.ActivationKeyProfiles.ContainsKey(_selectedActivationKey))
        {
            MessageBox.Show("Please select an activation key profile first.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new KeyMappingEditorDialog();
        if (dialog.ShowDialog() == true)
        {
            var sourceVk = dialog.SourceVkCode;
            var targetVk = dialog.TargetVkCode;
            var modifiers = dialog.TargetModifiers;
            var mappedKey = targetVk | modifiers;

            // Add or update the mapping for the selected activation key
            _options.ActivationKeyProfiles[_selectedActivationKey][sourceVk] = mappedKey;
            LoadKeyMappings();
            LoadKeyMappingActivationKeys(); // Refresh the activation key list to update mapping counts
            _hasChanges = true;
        }
    }

    private void EditMappingButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedActivationKey == 0 || !_options.ActivationKeyProfiles.ContainsKey(_selectedActivationKey))
        {
            MessageBox.Show("Please select an activation key profile first.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (KeyMappingsDataGrid.SelectedItem is KeyMappingDisplay selected)
        {
            var keyMappings = _options.ActivationKeyProfiles[_selectedActivationKey];

            // Find the original mapping
            var sourceVk = keyMappings.FirstOrDefault(kvp =>
                GetKeyName(kvp.Key) == selected.SourceKey).Key;

            if (sourceVk != 0)
            {
                var mappedKey = keyMappings[sourceVk];
                var targetVk = mappedKey & 0xFFFF;
                var modifiers = (int)(mappedKey & 0xFFFF0000);

                var dialog = new KeyMappingEditorDialog(sourceVk, targetVk, modifiers, selected.Description);
                if (dialog.ShowDialog() == true)
                {
                    // Remove old mapping
                    keyMappings.Remove(sourceVk);

                    // Add new mapping
                    var newSourceVk = dialog.SourceVkCode;
                    var newTargetVk = dialog.TargetVkCode;
                    var newModifiers = dialog.TargetModifiers;
                    var newMappedKey = newTargetVk | newModifiers;

                    keyMappings[newSourceVk] = newMappedKey;
                    LoadKeyMappings();
                    LoadKeyMappingActivationKeys(); // Refresh the activation key list to update mapping counts
                    _hasChanges = true;
                }
            }
        }
    }

    private void RemoveMappingButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedActivationKey == 0 || !_options.ActivationKeyProfiles.ContainsKey(_selectedActivationKey))
        {
            MessageBox.Show("Please select an activation key profile first.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (KeyMappingsDataGrid.SelectedItem is KeyMappingDisplay selected)
        {
            var loc = LocalizationManager.Instance;
            var result = MessageBox.Show(
                $"{loc.GetString("SettingsWindow.RemoveMappingConfirmation")}\n\n{selected.SourceKey} → {selected.TargetKey}",
                loc.GetString("SettingsWindow.RemoveMappingConfirmationTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var keyMappings = _options.ActivationKeyProfiles[_selectedActivationKey];

                // Find and remove the mapping
                var sourceVk = keyMappings.FirstOrDefault(kvp =>
                    GetKeyName(kvp.Key) == selected.SourceKey).Key;

                if (sourceVk != 0)
                {
                    keyMappings.Remove(sourceVk);
                    LoadKeyMappings();
                    LoadKeyMappingActivationKeys(); // Refresh the activation key list to update mapping counts
                    _hasChanges = true;
                }
            }
        }
    }

    private void ActivationKeyProfilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool hasSelection = ActivationKeyProfilesListBox.SelectedItem != null;
        RemoveActivationKeyButton.IsEnabled = hasSelection;
    }

    private void AddActivationKeyButton_Click(object sender, RoutedEventArgs e)
    {
        var loc = LocalizationManager.Instance;

        // Show a simple dialog to select activation key
        var dialog = new Window
        {
            Title = loc.GetString("SettingsWindow.AddProfile"),
            Width = 400,
            Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize
        };

        var stack = new StackPanel { Margin = new Thickness(20) };

        stack.Children.Add(new TextBlock
        {
            Text = loc.GetString("SettingsWindow.SelectActivationKeyPrompt"),
            Margin = new Thickness(0, 0, 0, 10)
        });

        var comboBox = new System.Windows.Controls.ComboBox { Width = 200, HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
        comboBox.Items.Add(new ComboBoxItem { Content = loc.GetString("SettingsWindow.ActivationKeySpace"), Tag = 32 });
        comboBox.Items.Add(new ComboBoxItem { Content = loc.GetString("SettingsWindow.ActivationKeyCapsLock"), Tag = 20 });
        comboBox.Items.Add(new ComboBoxItem { Content = loc.GetString("SettingsWindow.ActivationKeyTab"), Tag = 9 });
        comboBox.Items.Add(new ComboBoxItem { Content = loc.GetString("SettingsWindow.ActivationKeyBackspace"), Tag = 8 });
        comboBox.Items.Add(new ComboBoxItem { Content = loc.GetString("SettingsWindow.ActivationKeyFn"), Tag = 255 });
        // Note: Ctrl, Alt, Shift, Win keys cannot be used as activation keys
        // because they are modifier keys and bypass ProcessKey in KeyboardHookService
        comboBox.SelectedIndex = 0;
        stack.Children.Add(comboBox);

        var buttonPanel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0)
        };

        var okButton = new System.Windows.Controls.Button
        {
            Content = loc.GetString("SettingsWindow.OK"),
            Width = 80,
            Height = 30,
            Margin = new Thickness(0, 0, 10, 0),
            IsDefault = true
        };
        okButton.Click += (s, ev) => { dialog.DialogResult = true; dialog.Close(); };
        buttonPanel.Children.Add(okButton);

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = loc.GetString("SettingsWindow.Cancel"),
            Width = 80,
            Height = 30,
            IsCancel = true
        };
        cancelButton.Click += (s, ev) => { dialog.DialogResult = false; dialog.Close(); };
        buttonPanel.Children.Add(cancelButton);

        stack.Children.Add(buttonPanel);
        dialog.Content = stack;

        if (dialog.ShowDialog() == true && comboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            var vkCode = (int)selectedItem.Tag;

            if (_options.ActivationKeyProfiles.ContainsKey(vkCode))
            {
                MessageBox.Show("This activation key already exists!", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Add new profile with empty mappings
            _options.ActivationKeyProfiles[vkCode] = new Dictionary<int, int>();
            LoadActivationKeyProfiles();
            LoadKeyMappingActivationKeys(); // Refresh Key Mappings tab activation key selector
            _hasChanges = true;
        }
    }

    private void RemoveActivationKeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (ActivationKeyProfilesListBox.SelectedItem is ActivationKeyProfileDisplay selected)
        {
            if (_options.ActivationKeyProfiles.Count <= 1)
            {
                MessageBox.Show("Cannot remove the last activation key profile!", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to remove the '{selected.KeyName}' profile?\n\nThis will delete all key mappings for this activation key.",
                "Remove Activation Key Profile",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _options.ActivationKeyProfiles.Remove(selected.VkCode);
                LoadActivationKeyProfiles();
                LoadKeyMappingActivationKeys(); // Refresh Key Mappings tab activation key selector
                _hasChanges = true;
            }
        }
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        var loc = LocalizationManager.Instance;
        var result = MessageBox.Show(
            loc.GetString("SettingsWindow.ResetConfirmation"),
            loc.GetString("SettingsWindow.ResetConfirmationTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            // Reset to defaults
            var defaults = new TouchCursorOptions();
            _options.ActivationKey = defaults.ActivationKey;
            _options.ShowInNotificationArea = defaults.ShowInNotificationArea;
            _options.CheckForUpdates = defaults.CheckForUpdates;
            _options.BeepForMistakes = defaults.BeepForMistakes;
            _options.RolloverThresholdMs = defaults.RolloverThresholdMs;
            _options.UseEnableList = defaults.UseEnableList;
            _options.DisableProgs.Clear();
            _options.EnableProgs.Clear();
            _options.NeverTrainProgs.Clear();
            _options.OnlyTrainProgs.Clear();

            LoadOptionsToUI();
            _hasChanges = true;

            MessageBox.Show(
                loc.GetString("SettingsWindow.ResetComplete"),
                loc.GetString("SettingsWindow.ResetCompleteTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (_hasChanges)
        {
            _options.Save(TouchCursorOptions.GetDefaultConfigPath());
        }
        Hide(); // Hide to tray instead of closing
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_hasChanges)
        {
            var loc = LocalizationManager.Instance;
            var result = MessageBox.Show(
                loc.GetString("SettingsWindow.UnsavedChanges"),
                loc.GetString("SettingsWindow.UnsavedChangesTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                return;
            }

            // Restore original options
            RestoreOriginalOptions();
        }

        Hide(); // Hide to tray instead of closing
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

    private void RestoreOriginalOptions()
    {
        _options.ActivationKey = _originalOptions.ActivationKey;
        _options.ShowInNotificationArea = _originalOptions.ShowInNotificationArea;
        _options.CheckForUpdates = _originalOptions.CheckForUpdates;
        _options.BeepForMistakes = _originalOptions.BeepForMistakes;
        _options.RolloverThresholdMs = _originalOptions.RolloverThresholdMs;
        _options.UseEnableList = _originalOptions.UseEnableList;

        _options.DisableProgs.Clear();
        _options.DisableProgs.AddRange(_originalOptions.DisableProgs);

        _options.EnableProgs.Clear();
        _options.EnableProgs.AddRange(_originalOptions.EnableProgs);

        _options.NeverTrainProgs.Clear();
        _options.NeverTrainProgs.AddRange(_originalOptions.NeverTrainProgs);

        _options.OnlyTrainProgs.Clear();
        _options.OnlyTrainProgs.AddRange(_originalOptions.OnlyTrainProgs);
    }
}

// Helper class for displaying key mappings
public class KeyMappingDisplay : INotifyPropertyChanged
{
    public int SourceVkCode { get; set; }
    public string SourceKey { get; set; } = "";
    public string TargetKey { get; set; } = "";
    public string Description { get; set; } = "";

    private bool _ignoreRollover;
    public bool IgnoreRollover
    {
        get => _ignoreRollover;
        set
        {
            if (_ignoreRollover != value)
            {
                _ignoreRollover = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IgnoreRollover)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

// Simple input dialog for adding programs
// Helper class for displaying activation key profiles
public class ActivationKeyProfileDisplay
{
    public int VkCode { get; set; }
    public string KeyName { get; set; } = "";
    public string MappingCount { get; set; } = "";
}

public class InputDialog : Window
{
    private readonly System.Windows.Controls.TextBox _textBox;

    public string ResponseText => _textBox.Text;

    public InputDialog(string question, string title)
    {
        Title = title;
        Width = 400;
        Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var grid = new Grid { Margin = new Thickness(10) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var questionText = new TextBlock
        {
            Text = question,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(questionText, 0);
        grid.Children.Add(questionText);

        _textBox = new System.Windows.Controls.TextBox
        {
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(_textBox, 1);
        grid.Children.Add(_textBox);

        var buttonPanel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };

        var loc = LocalizationManager.Instance;

        var okButton = new System.Windows.Controls.Button
        {
            Content = loc.GetString("InputDialog.OK"),
            Width = 80,
            Height = 25,
            Margin = new Thickness(0, 0, 10, 0),
            IsDefault = true
        };
        okButton.Click += (s, e) => { DialogResult = true; Close(); };
        buttonPanel.Children.Add(okButton);

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = loc.GetString("InputDialog.Cancel"),
            Width = 80,
            Height = 25,
            IsCancel = true
        };
        cancelButton.Click += (s, e) => { DialogResult = false; Close(); };
        buttonPanel.Children.Add(cancelButton);

        Grid.SetRow(buttonPanel, 2);
        grid.Children.Add(buttonPanel);

        Content = grid;

        Loaded += (s, e) => _textBox.Focus();
    }
}
