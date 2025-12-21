using System.Collections.ObjectModel;
using System.Windows.Input;

namespace TouchCursor.Main.ViewModels;

public class KeyMappingEditorViewModel : BindableBase
{
    private int _sourceVkCode;
    private int _targetVkCode;
    private int _targetModifiers;
    private string _sourceKeyDisplay = "(Not set)";
    private string _targetKeyDisplay = "(Not set)";
    private string _description = "";
    private bool _isCapturingSource;
    private bool _isCapturingTarget;
    private bool _ctrlModifier;
    private bool _altModifier;
    private bool _shiftModifier;
    private bool _winModifier;
    private KeyItem? _selectedSourceKey;
    private KeyItem? _selectedTargetKey;

    public int SourceVkCode
    {
        get => _sourceVkCode;
        set
        {
            if (SetProperty(ref _sourceVkCode, value))
                SourceKeyDisplay = GetKeyName(value);
        }
    }

    public int TargetVkCode
    {
        get => _targetVkCode;
        set
        {
            if (SetProperty(ref _targetVkCode, value))
                UpdateTargetDisplay();
        }
    }

    public int TargetModifiers
    {
        get => _targetModifiers;
        set
        {
            if (SetProperty(ref _targetModifiers, value))
            {
                UpdateModifierCheckboxes();
                UpdateTargetDisplay();
            }
        }
    }

    public string SourceKeyDisplay
    {
        get => _sourceKeyDisplay;
        set => SetProperty(ref _sourceKeyDisplay, value);
    }

    public string TargetKeyDisplay
    {
        get => _targetKeyDisplay;
        set => SetProperty(ref _targetKeyDisplay, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public bool IsCapturingSource
    {
        get => _isCapturingSource;
        set => SetProperty(ref _isCapturingSource, value);
    }

    public bool IsCapturingTarget
    {
        get => _isCapturingTarget;
        set => SetProperty(ref _isCapturingTarget, value);
    }

    public bool CtrlModifier
    {
        get => _ctrlModifier;
        set
        {
            if (SetProperty(ref _ctrlModifier, value))
                UpdateModifiersFromCheckboxes();
        }
    }

    public bool AltModifier
    {
        get => _altModifier;
        set
        {
            if (SetProperty(ref _altModifier, value))
                UpdateModifiersFromCheckboxes();
        }
    }

    public bool ShiftModifier
    {
        get => _shiftModifier;
        set
        {
            if (SetProperty(ref _shiftModifier, value))
                UpdateModifiersFromCheckboxes();
        }
    }

    public bool WinModifier
    {
        get => _winModifier;
        set
        {
            if (SetProperty(ref _winModifier, value))
                UpdateModifiersFromCheckboxes();
        }
    }

    public KeyItem? SelectedSourceKey
    {
        get => _selectedSourceKey;
        set
        {
            if (SetProperty(ref _selectedSourceKey, value) && value != null && value.VkCode != 0)
            {
                SourceVkCode = value.VkCode;
            }
        }
    }

    public KeyItem? SelectedTargetKey
    {
        get => _selectedTargetKey;
        set
        {
            if (SetProperty(ref _selectedTargetKey, value) && value != null && value.VkCode != 0)
            {
                TargetVkCode = value.VkCode;
            }
        }
    }

    public ObservableCollection<KeyItem> SourceKeys { get; } = new();
    public ObservableCollection<KeyItem> TargetKeys { get; } = new();

    public ICommand CaptureSourceKeyCommand { get; }
    public ICommand CaptureTargetKeyCommand { get; }
    public ICommand OkCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action? OkRequested;
    public event Action? CancelRequested;
    public event Func<string, string, bool>? ValidationFailed;

    public KeyMappingEditorViewModel()
    {
        CaptureSourceKeyCommand = new DelegateCommand(ExecuteCaptureSourceKey);
        CaptureTargetKeyCommand = new DelegateCommand(ExecuteCaptureTargetKey);
        OkCommand = new DelegateCommand(ExecuteOk);
        CancelCommand = new DelegateCommand(ExecuteCancel);

        LoadKeyLists();
    }

    public void SetExistingMapping(int sourceVk, int targetVk, int modifiers, string description)
    {
        _sourceVkCode = sourceVk;
        _targetVkCode = targetVk;
        _targetModifiers = modifiers;
        _description = description;

        SourceKeyDisplay = GetKeyName(sourceVk);
        UpdateModifierCheckboxes();
        UpdateTargetDisplay();

        RaisePropertyChanged(nameof(SourceVkCode));
        RaisePropertyChanged(nameof(TargetVkCode));
        RaisePropertyChanged(nameof(TargetModifiers));
        RaisePropertyChanged(nameof(Description));
    }

    public void OnKeyPressed(int vkCode, bool shiftPressed, bool ctrlPressed, bool altPressed, bool winPressed)
    {
        // Ignore modifier keys themselves
        if (vkCode == 0x10 || vkCode == 0x11 || vkCode == 0x12 ||
            vkCode == 0x5B || vkCode == 0x5C ||
            vkCode == 0xA0 || vkCode == 0xA1 || vkCode == 0xA2 ||
            vkCode == 0xA3 || vkCode == 0xA4 || vkCode == 0xA5)
            return;

        if (IsCapturingSource)
        {
            SourceVkCode = vkCode;
            IsCapturingSource = false;
        }
        else if (IsCapturingTarget)
        {
            TargetVkCode = vkCode;

            // Capture modifier states
            _targetModifiers = 0;
            if (shiftPressed) _targetModifiers |= 0x00010000;
            if (ctrlPressed) _targetModifiers |= 0x00020000;
            if (altPressed) _targetModifiers |= 0x00040000;
            if (winPressed) _targetModifiers |= 0x00080000;

            UpdateModifierCheckboxes();
            UpdateTargetDisplay();
            IsCapturingTarget = false;
        }
    }

    private void ExecuteCaptureSourceKey()
    {
        IsCapturingSource = true;
        IsCapturingTarget = false;
    }

    private void ExecuteCaptureTargetKey()
    {
        IsCapturingTarget = true;
        IsCapturingSource = false;
    }

    private void ExecuteOk()
    {
        if (_sourceVkCode == 0)
        {
            ValidationFailed?.Invoke("Validation Error", "Please capture a source key.");
            return;
        }

        if (_targetVkCode == 0)
        {
            ValidationFailed?.Invoke("Validation Error", "Please capture a target key.");
            return;
        }

        OkRequested?.Invoke();
    }

    private void ExecuteCancel()
    {
        CancelRequested?.Invoke();
    }

    private void UpdateModifierCheckboxes()
    {
        _shiftModifier = (_targetModifiers & 0x00010000) != 0;
        _ctrlModifier = (_targetModifiers & 0x00020000) != 0;
        _altModifier = (_targetModifiers & 0x00040000) != 0;
        _winModifier = (_targetModifiers & 0x00080000) != 0;

        RaisePropertyChanged(nameof(ShiftModifier));
        RaisePropertyChanged(nameof(CtrlModifier));
        RaisePropertyChanged(nameof(AltModifier));
        RaisePropertyChanged(nameof(WinModifier));
    }

    private void UpdateModifiersFromCheckboxes()
    {
        _targetModifiers = 0;
        if (_shiftModifier) _targetModifiers |= 0x00010000;
        if (_ctrlModifier) _targetModifiers |= 0x00020000;
        if (_altModifier) _targetModifiers |= 0x00040000;
        if (_winModifier) _targetModifiers |= 0x00080000;

        UpdateTargetDisplay();
    }

    private void UpdateTargetDisplay()
    {
        if (_targetVkCode != 0)
        {
            TargetKeyDisplay = GetKeyNameWithModifiers(_targetVkCode, _targetModifiers);
        }
    }

    private void LoadKeyLists()
    {
        // Source keys
        SourceKeys.Add(new KeyItem { VkCode = 0, Name = "(Select a key)" });
        foreach (var key in GetCommonKeys().OrderBy(k => k.Value))
        {
            SourceKeys.Add(new KeyItem { VkCode = key.Key, Name = key.Value });
        }

        // Target keys
        TargetKeys.Add(new KeyItem { VkCode = 0, Name = "(Select a key)" });
        foreach (var key in GetTargetKeys().OrderBy(k => k.Value))
        {
            TargetKeys.Add(new KeyItem { VkCode = key.Key, Name = key.Value });
        }
    }

    private Dictionary<int, string> GetCommonKeys()
    {
        return new Dictionary<int, string>
        {
            { 0x41, "A" }, { 0x42, "B" }, { 0x43, "C" }, { 0x44, "D" }, { 0x45, "E" },
            { 0x46, "F" }, { 0x47, "G" }, { 0x48, "H" }, { 0x49, "I" }, { 0x4A, "J" },
            { 0x4B, "K" }, { 0x4C, "L" }, { 0x4D, "M" }, { 0x4E, "N" }, { 0x4F, "O" },
            { 0x50, "P" }, { 0x51, "Q" }, { 0x52, "R" }, { 0x53, "S" }, { 0x54, "T" },
            { 0x55, "U" }, { 0x56, "V" }, { 0x57, "W" }, { 0x58, "X" }, { 0x59, "Y" },
            { 0x5A, "Z" },
            { 0x30, "0" }, { 0x31, "1" }, { 0x32, "2" }, { 0x33, "3" }, { 0x34, "4" },
            { 0x35, "5" }, { 0x36, "6" }, { 0x37, "7" }, { 0x38, "8" }, { 0x39, "9" },
            { 0xBA, ";" }, { 0xBB, "=" }, { 0xBC, "," }, { 0xBD, "-" },
            { 0xBE, "." }, { 0xBF, "/" }, { 0xC0, "`" }, { 0xDB, "[" },
            { 0xDC, "\\" }, { 0xDD, "]" }, { 0xDE, "'" }
        };
    }

    private Dictionary<int, string> GetTargetKeys()
    {
        return new Dictionary<int, string>
        {
            { 0x25, "Left Arrow" }, { 0x26, "Up Arrow" }, { 0x27, "Right Arrow" }, { 0x28, "Down Arrow" },
            { 0x24, "Home" }, { 0x23, "End" }, { 0x21, "Page Up" }, { 0x22, "Page Down" },
            { 0x2D, "Insert" }, { 0x2E, "Delete" }, { 0x08, "Backspace" },
            { 0x0D, "Enter" }, { 0x1B, "Escape" }, { 0x09, "Tab" }, { 0x20, "Space" },
            { 0x70, "F1" }, { 0x71, "F2" }, { 0x72, "F3" }, { 0x73, "F4" },
            { 0x74, "F5" }, { 0x75, "F6" }, { 0x76, "F7" }, { 0x77, "F8" },
            { 0x78, "F9" }, { 0x79, "F10" }, { 0x7A, "F11" }, { 0x7B, "F12" },
            { 0x41, "A" }, { 0x42, "B" }, { 0x43, "C" }, { 0x44, "D" }, { 0x45, "E" },
            { 0x46, "F" }, { 0x47, "G" }, { 0x48, "H" }, { 0x49, "I" }, { 0x4A, "J" },
            { 0x4B, "K" }, { 0x4C, "L" }, { 0x4D, "M" }, { 0x4E, "N" }, { 0x4F, "O" },
            { 0x50, "P" }, { 0x51, "Q" }, { 0x52, "R" }, { 0x53, "S" }, { 0x54, "T" },
            { 0x55, "U" }, { 0x56, "V" }, { 0x57, "W" }, { 0x58, "X" }, { 0x59, "Y" },
            { 0x5A, "Z" }
        };
    }

    private string GetKeyName(int vkCode)
    {
        return vkCode switch
        {
            0x08 => "Backspace", 0x09 => "Tab", 0x0D => "Enter", 0x1B => "Escape",
            0x20 => "Space", 0x21 => "Page Up", 0x22 => "Page Down", 0x23 => "End",
            0x24 => "Home", 0x25 => "Left", 0x26 => "Up", 0x27 => "Right",
            0x28 => "Down", 0x2D => "Insert", 0x2E => "Delete",
            >= 0x30 and <= 0x39 => ((char)vkCode).ToString(),
            >= 0x41 and <= 0x5A => ((char)vkCode).ToString(),
            0x70 => "F1", 0x71 => "F2", 0x72 => "F3", 0x73 => "F4",
            0x74 => "F5", 0x75 => "F6", 0x76 => "F7", 0x77 => "F8",
            0x78 => "F9", 0x79 => "F10", 0x7A => "F11", 0x7B => "F12",
            0xBA => ";", 0xBB => "=", 0xBC => ",", 0xBD => "-",
            0xBE => ".", 0xBF => "/", 0xC0 => "`",
            _ => $"VK_{vkCode:X2}"
        };
    }

    private string GetKeyNameWithModifiers(int vkCode, int modifiers)
    {
        var result = "";
        if ((modifiers & 0x00010000) != 0) result += "Shift+";
        if ((modifiers & 0x00020000) != 0) result += "Ctrl+";
        if ((modifiers & 0x00040000) != 0) result += "Alt+";
        if ((modifiers & 0x00080000) != 0) result += "Win+";
        return result + GetKeyName(vkCode);
    }
}

public class KeyItem
{
    public int VkCode { get; set; }
    public string Name { get; set; } = "";
}
