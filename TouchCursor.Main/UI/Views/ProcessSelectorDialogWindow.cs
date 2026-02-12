using System.Windows;

namespace TouchCursor.Main.UI.Views;

[TemplatePart(Name = PART_DialogContent, Type = typeof(ProcessSelectorDialog))]
public class ProcessSelectorDialogWindow : Window
{
    private const string PART_DialogContent = "PART_DialogContent";

    private ProcessSelectorDialog? _dialogContent;

    public string? SelectedProcessName => _dialogContent?.SelectedProcessName;

    static ProcessSelectorDialogWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ProcessSelectorDialogWindow),
            new FrameworkPropertyMetadata(typeof(ProcessSelectorDialogWindow)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_dialogContent != null)
        {
            _dialogContent.OkClicked -= OnOkClicked;
            _dialogContent.CancelClicked -= OnCancelClicked;
        }

        _dialogContent = GetTemplateChild(PART_DialogContent) as ProcessSelectorDialog;

        if (_dialogContent != null)
        {
            _dialogContent.OkClicked += OnOkClicked;
            _dialogContent.CancelClicked += OnCancelClicked;
        }
    }

    private void OnOkClicked(object? sender, string? e)
    {
        if (!string.IsNullOrEmpty(e))
        {
            DialogResult = true;
            Close();
        }
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
