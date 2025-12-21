using System.Windows;
using System.Windows.Controls;

namespace TouchCursor.Main.UI.Views;

public class KeyOption
{
    public int VkCode { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

[TemplatePart(Name = PART_KeyComboBox, Type = typeof(ComboBox))]
[TemplatePart(Name = PART_OkButton, Type = typeof(Button))]
[TemplatePart(Name = PART_CancelButton, Type = typeof(Button))]
public class ActivationKeyDialog : Control
{
    private const string PART_KeyComboBox = "PART_KeyComboBox";
    private const string PART_OkButton = "PART_OkButton";
    private const string PART_CancelButton = "PART_CancelButton";

    private ComboBox? _keyComboBox;
    private Button? _okButton;
    private Button? _cancelButton;

    public static readonly KeyOption[] AvailableKeys =
    [
        new() { VkCode = 0x20, DisplayName = "스페이스 (기본값)" },
        new() { VkCode = 0x14, DisplayName = "CapsLock" },
        new() { VkCode = 0x09, DisplayName = "Tab" },
        new() { VkCode = 0xA0, DisplayName = "왼쪽 Shift" },
        new() { VkCode = 0xA1, DisplayName = "오른쪽 Shift" },
        new() { VkCode = 0xA2, DisplayName = "왼쪽 Ctrl" },
        new() { VkCode = 0xA3, DisplayName = "오른쪽 Ctrl" },
        new() { VkCode = 0xA4, DisplayName = "왼쪽 Alt" },
        new() { VkCode = 0xA5, DisplayName = "오른쪽 Alt" },
        new() { VkCode = 0x1B, DisplayName = "Escape" },
        new() { VkCode = 0xC0, DisplayName = "` (백틱)" },
    ];

    public KeyOption? SelectedKey => _keyComboBox?.SelectedItem as KeyOption;

    public event EventHandler<KeyOption?>? OkClicked;
    public event EventHandler? CancelClicked;

    static ActivationKeyDialog()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ActivationKeyDialog),
            new FrameworkPropertyMetadata(typeof(ActivationKeyDialog)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Disconnect old handlers
        if (_okButton != null)
            _okButton.Click -= OnOkButtonClick;
        if (_cancelButton != null)
            _cancelButton.Click -= OnCancelButtonClick;

        // Get template parts
        _keyComboBox = GetTemplateChild(PART_KeyComboBox) as ComboBox;
        _okButton = GetTemplateChild(PART_OkButton) as Button;
        _cancelButton = GetTemplateChild(PART_CancelButton) as Button;

        // Setup ComboBox
        if (_keyComboBox != null)
        {
            _keyComboBox.ItemsSource = AvailableKeys;
            _keyComboBox.DisplayMemberPath = nameof(KeyOption.DisplayName);
            _keyComboBox.SelectedIndex = 0;
        }

        // Connect new handlers
        if (_okButton != null)
            _okButton.Click += OnOkButtonClick;
        if (_cancelButton != null)
            _cancelButton.Click += OnCancelButtonClick;
    }

    private void OnOkButtonClick(object sender, RoutedEventArgs e)
    {
        OkClicked?.Invoke(this, SelectedKey);
    }

    private void OnCancelButtonClick(object sender, RoutedEventArgs e)
    {
        CancelClicked?.Invoke(this, EventArgs.Empty);
    }
}
