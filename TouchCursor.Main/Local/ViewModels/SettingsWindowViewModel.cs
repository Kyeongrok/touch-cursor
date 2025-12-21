using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;

namespace TouchCursor.Main.ViewModels;

public class SettingsWindowViewModel : BindableBase
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

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

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

    #endregion

    public SettingsWindowViewModel()
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
                }
            }
        }
    }

    private void ExecuteRemoveKeyMapping()
    {
        if (SelectedKeyMapping != null)
        {
            KeyMappings.Remove(SelectedKeyMapping);
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

public class ActivationKeyProfileViewModel : BindableBase
{
    private int _vkCode;
    private string _keyName = "";
    private int _mappingCount;

    public int VkCode
    {
        get => _vkCode;
        set => SetProperty(ref _vkCode, value);
    }

    public string KeyName
    {
        get => _keyName;
        set => SetProperty(ref _keyName, value);
    }

    public int MappingCount
    {
        get => _mappingCount;
        set => SetProperty(ref _mappingCount, value);
    }

    public string DisplayText => $"{KeyName} ({MappingCount} mappings)";
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

public class LanguageItem
{
    public string Code { get; set; } = "";
    public string NativeName { get; set; } = "";
}
