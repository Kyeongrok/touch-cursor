using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;

namespace TouchCursor.Main.ViewModels;

public class ExceptionAppsViewModel : BindableBase
{
    private string? _selectedProcessName;

    public ObservableCollection<string> ExcludedProcessNames { get; } = new();

    public string? SelectedProcessName
    {
        get => _selectedProcessName;
        set => SetProperty(ref _selectedProcessName, value);
    }

    public ICommand AddExceptionAppCommand { get; }
    public ICommand RemoveExceptionAppCommand { get; }

    public event Func<string?>? AddProcessRequested;
    public event Action? ExcludedProcessesChanged;

    public ExceptionAppsViewModel()
    {
        AddExceptionAppCommand = new DelegateCommand(ExecuteAddExceptionApp);
        RemoveExceptionAppCommand = new DelegateCommand(ExecuteRemoveExceptionApp, () => SelectedProcessName != null)
            .ObservesProperty(() => SelectedProcessName);
    }

    private void ExecuteAddExceptionApp()
    {
        var processName = AddProcessRequested?.Invoke();
        if (!string.IsNullOrWhiteSpace(processName))
        {
            if (!ExcludedProcessNames.Contains(processName, StringComparer.OrdinalIgnoreCase))
            {
                ExcludedProcessNames.Add(processName);
                ExcludedProcessesChanged?.Invoke();
            }
        }
    }

    private void ExecuteRemoveExceptionApp()
    {
        if (SelectedProcessName != null)
        {
            ExcludedProcessNames.Remove(SelectedProcessName);
            SelectedProcessName = null;
            ExcludedProcessesChanged?.Invoke();
        }
    }
}
