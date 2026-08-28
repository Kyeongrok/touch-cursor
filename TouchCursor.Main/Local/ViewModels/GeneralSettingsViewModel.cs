using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using TouchCursor.Support.Local.Helpers;
using TouchCursor.Support.Local.Services;

namespace TouchCursor.Main.ViewModels;

public class GeneralSettingsViewModel : BindableBase
{
    #region Fields

    private bool _isEnabled;
    private bool _isStartupRegistered;
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

    /// <summary>시작프로그램 폴더에 바로가기가 등록되어 있는지 여부.</summary>
    public bool IsStartupRegistered
    {
        get => _isStartupRegistered;
        private set
        {
            if (SetProperty(ref _isStartupRegistered, value))
            {
                RaisePropertyChanged(nameof(StartupStatusText));
                RunAtStartupChanged?.Invoke();
            }
        }
    }

    public string StartupStatusText => IsStartupRegistered
        ? $"등록됨: {StartupShortcutService.ShortcutPath}"
        : "시작프로그램에 등록되어 있지 않습니다.";

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
    public ICommand RegisterStartupCommand { get; }
    public ICommand UnregisterStartupCommand { get; }
    public ICommand OpenStartupFolderCommand { get; }

    #endregion

    #region Events

    public event Action? EnabledChanged;
    public event Action? RunAtStartupChanged;
    /// <summary>시작프로그램 등록/해제 결과를 사용자에게 보여달라는 요청. (메시지, 성공여부)</summary>
    public event Action<string, bool>? StartupRegistrationMessage;
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
        RegisterStartupCommand = new DelegateCommand(ExecuteRegisterStartup);
        UnregisterStartupCommand = new DelegateCommand(ExecuteUnregisterStartup, () => IsStartupRegistered)
            .ObservesProperty(() => IsStartupRegistered);

        OpenStartupFolderCommand = new DelegateCommand(ExecuteOpenStartupFolder);

        RefreshStartupRegistrationState();
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

    private void ExecuteRegisterStartup()
    {
        try
        {
            StartupShortcutService.Register();
            RefreshStartupRegistrationState();
            StartupRegistrationMessage?.Invoke(
                $"시작프로그램에 등록했습니다.\n{StartupShortcutService.ShortcutPath}", true);
        }
        catch (Exception ex)
        {
            RefreshStartupRegistrationState();
            StartupRegistrationMessage?.Invoke($"시작프로그램 등록에 실패했습니다.\n{ex.Message}", false);
        }
    }

    private void ExecuteUnregisterStartup()
    {
        try
        {
            StartupShortcutService.Unregister();
            RefreshStartupRegistrationState();
            StartupRegistrationMessage?.Invoke("시작프로그램 등록을 해제했습니다.", true);
        }
        catch (Exception ex)
        {
            RefreshStartupRegistrationState();
            StartupRegistrationMessage?.Invoke($"시작프로그램 해제에 실패했습니다.\n{ex.Message}", false);
        }
    }

    private void ExecuteOpenStartupFolder()
    {
        try
        {
            StartupShortcutService.OpenFolder();
            // 폴더 상태가 바뀌었을 수 있으니 다시 확인한다.
            RefreshStartupRegistrationState();
        }
        catch (Exception ex)
        {
            StartupRegistrationMessage?.Invoke($"시작프로그램 폴더를 열지 못했습니다.\n{ex.Message}", false);
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

    /// <summary>실제 시작프로그램 폴더 상태를 다시 읽어 UI에 반영한다.</summary>
    public void RefreshStartupRegistrationState()
    {
        IsStartupRegistered = StartupShortcutService.IsRegistered();
    }

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
