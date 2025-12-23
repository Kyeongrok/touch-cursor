using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;

namespace TouchCursor.Main.ViewModels;

public class KeyMappingsViewModel : BindableBase
{
    #region Fields

    private KeyMappingViewModel? _selectedKeyMapping;
    private int _selectedActivationKeyForMappings;
    private int _selectedTabIndex;

    #endregion

    #region Sub ViewModels

    public GeneralSettingsViewModel GeneralSettings { get; } = new();

    #endregion

    #region Properties

    public KeyMappingViewModel? SelectedKeyMapping
    {
        get => _selectedKeyMapping;
        set => SetProperty(ref _selectedKeyMapping, value);
    }

    public int SelectedActivationKeyForMappings
    {
        get => _selectedActivationKeyForMappings;
        set
        {
            if (SetProperty(ref _selectedActivationKeyForMappings, value))
                LoadKeyMappingsForActivationKey();
        }
    }

    #endregion

    #region Collections

    public ObservableCollection<KeyMappingViewModel> KeyMappings { get; } = new();

    #endregion

    #region Commands

    public ICommand AddKeyMappingCommand { get; }
    public ICommand EditKeyMappingCommand { get; }
    public ICommand RemoveKeyMappingCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AboutCommand { get; }

    #endregion

    #region Events

    public event Action? SaveRequested;
    public event Action? CancelRequested;
    public event Action? AboutRequested;
    public event Func<KeyMappingViewModel?, KeyMappingViewModel?>? EditKeyMappingRequested;
    public event Action? KeyMappingsChanged;

    #endregion

    public KeyMappingsViewModel()
    {
        AddKeyMappingCommand = new DelegateCommand(ExecuteAddKeyMapping);
        EditKeyMappingCommand = new DelegateCommand(ExecuteEditKeyMapping, () => SelectedKeyMapping != null)
            .ObservesProperty(() => SelectedKeyMapping);
        RemoveKeyMappingCommand = new DelegateCommand(ExecuteRemoveKeyMapping, () => SelectedKeyMapping != null)
            .ObservesProperty(() => SelectedKeyMapping);
        ResetToDefaultsCommand = new DelegateCommand(ExecuteResetToDefaults);
        SaveCommand = new DelegateCommand(ExecuteSave);
        CancelCommand = new DelegateCommand(ExecuteCancel);
        AboutCommand = new DelegateCommand(ExecuteAbout);
    }

    #region Command Implementations

    private void ExecuteAddKeyMapping()
    {
        var newMapping = EditKeyMappingRequested?.Invoke(null);
        if (newMapping != null)
        {
            KeyMappings.Add(newMapping);
            KeyMappingsChanged?.Invoke();
        }
    }

    private void ExecuteEditKeyMapping()
    {
        if (SelectedKeyMapping != null)
        {
            var editedMapping = EditKeyMappingRequested?.Invoke(SelectedKeyMapping);
            if (editedMapping != null)
            {
                var index = KeyMappings.IndexOf(SelectedKeyMapping);
                if (index >= 0)
                {
                    KeyMappings[index] = editedMapping;
                    SelectedKeyMapping = editedMapping;
                    KeyMappingsChanged?.Invoke();
                }
            }
        }
    }

    private void ExecuteRemoveKeyMapping()
    {
        if (SelectedKeyMapping != null)
        {
            KeyMappings.Remove(SelectedKeyMapping);
            KeyMappingsChanged?.Invoke();
        }
    }

    private void ExecuteResetToDefaults()
    {
        GeneralSettings.ResetToDefaults();
    }

    private void ExecuteSave()
    {
        SaveRequested?.Invoke();
    }

    private void ExecuteCancel()
    {
        CancelRequested?.Invoke();
    }

    private void ExecuteAbout()
    {
        AboutRequested?.Invoke();
    }

    #endregion

    #region Private Methods

    private void LoadKeyMappingsForActivationKey()
    {
        KeyMappings.Clear();
    }

    #endregion
}

public class KeyMappingViewModel : BindableBase
{
    private int _sourceVkCode;
    private string _sourceKey = "";
    private int _targetVkCode;
    private int _modifiers;
    private string _targetKey = "";
    private string _description = "";
    private bool _ignoreRollover;

    public int SourceVkCode
    {
        get => _sourceVkCode;
        set => SetProperty(ref _sourceVkCode, value);
    }

    public string SourceKey
    {
        get => _sourceKey;
        set => SetProperty(ref _sourceKey, value);
    }

    public int TargetVkCode
    {
        get => _targetVkCode;
        set => SetProperty(ref _targetVkCode, value);
    }

    public int Modifiers
    {
        get => _modifiers;
        set => SetProperty(ref _modifiers, value);
    }

    public string TargetKey
    {
        get => _targetKey;
        set => SetProperty(ref _targetKey, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public bool IgnoreRollover
    {
        get => _ignoreRollover;
        set => SetProperty(ref _ignoreRollover, value);
    }
}
