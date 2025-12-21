using System.Collections.ObjectModel;
using System.Windows.Input;
using TouchCursor.Main.Core;

namespace TouchCursor.Main.ViewModels;

public class SettingsWindowViewModel : ViewModelBase
{
    #region Fields

    private bool _isEnabled;
    private bool _trainingMode;
    private bool _runAtStartup;
    private bool _showInTray = true;
    private bool _checkUpdates = true;
    private bool _beepForMistakes;
    private bool _typingAnalyticsEnabled;
    private bool _autoSwitchToEnglish;
    private int _holdDelayMs;
    private int _rolloverThresholdMs = 50;
    private bool _useEnableList;
    private string _selectedLanguage = "en";
    private ActivationKeyProfileViewModel? _selectedActivationKeyProfile;
    private KeyMappingViewModel? _selectedKeyMapping;
    private int _selectedActivationKeyForMappings;

    #endregion

    #region Properties

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
                OnEnabledChanged();
        }
    }

    public bool TrainingMode
    {
        get => _trainingMode;
        set
        {
            if (SetProperty(ref _trainingMode, value))
            {
                BeepForMistakes = value;
                OnTrainingModeChanged();
            }
        }
    }

    public bool RunAtStartup
    {
        get => _runAtStartup;
        set
        {
            if (SetProperty(ref _runAtStartup, value))
                OnRunAtStartupChanged();
        }
    }

    public bool ShowInTray
    {
        get => _showInTray;
        set => SetProperty(ref _showInTray, value);
    }

    public bool CheckUpdates
    {
        get => _checkUpdates;
        set => SetProperty(ref _checkUpdates, value);
    }

    public bool BeepForMistakes
    {
        get => _beepForMistakes;
        set => SetProperty(ref _beepForMistakes, value);
    }

    public bool TypingAnalyticsEnabled
    {
        get => _typingAnalyticsEnabled;
        set => SetProperty(ref _typingAnalyticsEnabled, value);
    }

    public bool AutoSwitchToEnglish
    {
        get => _autoSwitchToEnglish;
        set => SetProperty(ref _autoSwitchToEnglish, value);
    }

    public int HoldDelayMs
    {
        get => _holdDelayMs;
        set => SetProperty(ref _holdDelayMs, value);
    }

    public int RolloverThresholdMs
    {
        get => _rolloverThresholdMs;
        set => SetProperty(ref _rolloverThresholdMs, value);
    }

    public bool UseEnableList
    {
        get => _useEnableList;
        set => SetProperty(ref _useEnableList, value);
    }

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value))
                OnLanguageChanged();
        }
    }

    public ActivationKeyProfileViewModel? SelectedActivationKeyProfile
    {
        get => _selectedActivationKeyProfile;
        set => SetProperty(ref _selectedActivationKeyProfile, value);
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

    public ObservableCollection<ActivationKeyProfileViewModel> ActivationKeyProfiles { get; } = new();
    public ObservableCollection<KeyMappingViewModel> KeyMappings { get; } = new();
    public ObservableCollection<string> DisableProgs { get; } = new();
    public ObservableCollection<string> EnableProgs { get; } = new();
    public ObservableCollection<LanguageItem> AvailableLanguages { get; } = new();

    #endregion

    #region Commands

    public ICommand AddActivationKeyProfileCommand { get; }
    public ICommand RemoveActivationKeyProfileCommand { get; }
    public ICommand AddKeyMappingCommand { get; }
    public ICommand EditKeyMappingCommand { get; }
    public ICommand RemoveKeyMappingCommand { get; }
    public ICommand AddDisableProgCommand { get; }
    public ICommand RemoveDisableProgCommand { get; }
    public ICommand AddEnableProgCommand { get; }
    public ICommand RemoveEnableProgCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    #endregion

    #region Events

    public event Action? EnabledChanged;
    public event Action? TrainingModeChanged;
    public event Action? RunAtStartupChanged;
    public event Action? LanguageChanged;
    public event Action? SaveRequested;
    public event Action? CancelRequested;
    public event Func<string?, string?>? AddProgramRequested;
    public event Func<ActivationKeyProfileViewModel?>? AddActivationKeyRequested;
    public event Func<KeyMappingViewModel?, KeyMappingViewModel?>? EditKeyMappingRequested;

    #endregion

    public SettingsWindowViewModel()
    {
        // Initialize commands
        AddActivationKeyProfileCommand = new RelayCommand(ExecuteAddActivationKeyProfile);
        RemoveActivationKeyProfileCommand = new RelayCommand(ExecuteRemoveActivationKeyProfile, () => SelectedActivationKeyProfile != null);
        AddKeyMappingCommand = new RelayCommand(ExecuteAddKeyMapping);
        EditKeyMappingCommand = new RelayCommand(ExecuteEditKeyMapping, () => SelectedKeyMapping != null);
        RemoveKeyMappingCommand = new RelayCommand(ExecuteRemoveKeyMapping, () => SelectedKeyMapping != null);
        AddDisableProgCommand = new RelayCommand(ExecuteAddDisableProg);
        RemoveDisableProgCommand = new RelayCommand<string>(ExecuteRemoveDisableProg);
        AddEnableProgCommand = new RelayCommand(ExecuteAddEnableProg);
        RemoveEnableProgCommand = new RelayCommand<string>(ExecuteRemoveEnableProg);
        ResetToDefaultsCommand = new RelayCommand(ExecuteResetToDefaults);
        SaveCommand = new RelayCommand(ExecuteSave);
        CancelCommand = new RelayCommand(ExecuteCancel);
    }

    #region Command Implementations

    private void ExecuteAddActivationKeyProfile()
    {
        var newProfile = AddActivationKeyRequested?.Invoke();
        if (newProfile != null)
        {
            ActivationKeyProfiles.Add(newProfile);
        }
    }

    private void ExecuteRemoveActivationKeyProfile()
    {
        if (SelectedActivationKeyProfile != null && ActivationKeyProfiles.Count > 1)
        {
            ActivationKeyProfiles.Remove(SelectedActivationKeyProfile);
        }
    }

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

    private void ExecuteAddDisableProg()
    {
        var program = AddProgramRequested?.Invoke(null);
        if (!string.IsNullOrWhiteSpace(program) && !DisableProgs.Contains(program))
        {
            DisableProgs.Add(program);
        }
    }

    private void ExecuteRemoveDisableProg(string? program)
    {
        if (program != null)
        {
            DisableProgs.Remove(program);
        }
    }

    private void ExecuteAddEnableProg()
    {
        var program = AddProgramRequested?.Invoke(null);
        if (!string.IsNullOrWhiteSpace(program) && !EnableProgs.Contains(program))
        {
            EnableProgs.Add(program);
        }
    }

    private void ExecuteRemoveEnableProg(string? program)
    {
        if (program != null)
        {
            EnableProgs.Remove(program);
        }
    }

    private void ExecuteResetToDefaults()
    {
        // Reset all settings to defaults
        HoldDelayMs = 0;
        RolloverThresholdMs = 50;
        ShowInTray = true;
        CheckUpdates = true;
        BeepForMistakes = false;
        UseEnableList = false;
        DisableProgs.Clear();
        EnableProgs.Clear();
    }

    private void ExecuteSave()
    {
        SaveRequested?.Invoke();
    }

    private void ExecuteCancel()
    {
        CancelRequested?.Invoke();
    }

    #endregion

    #region Event Raisers

    protected virtual void OnEnabledChanged() => EnabledChanged?.Invoke();
    protected virtual void OnTrainingModeChanged() => TrainingModeChanged?.Invoke();
    protected virtual void OnRunAtStartupChanged() => RunAtStartupChanged?.Invoke();
    protected virtual void OnLanguageChanged() => LanguageChanged?.Invoke();

    #endregion

    #region Private Methods

    private void LoadKeyMappingsForActivationKey()
    {
        KeyMappings.Clear();
        // This will be called when the selected activation key changes
        // The actual loading logic will be implemented by the host application
    }

    #endregion
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
    public void Execute(object? parameter) => _execute((T?)parameter);
}

public class ActivationKeyProfileViewModel : ViewModelBase
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

public class KeyMappingViewModel : ViewModelBase
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
