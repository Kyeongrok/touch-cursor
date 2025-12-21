using System.Windows;

namespace TouchCursor.Main.UI.Views;

[TemplatePart(Name = PART_DialogContent, Type = typeof(ActivationKeyDialog))]
public class ActivationKeyDialogWindow : Window
{
    private const string PART_DialogContent = "PART_DialogContent";

    private ActivationKeyDialog? _dialogContent;

    public KeyOption? SelectedKey => _dialogContent?.SelectedKey;

    static ActivationKeyDialogWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ActivationKeyDialogWindow),
            new FrameworkPropertyMetadata(typeof(ActivationKeyDialogWindow)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_dialogContent != null)
        {
            _dialogContent.OkClicked -= OnOkClicked;
            _dialogContent.CancelClicked -= OnCancelClicked;
        }

        _dialogContent = GetTemplateChild(PART_DialogContent) as ActivationKeyDialog;

        if (_dialogContent != null)
        {
            _dialogContent.OkClicked += OnOkClicked;
            _dialogContent.CancelClicked += OnCancelClicked;
        }
    }

    private void OnOkClicked(object? sender, KeyOption? e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
