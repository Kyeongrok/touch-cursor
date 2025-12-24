using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using TouchCursor.Support.Local.Helpers;

namespace TouchCursor.Main.ViewModels;

public class GeneralSettingsViewModel : BindableBase
{
    #region Fields

    private bool _isEnabled;
    private bool _runAtStartup;
    private bool _showInTray = true;
    private bool _beepForMistakes;
    private int _holdDelayMs;
    private bool _rolloverEnabled = true;
    private string _selectedLanguage = "en";
    private ActivationKeyProfileViewModel? _selectedActivationKeyProfile;
    private OverlayPosition _overlayPosition = OverlayPosition.BottomRight;
    private bool _showActivationOverlay = true;

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

    public bool BeepForMistakes
    {
        get => _beepForMistakes;
        set => SetProperty(ref _beepForMistakes, value);
    }

    public int HoldDelayMs
    {
        get => _holdDelayMs;
        set
        {
            if (SetProperty(ref _holdDelayMs, value))
                HoldDelayMsChanged?.Invoke();
        }
    }

    public bool RolloverEnabled
    {
        get => _rolloverEnabled;
        set => SetProperty(ref _rolloverEnabled, value);
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

    public OverlayPosition OverlayPosition
    {
        get => _overlayPosition;
        set
        {
            if (SetProperty(ref _overlayPosition, value))
                OverlayPositionChanged?.Invoke();
        }
    }

    public bool ShowActivationOverlay
    {
        get => _showActivationOverlay;
        set => SetProperty(ref _showActivationOverlay, value);
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
    public event Action? RunAtStartupChanged;
    public event Action? LanguageChanged;
    public event Action? OverlayPositionChanged;
    public event Action? HoldDelayMsChanged;
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
        RolloverEnabled = true;
        ShowInTray = true;
        BeepForMistakes = false;
        ShowActivationOverlay = true;
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

public class LanguageItem
{
    public string Code { get; set; } = "";
    public string NativeName { get; set; } = "";
}
