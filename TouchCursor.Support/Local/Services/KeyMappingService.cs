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
                // 아직 활성화된 것이 아님 - 매핑이 사용될 때 이벤트 발생
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
                else
                {
                    // 매핑이 사용된 경우에만 비활성화 이벤트 발생
                    ActivationStateChanged?.Invoke(0, false);
                }

                _currentActivationKey = 0;
                return true;
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
                var elapsedMs = (DateTime.Now.Ticks - _activationKeyPressTime) / TimeSpan.TicksPerMillisecond;

                var isRolloverException = _options.RolloverExceptionKeys.TryGetValue(_currentActivationKey, out var exceptionKeys)
                                          && exceptionKeys.Contains(vkCode);

                // 홀드 딜레이 확인
                if (!isRolloverException && _options.ActivationKeyHoldDelayMs > 0)
                {
                    if (elapsedMs < _options.ActivationKeyHoldDelayMs)
                    {
                        SendKeyRequested?.Invoke(_currentActivationKey, true, 0);
                        SendKeyRequested?.Invoke(_currentActivationKey, false, 0);
                        _currentActivationKey = 0;
                        _activationKeyPressTime = 0;
                        _activationKeyUsedForMapping = false;
                        // 활성화된 적 없으므로 이벤트 불필요
                        return false;
                    }
                }

                // 롤오버 감지
                if (!isRolloverException && _options.RolloverThresholdMs > 0)
                {
                    if (elapsedMs <= _options.RolloverThresholdMs)
                    {
                        SendKeyRequested?.Invoke(_currentActivationKey, true, 0);
                        SendKeyRequested?.Invoke(_currentActivationKey, false, 0);
                        _currentActivationKey = 0;
                        _activationKeyPressTime = 0;
                        _activationKeyUsedForMapping = false;
                        // 활성화된 적 없으므로 이벤트 불필요
                        return false;
                    }
                }

                // 첫 매핑 사용 시 활성화 이벤트 발생
                if (!_activationKeyUsedForMapping)
                {
                    _activationKeyUsedForMapping = true;
                    ActivationStateChanged?.Invoke(_currentActivationKey, true);
                }
                var effectiveModifiers = modifiers & ~_modifierState;
                SendKeyRequested?.Invoke(targetVk, true, effectiveModifiers);
                _mappedKeysHeld.Add(mappedKey);

                if (_options.TrainingMode && _options.BeepForMistakes)
                {
                    Console.Beep(1000, 50);
                }
            }
            else if (isKeyUp && _mappedKeysHeld.Contains(mappedKey))
            {
                SendKeyRequested?.Invoke(targetVk, false, 0);
                _mappedKeysHeld.Remove(mappedKey);
            }

            return true;
        }

        // 훈련 모드 비프음
        if (_currentActivationKey != 0 && _options.TrainingMode && _options.BeepForMistakes && isKeyDown)
        {
            Console.Beep(500, 100);
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
