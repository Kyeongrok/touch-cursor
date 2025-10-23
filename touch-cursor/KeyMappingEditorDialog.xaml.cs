// Copyright © 2025. Ported to C# from original C++ TouchCursor by Martin Stone.
// Original project licensed under GNU GPL v3.

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using touch_cursor.Models;

namespace touch_cursor;

public partial class KeyMappingEditorDialog : Window
{
    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12; // Alt
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;

    private int _sourceVkCode = 0;
    private int _targetVkCode = 0;
    private int _targetModifiers = 0;
    private bool _capturingSource = false;
    private bool _capturingTarget = false;

    public int SourceVkCode => _sourceVkCode;
    public int TargetVkCode => _targetVkCode;
    public int TargetModifiers => _targetModifiers;
    public string Description => DescriptionTextBox.Text;

    // Virtual key code to friendly name mapping
    private static readonly Dictionary<int, string> VkCodeNames = new()
    {
        { 0x08, "Backspace" }, { 0x09, "Tab" }, { 0x0D, "Enter" }, { 0x1B, "Escape" },
        { 0x20, "Space" }, { 0x21, "Page Up" }, { 0x22, "Page Down" }, { 0x23, "End" },
        { 0x24, "Home" }, { 0x25, "Left" }, { 0x26, "Up" }, { 0x27, "Right" },
        { 0x28, "Down" }, { 0x2D, "Insert" }, { 0x2E, "Delete" },
        { 0x30, "0" }, { 0x31, "1" }, { 0x32, "2" }, { 0x33, "3" }, { 0x34, "4" },
        { 0x35, "5" }, { 0x36, "6" }, { 0x37, "7" }, { 0x38, "8" }, { 0x39, "9" },
        { 0x41, "A" }, { 0x42, "B" }, { 0x43, "C" }, { 0x44, "D" }, { 0x45, "E" },
        { 0x46, "F" }, { 0x47, "G" }, { 0x48, "H" }, { 0x49, "I" }, { 0x4A, "J" },
        { 0x4B, "K" }, { 0x4C, "L" }, { 0x4D, "M" }, { 0x4E, "N" }, { 0x4F, "O" },
        { 0x50, "P" }, { 0x51, "Q" }, { 0x52, "R" }, { 0x53, "S" }, { 0x54, "T" },
        { 0x55, "U" }, { 0x56, "V" }, { 0x57, "W" }, { 0x58, "X" }, { 0x59, "Y" },
        { 0x5A, "Z" },
        { 0x70, "F1" }, { 0x71, "F2" }, { 0x72, "F3" }, { 0x73, "F4" },
        { 0x74, "F5" }, { 0x75, "F6" }, { 0x76, "F7" }, { 0x77, "F8" },
        { 0x78, "F9" }, { 0x79, "F10" }, { 0x7A, "F11" }, { 0x7B, "F12" },
        { 0xBA, ";" }, { 0xBB, "=" }, { 0xBC, "," }, { 0xBD, "-" },
        { 0xBE, "." }, { 0xBF, "/" }, { 0xC0, "`" }, { 0xDB, "[" },
        { 0xDC, "\\" }, { 0xDD, "]" }, { 0xDE, "'" }
    };

    public KeyMappingEditorDialog()
    {
        InitializeComponent();
        LoadKeyComboBoxes();
    }

    public KeyMappingEditorDialog(int sourceVk, int targetVk, int modifiers, string description) : this()
    {
        _sourceVkCode = sourceVk;
        _targetVkCode = targetVk;
        _targetModifiers = modifiers;

        SourceKeyTextBlock.Text = GetKeyName(sourceVk);
        TargetKeyTextBlock.Text = GetKeyNameWithModifiers(targetVk, modifiers);
        DescriptionTextBox.Text = description;
    }

    private void CaptureSourceKeyButton_Click(object sender, RoutedEventArgs e)
    {
        _capturingSource = true;
        _capturingTarget = false;
        CaptureSourceKeyButton.Content = "Press a key...";
        CaptureSourceKeyButton.IsEnabled = false;
        Focus();
    }

    private void CaptureTargetKeyButton_Click(object sender, RoutedEventArgs e)
    {
        _capturingTarget = true;
        _capturingSource = false;
        CaptureTargetKeyButton.Content = "Press a key...";
        CaptureTargetKeyButton.IsEnabled = false;
        Focus();
    }

    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (!_capturingSource && !_capturingTarget)
            return;

        e.Handled = true;

        // Convert WPF Key to Virtual Key Code
        int vkCode = KeyInterop.VirtualKeyFromKey(e.Key);

        // Ignore modifier keys themselves
        if (vkCode == VK_SHIFT || vkCode == VK_CONTROL || vkCode == VK_MENU ||
            vkCode == VK_LWIN || vkCode == VK_RWIN)
            return;

        if (_capturingSource)
        {
            _sourceVkCode = vkCode;
            SourceKeyTextBlock.Text = GetKeyName(vkCode);
            CaptureSourceKeyButton.Content = "Capture Key...";
            CaptureSourceKeyButton.IsEnabled = true;
            _capturingSource = false;
        }
        else if (_capturingTarget)
        {
            _targetVkCode = vkCode;

            // Capture modifier states
            _targetModifiers = 0;
            if ((GetKeyState(VK_SHIFT) & 0x8000) != 0)
                _targetModifiers |= (int)ModifierFlags.Shift;
            if ((GetKeyState(VK_CONTROL) & 0x8000) != 0)
                _targetModifiers |= (int)ModifierFlags.Ctrl;
            if ((GetKeyState(VK_MENU) & 0x8000) != 0)
                _targetModifiers |= (int)ModifierFlags.Alt;
            if ((GetKeyState(VK_LWIN) & 0x8000) != 0 || (GetKeyState(VK_RWIN) & 0x8000) != 0)
                _targetModifiers |= (int)ModifierFlags.Win;

            TargetKeyTextBlock.Text = GetKeyNameWithModifiers(vkCode, _targetModifiers);
            CaptureTargetKeyButton.Content = "Capture Key...";
            CaptureTargetKeyButton.IsEnabled = true;
            _capturingTarget = false;
        }
    }

    private string GetKeyName(int vkCode)
    {
        if (VkCodeNames.TryGetValue(vkCode, out var name))
            return name;
        return $"VK_{vkCode:X2}";
    }

    private string GetKeyNameWithModifiers(int vkCode, int modifiers)
    {
        var parts = new List<string>();

        if ((modifiers & (int)ModifierFlags.Ctrl) != 0)
            parts.Add("Ctrl");
        if ((modifiers & (int)ModifierFlags.Alt) != 0)
            parts.Add("Alt");
        if ((modifiers & (int)ModifierFlags.Shift) != 0)
            parts.Add("Shift");
        if ((modifiers & (int)ModifierFlags.Win) != 0)
            parts.Add("Win");

        parts.Add(GetKeyName(vkCode));

        return string.Join(" + ", parts);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (_sourceVkCode == 0)
        {
            System.Windows.MessageBox.Show("Please capture a source key.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_targetVkCode == 0)
        {
            System.Windows.MessageBox.Show("Please capture a target key.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void LoadKeyComboBoxes()
    {
        // Source keys (common keys used with activation key)
        var sourceKeys = new Dictionary<int, string>
        {
            { 0x41, "A" }, { 0x42, "B" }, { 0x43, "C" }, { 0x44, "D" }, { 0x45, "E" },
            { 0x46, "F" }, { 0x47, "G" }, { 0x48, "H" }, { 0x49, "I" }, { 0x4A, "J" },
            { 0x4B, "K" }, { 0x4C, "L" }, { 0x4D, "M" }, { 0x4E, "N" }, { 0x4F, "O" },
            { 0x50, "P" }, { 0x51, "Q" }, { 0x52, "R" }, { 0x53, "S" }, { 0x54, "T" },
            { 0x55, "U" }, { 0x56, "V" }, { 0x57, "W" }, { 0x58, "X" }, { 0x59, "Y" },
            { 0x5A, "Z" },
            { 0x30, "0" }, { 0x31, "1" }, { 0x32, "2" }, { 0x33, "3" }, { 0x34, "4" },
            { 0x35, "5" }, { 0x36, "6" }, { 0x37, "7" }, { 0x38, "8" }, { 0x39, "9" },
            { 0xBA, ";" }, { 0xBB, "=" }, { 0xBC, "," }, { 0xBD, "-" },
            { 0xBE, "." }, { 0xBF, "/" }, { 0xC0, "`" }, { 0xDB, "[" },
            { 0xDC, "\\" }, { 0xDD, "]" }, { 0xDE, "'" },
            { 0x08, "Backspace" }, { 0x09, "Tab" }, { 0x0D, "Enter" }, { 0x20, "Space" }
        };

        // Target keys (navigation and function keys)
        var targetKeys = new Dictionary<int, string>
        {
            { 0x25, "Left Arrow" }, { 0x26, "Up Arrow" }, { 0x27, "Right Arrow" }, { 0x28, "Down Arrow" },
            { 0x24, "Home" }, { 0x23, "End" }, { 0x21, "Page Up" }, { 0x22, "Page Down" },
            { 0x2D, "Insert" }, { 0x2E, "Delete" }, { 0x08, "Backspace" },
            { 0x0D, "Enter" }, { 0x1B, "Escape" }, { 0x09, "Tab" }, { 0x20, "Space" },
            { 0x70, "F1" }, { 0x71, "F2" }, { 0x72, "F3" }, { 0x73, "F4" },
            { 0x74, "F5" }, { 0x75, "F6" }, { 0x76, "F7" }, { 0x77, "F8" },
            { 0x78, "F9" }, { 0x79, "F10" }, { 0x7A, "F11" }, { 0x7B, "F12" },
            { 0x41, "A" }, { 0x42, "B" }, { 0x43, "C" }, { 0x44, "D" }, { 0x45, "E" },
            { 0x46, "F" }, { 0x47, "G" }, { 0x48, "H" }, { 0x49, "I" }, { 0x4A, "J" },
            { 0x4B, "K" }, { 0x4C, "L" }, { 0x4D, "M" }, { 0x4E, "N" }, { 0x4F, "O" },
            { 0x50, "P" }, { 0x51, "Q" }, { 0x52, "R" }, { 0x53, "S" }, { 0x54, "T" },
            { 0x55, "U" }, { 0x56, "V" }, { 0x57, "W" }, { 0x58, "X" }, { 0x59, "Y" },
            { 0x5A, "Z" },
            { 0x30, "0" }, { 0x31, "1" }, { 0x32, "2" }, { 0x33, "3" }, { 0x34, "4" },
            { 0x35, "5" }, { 0x36, "6" }, { 0x37, "7" }, { 0x38, "8" }, { 0x39, "9" }
        };

        // Populate source key combobox
        SourceKeyComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "(Select a key)", Tag = 0 });
        foreach (var kvp in sourceKeys.OrderBy(k => k.Value))
        {
            SourceKeyComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = kvp.Value, Tag = kvp.Key });
        }
        SourceKeyComboBox.SelectedIndex = 0;

        // Populate target key combobox
        TargetKeyComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "(Select a key)", Tag = 0 });
        foreach (var kvp in targetKeys.OrderBy(k => k.Value))
        {
            TargetKeyComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = kvp.Value, Tag = kvp.Key });
        }
        TargetKeyComboBox.SelectedIndex = 0;
    }

    private void SourceKeyComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (SourceKeyComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item && item.Tag is int vkCode && vkCode != 0)
        {
            _sourceVkCode = vkCode;
            SourceKeyTextBlock.Text = GetKeyName(vkCode);
        }
    }

    private void TargetKeyComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (TargetKeyComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item && item.Tag is int vkCode && vkCode != 0)
        {
            _targetVkCode = vkCode;

            // Capture current modifier keys
            _targetModifiers = 0;
            if ((GetKeyState(VK_CONTROL) & 0x8000) != 0)
                _targetModifiers |= (int)ModifierFlags.Ctrl;
            if ((GetKeyState(VK_SHIFT) & 0x8000) != 0)
                _targetModifiers |= (int)ModifierFlags.Shift;
            if ((GetKeyState(VK_MENU) & 0x8000) != 0)
                _targetModifiers |= (int)ModifierFlags.Alt;
            if ((GetKeyState(VK_LWIN) & 0x8000) != 0 || (GetKeyState(VK_RWIN) & 0x8000) != 0)
                _targetModifiers |= (int)ModifierFlags.Win;

            TargetKeyTextBlock.Text = GetKeyNameWithModifiers(vkCode, _targetModifiers);
        }
    }
}
