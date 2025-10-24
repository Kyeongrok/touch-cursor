// Copyright © 2025. Ported to C# from original C++ TouchCursor by Martin Stone.
// Original project licensed under GNU GPL v3.

using System.Diagnostics;
using touch_cursor.Models;

namespace touch_cursor.Services;

public class KeyMappingService
{
    private readonly TouchCursorOptions _options;

    private int _currentActivationKey = 0; // Which activation key is currently pressed
    private readonly HashSet<int> _mappedKeysHeld = new();
    private int _modifierState = 0;
    private bool _activationKeyUsedForMapping = false;
    private long _activationKeyPressTime = 0; // Timestamp when activation key was pressed (ticks)

    // Event for sending key events
    public event Action<int, bool, int>? SendKeyRequested;

    // Modifier key mappings
    private readonly Dictionary<int, int> _modifierKeys = new()
    {
        { 0x10, (int)ModifierFlags.Shift },  // VK_SHIFT
        { 0xA0, (int)ModifierFlags.Shift },  // VK_LSHIFT
        { 0xA1, (int)ModifierFlags.Shift },  // VK_RSHIFT
        { 0x11, (int)ModifierFlags.Ctrl },   // VK_CONTROL
        { 0xA2, (int)ModifierFlags.Ctrl },   // VK_LCONTROL
        { 0xA3, (int)ModifierFlags.Ctrl },   // VK_RCONTROL
        { 0x12, (int)ModifierFlags.Alt },    // VK_MENU
        { 0xA4, (int)ModifierFlags.Alt },    // VK_LMENU
        { 0xA5, (int)ModifierFlags.Alt },    // VK_RMENU
        { 0x5B, (int)ModifierFlags.Win },    // VK_LWIN
        { 0x5C, (int)ModifierFlags.Win },    // VK_RWIN
    };

    public KeyMappingService(TouchCursorOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Update modifier state - must be called for ALL keys including injected ones (like original C++ code)
    /// </summary>
    public void UpdateModifierState(int vkCode, bool isKeyDown, bool isKeyUp)
    {
        if (_modifierKeys.ContainsKey(vkCode))
        {
            if (isKeyDown)
            {
                _modifierState |= _modifierKeys[vkCode];
                Debug.WriteLine($"[UpdateModifierState] Key {vkCode} DOWN, modifierState now: {_modifierState:X}");
            }
            else if (isKeyUp)
            {
                _modifierState &= ~_modifierKeys[vkCode];
                Debug.WriteLine($"[UpdateModifierState] Key {vkCode} UP, modifierState now: {_modifierState:X}");
            }
        }
    }

    /// <summary>
    /// Process a key event and determine if it should be blocked or remapped.
    /// NOTE: Modifier keys should NOT be passed to this method (they are handled separately)
    /// </summary>
    /// <returns>True if the key event should be blocked, false to let it through</returns>
    public bool ProcessKey(int vkCode, bool isKeyDown, bool isKeyUp)
    {
        Debug.WriteLine($"[ProcessKey] vkCode={vkCode}, isKeyDown={isKeyDown}, isKeyUp={isKeyUp}, Enabled={_options.Enabled}, modifierState={_modifierState:X}");

        if (!_options.Enabled)
            return false;

        // Check if this is any of the configured activation keys
        if (_options.ActivationKeyProfiles.ContainsKey(vkCode))
        {
            Debug.WriteLine($"[ProcessKey] Activation key detected! vkCode={vkCode}, _currentActivationKey={_currentActivationKey}");
            if (isKeyDown && _currentActivationKey == 0)
            {
                _currentActivationKey = vkCode;
                _activationKeyUsedForMapping = false;
                _activationKeyPressTime = DateTime.Now.Ticks; // Record timestamp
                Debug.WriteLine($"[ProcessKey] Activation key pressed - blocking and setting _currentActivationKey={vkCode}, timestamp={_activationKeyPressTime}");
                return true; // Block the activation key press
            }
            else if (isKeyDown && _currentActivationKey != 0)
            {
                // Auto-repeat of activation key - block it (like original C++ state machine)
                Debug.WriteLine($"[ProcessKey] Activation key auto-repeat - blocking");
                return true;
            }
            else if (isKeyUp && _currentActivationKey == vkCode)
            {
                Debug.WriteLine($"[ProcessKey] Activation key released - releasing {_mappedKeysHeld.Count} held keys, wasUsedForMapping={_activationKeyUsedForMapping}");

                // Release all held mapped keys WITHOUT modifiers (original C++ behavior)
                foreach (var heldKey in _mappedKeysHeld)
                {
                    var targetVk = heldKey & 0xFFFF;
                    Debug.WriteLine($"[ProcessKey] Releasing held key: targetVk={targetVk}");
                    SendKeyRequested?.Invoke(targetVk, false, 0);
                }
                _mappedKeysHeld.Clear();

                // If the activation key was not used for any mapping, send it through
                if (!_activationKeyUsedForMapping)
                {
                    Debug.WriteLine($"[ProcessKey] Activation key was not used for mapping - sending key {vkCode}");
                    SendKeyRequested?.Invoke(vkCode, true, 0);
                    SendKeyRequested?.Invoke(vkCode, false, 0);
                }

                _currentActivationKey = 0;
                return true; // Block the activation key release
            }
        }

        // If activation key is down, check for mappings in the current profile
        if (_currentActivationKey != 0 &&
            _options.ActivationKeyProfiles.TryGetValue(_currentActivationKey, out var keyMappings) &&
            keyMappings.TryGetValue(vkCode, out var mappedKey))
        {
            var targetVk = mappedKey & 0xFFFF;
            var modifiers = (int)(mappedKey & 0xFFFF0000);
            Debug.WriteLine($"[ProcessKey] Mapping found! vkCode={vkCode} -> targetVk={targetVk}, modifiers={modifiers:X}");

            if (isKeyDown)
            {
                // Rollover detection: check if key was pressed too quickly after activation key
                var elapsedMs = (DateTime.Now.Ticks - _activationKeyPressTime) / TimeSpan.TicksPerMillisecond;
                Debug.WriteLine($"[ProcessKey] Elapsed time since activation: {elapsedMs}ms, threshold: {_options.RolloverThresholdMs}ms");

                if (elapsedMs <= _options.RolloverThresholdMs)
                {
                    // ROLLOVER DETECTED: treat as normal typing
                    Debug.WriteLine($"[ProcessKey] ROLLOVER DETECTED! Treating both keys as normal input");

                    // Send the activation key that was blocked earlier
                    SendKeyRequested?.Invoke(_currentActivationKey, true, 0);
                    SendKeyRequested?.Invoke(_currentActivationKey, false, 0);

                    // Reset activation state
                    _currentActivationKey = 0;
                    _activationKeyPressTime = 0;
                    _activationKeyUsedForMapping = false;

                    // Let the current key through (don't block it)
                    return false;
                }

                Debug.WriteLine($"[ProcessKey] Sending mapped key DOWN: targetVk={targetVk}, mappingModifiers={modifiers:X}, currentModifiers={_modifierState:X}");
                _activationKeyUsedForMapping = true; // Mark that we used the activation key for mapping
                // Only inject modifiers that aren't already pressed (like original C++ code)
                var effectiveModifiers = modifiers & ~_modifierState;
                Debug.WriteLine($"[ProcessKey] Effective modifiers to inject: {effectiveModifiers:X}");
                SendKeyRequested?.Invoke(targetVk, true, effectiveModifiers);
                _mappedKeysHeld.Add(mappedKey);

                if (_options.TrainingMode && _options.BeepForMistakes)
                {
                    // Beep to indicate successful mapping in training mode
                    Console.Beep(1000, 50);
                }
            }
            else if (isKeyUp && _mappedKeysHeld.Contains(mappedKey))
            {
                Debug.WriteLine($"[ProcessKey] Sending mapped key UP: targetVk={targetVk}");
                // Send the mapped key up WITHOUT modifiers (original C++ behavior)
                SendKeyRequested?.Invoke(targetVk, false, 0);
                _mappedKeysHeld.Remove(mappedKey);
            }

            return true; // Block the original key
        }

        // Training mode: beep for unmapped keys while activation key is down
        if (_currentActivationKey != 0 && _options.TrainingMode && _options.BeepForMistakes && isKeyDown)
        {
            Debug.WriteLine($"[ProcessKey] Training mode: unmapped key {vkCode} pressed while activation key down");
            Console.Beep(500, 100);
        }

        Debug.WriteLine($"[ProcessKey] Letting key through: vkCode={vkCode}");
        return false; // Let the key through
    }

    public void Reset()
    {
        _currentActivationKey = 0;
        _mappedKeysHeld.Clear();
        _modifierState = 0;
        _activationKeyUsedForMapping = false;
        _activationKeyPressTime = 0;
    }
}
