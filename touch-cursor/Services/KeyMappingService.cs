// Copyright © 2025. Ported to C# from original C++ TouchCursor by Martin Stone.
// Original project licensed under GNU GPL v3.

using touch_cursor.Models;

namespace touch_cursor.Services;

public class KeyMappingService
{
    private readonly TouchCursorOptions _options;

    private bool _activationKeyDown = false;
    private readonly HashSet<int> _mappedKeysHeld = new();
    private int _modifierState = 0;

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
    /// Process a key event and determine if it should be blocked or remapped.
    /// </summary>
    /// <returns>True if the key event should be blocked, false to let it through</returns>
    public bool ProcessKey(int vkCode, bool isKeyDown, bool isKeyUp)
    {
        if (!_options.Enabled)
            return false;

        // Update modifier state
        if (_modifierKeys.ContainsKey(vkCode))
        {
            if (isKeyDown)
                _modifierState |= _modifierKeys[vkCode];
            else if (isKeyUp)
                _modifierState &= ~_modifierKeys[vkCode];
        }

        // Check if this is the activation key (default: Space)
        if (vkCode == _options.ActivationKey)
        {
            if (isKeyDown && !_activationKeyDown)
            {
                _activationKeyDown = true;
                return true; // Block the activation key press
            }
            else if (isKeyUp && _activationKeyDown)
            {
                _activationKeyDown = false;

                // Release all held mapped keys
                foreach (var heldKey in _mappedKeysHeld)
                {
                    var targetVk = heldKey & 0xFFFF;
                    var modifiers = (int)(heldKey & 0xFFFF0000);
                    SendKeyRequested?.Invoke(targetVk, false, modifiers);
                }
                _mappedKeysHeld.Clear();

                return true; // Block the activation key release
            }
        }

        // If activation key is down, check for mappings
        if (_activationKeyDown && _options.KeyMapping.TryGetValue(vkCode, out var mappedKey))
        {
            var targetVk = mappedKey & 0xFFFF;
            var modifiers = (int)(mappedKey & 0xFFFF0000);

            if (isKeyDown)
            {
                // Send the mapped key down
                SendKeyRequested?.Invoke(targetVk, true, modifiers);
                _mappedKeysHeld.Add(mappedKey);

                if (_options.TrainingMode && _options.BeepForMistakes)
                {
                    // Beep to indicate successful mapping in training mode
                    Console.Beep(1000, 50);
                }
            }
            else if (isKeyUp && _mappedKeysHeld.Contains(mappedKey))
            {
                // Send the mapped key up
                SendKeyRequested?.Invoke(targetVk, false, modifiers);
                _mappedKeysHeld.Remove(mappedKey);
            }

            return true; // Block the original key
        }

        // Training mode: beep for unmapped keys while activation key is down
        if (_activationKeyDown && _options.TrainingMode && _options.BeepForMistakes && isKeyDown)
        {
            Console.Beep(500, 100);
        }

        return false; // Let the key through
    }

    public void Reset()
    {
        _activationKeyDown = false;
        _mappedKeysHeld.Clear();
        _modifierState = 0;
    }
}
