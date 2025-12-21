using System.Windows;
using TouchCursor.Main.ViewModels;

namespace TouchCursor.Main.UI.Views;

public partial class KeyMappingEditorDialog : Window
{
    private readonly KeyMappingEditorViewModel _viewModel;

    public int SourceVkCode => _viewModel.SourceVkCode;
    public int TargetVkCode => _viewModel.TargetVkCode;
    public int TargetModifiers => _viewModel.TargetModifiers;
    public string Description => _viewModel.Description;

    public KeyMappingEditorDialog()
    {
        InitializeComponent();

        _viewModel = new KeyMappingEditorViewModel();
        EditorControl.ViewModel = _viewModel;

        _viewModel.OkRequested += OnOkRequested;
        _viewModel.CancelRequested += OnCancelRequested;
        _viewModel.ValidationFailed += OnValidationFailed;
    }

    public KeyMappingEditorDialog(int sourceVk, int targetVk, int modifiers, string description) : this()
    {
        _viewModel.SetExistingMapping(sourceVk, targetVk, modifiers, description);
    }

    private void OnOkRequested()
    {
        DialogResult = true;
        Close();
    }

    private void OnCancelRequested()
    {
        DialogResult = false;
        Close();
    }

    private bool OnValidationFailed(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }
}
