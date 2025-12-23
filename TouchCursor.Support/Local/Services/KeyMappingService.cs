// Copyright © 2025. Ported to C# from original C++ TouchCursor by Martin Stone.
// Original project licensed under GNU GPL v3.

using System.Diagnostics;
using TouchCursor.Support.Local.Helpers;

namespace TouchCursor.Support.Local.Services;

public class KeyMappingService : IKeyMappingService
{
    private readonly ITouchCursorOptions _options;

    private int _currentActivationKey = 0;
    private readonly HashSet<int> _mappedKeysHeld = new();
    private int _modifierState = 0;
    private bool _activationKeyUsedForMapping = false;
    private long _activationKeyPressTime = 0;

    // Mod Switch 상태
    private bool _modSwitchToggled = false;
    private int _toggledActivationKey = 0;

    public event Action<int, bool, int>? SendKeyRequested;
    public event Action<int, bool>? ActivationStateChanged;
    public event Action<int>? ActivationKeyPressed;

    private readonly Dictionary<int, int> _modifierKeys = new()
    {
        { 0x10, (int)ModifierFlags.Shift },
        { 0xA0, (int)ModifierFlags.Shift },
        { 0xA1, (int)ModifierFlags.Shift },
        { 0x11, (int)ModifierFlags.Ctrl },
        { 0xA2, (int)ModifierFlags.Ctrl },
        { 0xA3, (int)ModifierFlags.Ctrl },
        { 0x12, (int)ModifierFlags.Alt },
        { 0xA4, (int)ModifierFlags.Alt },
        { 0xA5, (int)ModifierFlags.Alt },
        { 0x5B, (int)ModifierFlags.Win },
        { 0x5C, (int)ModifierFlags.Win },
    };

    public KeyMappingService(ITouchCursorOptions options)
    {
        _options = options;
    }

    public void UpdateModifierState(int vkCode, bool isKeyDown, bool isKeyUp)
    {
        if (_modifierKeys.ContainsKey(vkCode))
        {
            if (isKeyDown)
            {
                _modifierState |= _modifierKeys[vkCode];
            }
            else if (isKeyUp)
            {
                _modifierState &= ~_modifierKeys[vkCode];
            }
        }
    }

    public bool ProcessKey(int vkCode, bool isKeyDown, bool isKeyUp)
    {
        // Mod Switch 토글 단축키 감지
        if (_options.ModSwitchEnabled && isKeyDown &&
            vkCode == _options.ModSwitchToggleKey &&
            (_modifierState & _options.ModSwitchToggleModifiers) == _options.ModSwitchToggleModifiers)
        {
            _modSwitchToggled = !_modSwitchToggled;

            if (_modSwitchToggled)
            {
                _toggledActivationKey = _options.ActivationKeyProfiles.Keys.FirstOrDefault(0x20);
                _currentActivationKey = _toggledActivationKey;
                _activationKeyUsedForMapping = false;
                _activationKeyPressTime = DateTime.Now.Ticks;
                ActivationStateChanged?.Invoke(_currentActivationKey, true);
                Console.Beep(1200, 100);
            }
            else
            {
                foreach (var heldKey in _mappedKeysHeld)
                {
                    var targetVk = heldKey & 0xFFFF;
                    SendKeyRequested?.Invoke(targetVk, false, 0);
                }
                _mappedKeysHeld.Clear();
                _currentActivationKey = 0;
                _toggledActivationKey = 0;
                ActivationStateChanged?.Invoke(0, false);
                Console.Beep(800, 100);
            }

            return true;
        }

        if (!_options.Enabled)
            return false;

        // 활성화 키 처리
        if (!_modSwitchToggled && _options.ActivationKeyProfiles.ContainsKey(vkCode))
        {
            if (isKeyDown && _currentActivationKey == 0)
            {
                _currentActivationKey = vkCode;
                _activationKeyUsedForMapping = false;
                _activationKeyPressTime = DateTime.Now.Ticks;
                // 대기 상태 알림 (초록색 표시)
                ActivationKeyPressed?.Invoke(vkCode);
                return true;
            }
            else if (isKeyDown && _currentActivationKey != 0)
            {
                return true;
            }
            else if (isKeyUp && _currentActivationKey == vkCode)
            {
                foreach (var heldKey in _mappedKeysHeld)
                {
                    var targetVk = heldKey & 0xFFFF;
                    SendKeyRequested?.Invoke(targetVk, false, 0);
                }
                _mappedKeysHeld.Clear();

                if (!_activationKeyUsedForMapping)
                {
                    SendKeyRequested?.Invoke(vkCode, true, 0);
                    SendKeyRequested?.Invoke(vkCode, false, 0);
                }

                // 항상 오버레이 숨기기 (매핑 사용 여부와 관계없이)
                ActivationStateChanged?.Invoke(0, false);

                _currentActivationKey = 0;
                return true;
            }
        }

        // 롤오버 처리 (매핑 여부와 관계없이 모든 키에 적용)
        if (_currentActivationKey != 0 && isKeyDown)
        {
            var elapsedMs = (DateTime.Now.Ticks - _activationKeyPressTime) / TimeSpan.TicksPerMillisecond;

            var isRolloverException = _options.RolloverExceptionKeys.TryGetValue(_currentActivationKey, out var exceptionKeys)
                                      && exceptionKeys.Contains(vkCode);

            // 홀드 딜레이 전에 키가 눌리면 롤오버 처리
            if (!isRolloverException && _options.RolloverEnabled && _options.ActivationKeyHoldDelayMs > 0)
            {
                if (elapsedMs < _options.ActivationKeyHoldDelayMs)
                {
                    // Space 먼저, 그 다음 원래 키를 순서대로 inject
                    SendKeyRequested?.Invoke(_currentActivationKey, true, 0);
                    SendKeyRequested?.Invoke(_currentActivationKey, false, 0);
                    SendKeyRequested?.Invoke(vkCode, true, 0);
                    SendKeyRequested?.Invoke(vkCode, false, 0);
                    _currentActivationKey = 0;
                    _activationKeyPressTime = 0;
                    _activationKeyUsedForMapping = false;
                    ActivationStateChanged?.Invoke(0, false);
                    return true; // 원래 키는 차단 (이미 inject했으므로)
                }
            }
        }

        // 키 매핑 처리
        if (_currentActivationKey != 0 &&
            _options.ActivationKeyProfiles.TryGetValue(_currentActivationKey, out var keyMappings) &&
            keyMappings.TryGetValue(vkCode, out var mappedKey))
        {
            var targetVk = mappedKey & 0xFFFF;
            var modifiers = (int)(mappedKey & 0xFFFF0000);

            if (isKeyDown)
            {
                // 매핑 사용 플래그 설정
                if (!_activationKeyUsedForMapping)
                {
                    _activationKeyUsedForMapping = true;
                    ActivationStateChanged?.Invoke(_currentActivationKey, true);
                }
                var effectiveModifiers = modifiers & ~_modifierState;
                SendKeyRequested?.Invoke(targetVk, true, effectiveModifiers);
                _mappedKeysHeld.Add(mappedKey);
            }
            else if (isKeyUp && _mappedKeysHeld.Contains(mappedKey))
            {
                SendKeyRequested?.Invoke(targetVk, false, 0);
                _mappedKeysHeld.Remove(mappedKey);
            }

            return true;
        }

        return false;
    }

    public void Reset()
    {
        _currentActivationKey = 0;
        _mappedKeysHeld.Clear();
        _modifierState = 0;
        _activationKeyUsedForMapping = false;
        _activationKeyPressTime = 0;
        _modSwitchToggled = false;
        _toggledActivationKey = 0;
    }

    public bool IsModSwitchToggled => _modSwitchToggled;
}
