using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using touch_cursor.Models;
using MessageBox = System.Windows.MessageBox;

namespace touch_cursor;

public partial class SettingsWindow : Window
{
    private readonly TouchCursorOptions _options;
    private readonly TouchCursorOptions _originalOptions;
    private bool _hasChanges = false;

    public SettingsWindow(TouchCursorOptions options)
    {
        InitializeComponent();

        _options = options;
        // Create a backup of original options
        _originalOptions = LoadBackup(options);

        LoadOptionsToUI();
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
            ActivationKey = options.ActivationKey,
            UseEnableList = options.UseEnableList,
            DisableProgs = new List<string>(options.DisableProgs),
            EnableProgs = new List<string>(options.EnableProgs),
            NeverTrainProgs = new List<string>(options.NeverTrainProgs),
            OnlyTrainProgs = new List<string>(options.OnlyTrainProgs)
        };
        return backup;
    }

    private void LoadOptionsToUI()
    {
        // General Tab
        SetActivationKeySelection(_options.ActivationKey);
        ShowInTrayCheckBox.IsChecked = _options.ShowInNotificationArea;
        CheckUpdatesCheckBox.IsChecked = _options.CheckForUpdates;
        BeepForMistakesCheckBox.IsChecked = _options.BeepForMistakes;

        // Key Mappings Tab
        LoadKeyMappings();

        // Program Lists Tab
        UseEnableListCheckBox.IsChecked = _options.UseEnableList;
        DisableProgsListBox.ItemsSource = new ObservableCollection<string>(_options.DisableProgs);
        EnableProgsListBox.ItemsSource = new ObservableCollection<string>(_options.EnableProgs);
    }

    private void SetActivationKeySelection(int vkCode)
    {
        foreach (ComboBoxItem item in ActivationKeyComboBox.Items)
        {
            if (item.Tag is string tagStr && int.Parse(tagStr) == vkCode)
            {
                ActivationKeyComboBox.SelectedItem = item;
                return;
            }
        }
        // Default to Space if not found
        ActivationKeyComboBox.SelectedIndex = 0;
    }

    private void LoadKeyMappings()
    {
        var mappings = new ObservableCollection<KeyMappingDisplay>();

        // Convert key mappings to display format
        foreach (var kvp in _options.KeyMapping)
        {
            var sourceKey = GetKeyName(kvp.Key);
            var targetVk = kvp.Value & 0xFFFF;
            var modifiers = (int)(kvp.Value & 0xFFFF0000);
            var targetKey = GetKeyNameWithModifiers(targetVk, modifiers);
            var description = GetMappingDescription(kvp.Key, targetVk, modifiers);

            mappings.Add(new KeyMappingDisplay
            {
                SourceKey = sourceKey,
                TargetKey = targetKey,
                Description = description
            });
        }

        KeyMappingsDataGrid.ItemsSource = mappings;
    }

    private string GetKeyName(int vkCode)
    {
        return vkCode switch
        {
            0x49 => "I",
            0x4A => "J",
            0x4B => "K",
            0x4C => "L",
            0x55 => "U",
            0x4F => "O",
            0x48 => "H",
            0x50 => "P",
            0x4D => "M",
            0xBC => ",",
            0x4E => "N",
            0xBE => ".",
            _ => $"VK_{vkCode:X2}"
        };
    }

    private string GetKeyNameWithModifiers(int vkCode, int modifiers)
    {
        var modifierStr = "";
        if ((modifiers & (int)ModifierFlags.Ctrl) != 0) modifierStr += "Ctrl+";
        if ((modifiers & (int)ModifierFlags.Shift) != 0) modifierStr += "Shift+";
        if ((modifiers & (int)ModifierFlags.Alt) != 0) modifierStr += "Alt+";
        if ((modifiers & (int)ModifierFlags.Win) != 0) modifierStr += "Win+";

        var keyName = vkCode switch
        {
            0x26 => "Up",
            0x28 => "Down",
            0x25 => "Left",
            0x27 => "Right",
            0x24 => "Home",
            0x23 => "End",
            0x21 => "Page Up",
            0x22 => "Page Down",
            0x08 => "Backspace",
            0x2E => "Delete",
            _ => $"VK_{vkCode:X2}"
        };

        return modifierStr + keyName;
    }

    private string GetMappingDescription(int sourceVk, int targetVk, int modifiers)
    {
        return (sourceVk, targetVk) switch
        {
            (0x49, 0x26) => "Move cursor up",
            (0x4B, 0x28) => "Move cursor down",
            (0x4A, 0x25) => "Move cursor left",
            (0x4C, 0x27) => "Move cursor right",
            (0x55, 0x24) => "Jump to beginning of line",
            (0x4F, 0x23) => "Jump to end of line",
            (0x48, 0x21) => "Scroll page up",
            (0x50, 0x22) => "Scroll page down",
            (0x4D, 0x08) => "Delete character before cursor",
            (0xBC, 0x2E) => "Delete character after cursor",
            (0x4E, 0x25) when (modifiers & (int)ModifierFlags.Ctrl) != 0 => "Move cursor one word left",
            (0xBE, 0x27) when (modifiers & (int)ModifierFlags.Ctrl) != 0 => "Move cursor one word right",
            _ => "Custom mapping"
        };
    }

    private void ActivationKeyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ActivationKeyComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tagStr)
        {
            _options.ActivationKey = int.Parse(tagStr);
            _hasChanges = true;
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

    private void UseEnableListCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_options == null) return;
        _options.UseEnableList = UseEnableListCheckBox.IsChecked == true;
        _hasChanges = true;
    }

    private void AddDisableProg_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new InputDialog("Enter program name (e.g., notepad.exe):", "Add Program");
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
        var dialog = new InputDialog("Enter program name (e.g., notepad.exe):", "Add Program");
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

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to reset all settings to defaults?\n\nThis will reset:\n• Activation key\n• Key mappings\n• Program lists\n• All preferences",
            "Reset to Defaults",
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
            _options.UseEnableList = defaults.UseEnableList;
            _options.DisableProgs.Clear();
            _options.EnableProgs.Clear();
            _options.NeverTrainProgs.Clear();
            _options.OnlyTrainProgs.Clear();

            LoadOptionsToUI();
            _hasChanges = true;

            MessageBox.Show("Settings have been reset to defaults.", "Reset Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (_hasChanges)
        {
            _options.Save(TouchCursorOptions.GetDefaultConfigPath());
        }
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_hasChanges)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to discard them?",
                "Unsaved Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                return;
            }

            // Restore original options
            RestoreOriginalOptions();
        }

        DialogResult = false;
        Close();
    }

    private void RestoreOriginalOptions()
    {
        _options.ActivationKey = _originalOptions.ActivationKey;
        _options.ShowInNotificationArea = _originalOptions.ShowInNotificationArea;
        _options.CheckForUpdates = _originalOptions.CheckForUpdates;
        _options.BeepForMistakes = _originalOptions.BeepForMistakes;
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
public class KeyMappingDisplay
{
    public string SourceKey { get; set; } = "";
    public string TargetKey { get; set; } = "";
    public string Description { get; set; } = "";
}

// Simple input dialog for adding programs
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

        var okButton = new System.Windows.Controls.Button
        {
            Content = "OK",
            Width = 80,
            Height = 25,
            Margin = new Thickness(0, 0, 10, 0),
            IsDefault = true
        };
        okButton.Click += (s, e) => { DialogResult = true; Close(); };
        buttonPanel.Children.Add(okButton);

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "Cancel",
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
