using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;

namespace TouchCursor.Main.ViewModels;

public class GeneralSettingsViewModel : BindableBase
{
    #region Fields

    private bool _isEnabled;
    private bool _trainingMode;
    private bool _runAtStartup;
    private bool _showInTray = true;
    private bool _checkUpdates = true;
    private bool _beepForMistakes;
    private int _holdDelayMs;
    private int _rolloverThresholdMs = 50;
    private string _selectedLanguage = "en";
    private ActivationKeyProfileViewModel? _selectedActivationKeyProfile;

    #endregion

    #region Properties

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
                EnabledChanged?.Invoke();
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
                TrainingModeChanged?.Invoke();
            }
        }
    }

    public bool RunAtStartup
    {
        get => _runAtStartup;
        set
        {
            if (SetProperty(ref _runAtStartup, value))
                RunAtStartupChanged?.Invoke();
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

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value))
                LanguageChanged?.Invoke();
        }
    }

    public ActivationKeyProfileViewModel? SelectedActivationKeyProfile
    {
        get => _selectedActivationKeyProfile;
        set => SetProperty(ref _selectedActivationKeyProfile, value);
    }

    #endregion

    #region Collections

    public ObservableCollection<ActivationKeyProfileViewModel> ActivationKeyProfiles { get; } = new();
    public ObservableCollection<LanguageItem> AvailableLanguages { get; } = new();

    #endregion

    #region Commands

    public ICommand AddActivationKeyProfileCommand { get; }
    public ICommand RemoveActivationKeyProfileCommand { get; }
    public ICommand ChangeLanguageCommand { get; }

    #endregion

    #region Events

    public event Action? EnabledChanged;
    public event Action? TrainingModeChanged;
    public event Action? RunAtStartupChanged;
    public event Action? LanguageChanged;
    public event Func<ActivationKeyProfileViewModel?>? AddActivationKeyRequested;

    #endregion

    public GeneralSettingsViewModel()
    {
        AddActivationKeyProfileCommand = new DelegateCommand(ExecuteAddActivationKeyProfile);
        RemoveActivationKeyProfileCommand = new DelegateCommand(ExecuteRemoveActivationKeyProfile, () => SelectedActivationKeyProfile != null)
            .ObservesProperty(() => SelectedActivationKeyProfile);
        ChangeLanguageCommand = new DelegateCommand<string>(ExecuteChangeLanguage);
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

    private void ExecuteChangeLanguage(string languageCode)
    {
        if (!string.IsNullOrEmpty(languageCode))
        {
            SelectedLanguage = languageCode;
        }
    }

    #endregion

    #region Public Methods

    public void ResetToDefaults()
    {
        HoldDelayMs = 0;
        RolloverThresholdMs = 50;
        ShowInTray = true;
        CheckUpdates = true;
        BeepForMistakes = false;
    }

    #endregion
}
