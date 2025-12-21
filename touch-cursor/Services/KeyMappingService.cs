// Copyright © 2025. Ported to C# from original C++ TouchCursor by Martin Stone.
// Original project licensed under GNU GPL v3.

using System.Diagnostics;
using touch_cursor.Models;
using TouchCursor.Support.Local.Helpers;

namespace touch_cursor.Services;

public class KeyMappingService : IKeyMappingService
{
    private readonly TouchCursorOptions _options;
    private readonly TypingLogger? _typingLogger;

    private int _currentActivationKey = 0; // Which activation key is currently pressed
    private readonly HashSet<int> _mappedKeysHeld = new();
    private int _modifierState = 0;
    private bool _activationKeyUsedForMapping = false;
    private long _activationKeyPressTime = 0; // Timestamp when activation key was pressed (ticks)

    // Mod Switch 상태
    private bool _modSwitchToggled = false; // 토글 모드가 활성화되었는지
    private int _toggledActivationKey = 0; // 어떤 활성화 키가 토글되었는지

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

    public KeyMappingService(TouchCursorOptions options, TypingLogger? typingLogger = null)
    {
        _options = options;
        _typingLogger = typingLogger;
    }

    /// <summary>
    /// 수정자 상태 업데이트 - 주입된 키를 포함한 모든 키에 대해 호출되어야 함 (원본 C++ 코드처럼)
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
    /// 키 이벤트를 처리하고 차단 또는 재매핑 여부 결정
    /// 참고: 수정자 키는 이 메서드로 전달하면 안 됨 (별도로 처리됨)
    /// </summary>
    /// <returns>키 이벤트를 차단해야 하면 True, 통과시키려면 false</returns>
    public bool ProcessKey(int vkCode, bool isKeyDown, bool isKeyUp)
    {
        Debug.WriteLine($"[ProcessKey] vkCode={vkCode}, isKeyDown={isKeyDown}, isKeyUp={isKeyUp}, Enabled={_options.Enabled}, modifierState={_modifierState:X}, modSwitchToggled={_modSwitchToggled}");

        // Mod Switch 토글 단축키 감지 (예: Alt + Space)
        if (_options.ModSwitchEnabled && isKeyDown &&
            vkCode == _options.ModSwitchToggleKey &&
            (_modifierState & _options.ModSwitchToggleModifiers) == _options.ModSwitchToggleModifiers)
        {
            // Mod Switch 토글
            _modSwitchToggled = !_modSwitchToggled;

            if (_modSwitchToggled)
            {
                // 토글 활성화: 첫 번째 활성화 키 프로파일 사용
                _toggledActivationKey = _options.ActivationKeyProfiles.Keys.FirstOrDefault(0x20);
                _currentActivationKey = _toggledActivationKey;
                _activationKeyUsedForMapping = false;
                _activationKeyPressTime = DateTime.Now.Ticks;
                Debug.WriteLine($"[ProcessKey] Mod Switch ON - 활성화 키 {_toggledActivationKey} 토글됨");
                Console.Beep(1200, 100); // 높은 톤 비프음
            }
            else
            {
                // 토글 비활성화: 모든 유지된 키 해제
                Debug.WriteLine($"[ProcessKey] Mod Switch OFF - {_mappedKeysHeld.Count}개 키 해제");
                foreach (var heldKey in _mappedKeysHeld)
                {
                    var targetVk = heldKey & 0xFFFF;
                    SendKeyRequested?.Invoke(targetVk, false, 0);
                }
                _mappedKeysHeld.Clear();
                _currentActivationKey = 0;
                _toggledActivationKey = 0;
                Console.Beep(800, 100); // 낮은 톤 비프음
            }

            return true; // 토글 단축키 차단
        }

        // Ctrl+Shift+Z 단축키로 마지막 항목을 실수로 표시
        if (isKeyDown && vkCode == 0x5A && // Z 키
            (_modifierState & (int)ModifierFlags.Ctrl) != 0 &&
            (_modifierState & (int)ModifierFlags.Shift) != 0)
        {
            Debug.WriteLine("[ProcessKey] Ctrl+Shift+Z 감지 - 마지막 항목을 실수로 표시");
            _typingLogger?.MarkLastAsMistake();
            return false; // 키를 통과시킴 (차단하지 않음)
        }

        if (!_options.Enabled)
            return false;

        // 설정된 활성화 키인지 확인 (토글 모드가 아닐 때만)
        if (!_modSwitchToggled && _options.ActivationKeyProfiles.ContainsKey(vkCode))
        {
            Debug.WriteLine($"[ProcessKey] 활성화 키 감지! vkCode={vkCode}, _currentActivationKey={_currentActivationKey}");
            if (isKeyDown && _currentActivationKey == 0)
            {
                _currentActivationKey = vkCode;
                _activationKeyUsedForMapping = false;
                _activationKeyPressTime = DateTime.Now.Ticks; // 타임스탬프 기록
                Debug.WriteLine($"[ProcessKey] 활성화 키 눌림 - 차단 및 _currentActivationKey={vkCode} 설정, timestamp={_activationKeyPressTime}");
                return true; // 활성화 키 누름 차단
            }
            else if (isKeyDown && _currentActivationKey != 0)
            {
                // 활성화 키 자동 반복 - 차단 (원본 C++ 상태 머신처럼)
                Debug.WriteLine($"[ProcessKey] 활성화 키 자동 반복 - 차단");
                return true;
            }
            else if (isKeyUp && _currentActivationKey == vkCode)
            {
                Debug.WriteLine($"[ProcessKey] 활성화 키 해제 - {_mappedKeysHeld.Count}개 유지 키 해제, wasUsedForMapping={_activationKeyUsedForMapping}");

                // 모든 유지된 매핑 키를 수정자 없이 해제 (원본 C++ 동작)
                foreach (var heldKey in _mappedKeysHeld)
                {
                    var targetVk = heldKey & 0xFFFF;
                    Debug.WriteLine($"[ProcessKey] 유지 키 해제: targetVk={targetVk}");
                    SendKeyRequested?.Invoke(targetVk, false, 0);
                }
                _mappedKeysHeld.Clear();

                // 활성화 키가 매핑에 사용되지 않았으면 키를 통과시킴
                if (!_activationKeyUsedForMapping)
                {
                    Debug.WriteLine($"[ProcessKey] 활성화 키가 매핑에 사용되지 않음 - 키 {vkCode} 전송");
                    SendKeyRequested?.Invoke(vkCode, true, 0);
                    SendKeyRequested?.Invoke(vkCode, false, 0);
                }

                _currentActivationKey = 0;
                return true; // 활성화 키 해제 차단
            }
        }

        // 활성화 키가 눌려있으면 현재 프로파일에서 매핑 확인
        if (_currentActivationKey != 0 &&
            _options.ActivationKeyProfiles.TryGetValue(_currentActivationKey, out var keyMappings) &&
            keyMappings.TryGetValue(vkCode, out var mappedKey))
        {
            var targetVk = mappedKey & 0xFFFF;
            var modifiers = (int)(mappedKey & 0xFFFF0000);
            Debug.WriteLine($"[ProcessKey] 매핑 발견! vkCode={vkCode} -> targetVk={targetVk}, modifiers={modifiers:X}");

            if (isKeyDown)
            {
                // 활성화 키 누른 이후 경과 시간 계산
                var elapsedMs = (DateTime.Now.Ticks - _activationKeyPressTime) / TimeSpan.TicksPerMillisecond;

                // 이 키가 롤오버 감지 예외인지 확인
                var isRolloverException = _options.RolloverExceptionKeys.TryGetValue(_currentActivationKey, out var exceptionKeys)
                                          && exceptionKeys.Contains(vkCode);

                // 홀드 딜레이 확인: 활성화 키를 최소 지속시간만큼 눌러야 함
                // 이 확인은 롤오버 감지 이전에 수행됨
                if (!isRolloverException && _options.ActivationKeyHoldDelayMs > 0)
                {
                    Debug.WriteLine($"[ProcessKey] 홀드 딜레이 확인: elapsed={elapsedMs}ms, required={_options.ActivationKeyHoldDelayMs}ms");

                    if (elapsedMs < _options.ActivationKeyHoldDelayMs)
                    {
                        // 홀드 딜레이 미달: 활성화 키를 충분히 오래 누르지 않음
                        Debug.WriteLine($"[ProcessKey] 홀드 딜레이 미달! 두 키 모두 일반 입력으로 처리");

                        // 홀드 딜레이 이벤트 기록
                        _typingLogger?.LogKeyEvent(new TypingLogEntry
                        {
                            Timestamp = DateTime.Now,
                            ActivationKey = _currentActivationKey,
                            ActivationKeyName = GetKeyName(_currentActivationKey),
                            SourceKey = vkCode,
                            SourceKeyName = GetKeyName(vkCode),
                            TargetKey = targetVk,
                            TargetKeyName = GetKeyName(targetVk),
                            Modifiers = _modifierState,
                            ElapsedMs = elapsedMs,
                            RolloverThreshold = _options.RolloverThresholdMs,
                            RolloverDetected = false,
                            IsRolloverException = false,
                            EventType = "hold_delay_not_met",
                            TrainingMode = _options.TrainingMode,
                            MarkedAsMistake = false
                        });

                        // 이전에 차단된 활성화 키 전송
                        SendKeyRequested?.Invoke(_currentActivationKey, true, 0);
                        SendKeyRequested?.Invoke(_currentActivationKey, false, 0);

                        // 활성화 상태 초기화
                        _currentActivationKey = 0;
                        _activationKeyPressTime = 0;
                        _activationKeyUsedForMapping = false;

                        // 현재 키를 통과시킴 (차단하지 않음)
                        return false;
                    }
                }

                // 롤오버 감지: 활성화 키 누른 후 너무 빨리 키를 눌렀는지 확인
                // 이 키가 예외 목록에 있으면 롤오버 확인 건너뜀
                if (!isRolloverException && _options.RolloverThresholdMs > 0)
                {
                    Debug.WriteLine($"[ProcessKey] 활성화 이후 경과 시간: {elapsedMs}ms, 임계값: {_options.RolloverThresholdMs}ms");

                    if (elapsedMs <= _options.RolloverThresholdMs)
                    {
                        // 롤오버 감지: 일반 타이핑으로 처리
                        Debug.WriteLine($"[ProcessKey] 롤오버 감지! 두 키 모두 일반 입력으로 처리");

                        // 롤오버 이벤트 기록
                        _typingLogger?.LogKeyEvent(new TypingLogEntry
                        {
                            Timestamp = DateTime.Now,
                            ActivationKey = _currentActivationKey,
                            ActivationKeyName = GetKeyName(_currentActivationKey),
                            SourceKey = vkCode,
                            SourceKeyName = GetKeyName(vkCode),
                            TargetKey = targetVk,
                            TargetKeyName = GetKeyName(targetVk),
                            Modifiers = _modifierState,
                            ElapsedMs = elapsedMs,
                            RolloverThreshold = _options.RolloverThresholdMs,
                            RolloverDetected = true,
                            IsRolloverException = false,
                            EventType = "rollover",
                            TrainingMode = _options.TrainingMode,
                            MarkedAsMistake = false
                        });

                        // 이전에 차단된 활성화 키 전송
                        SendKeyRequested?.Invoke(_currentActivationKey, true, 0);
                        SendKeyRequested?.Invoke(_currentActivationKey, false, 0);

                        // 활성화 상태 초기화
                        _currentActivationKey = 0;
                        _activationKeyPressTime = 0;
                        _activationKeyUsedForMapping = false;

                        // 현재 키를 통과시킴 (차단하지 않음)
                        return false;
                    }
                }
                else if (isRolloverException)
                {
                    Debug.WriteLine($"[ProcessKey] 키 {vkCode}는 롤오버 예외 목록에 있음 - 롤오버 감지 건너뜀");
                }

                Debug.WriteLine($"[ProcessKey] 매핑된 키 DOWN 전송: targetVk={targetVk}, mappingModifiers={modifiers:X}, currentModifiers={_modifierState:X}");
                _activationKeyUsedForMapping = true; // 활성화 키를 매핑에 사용했다고 표시
                // 이미 눌려있지 않은 수정자만 주입 (원본 C++ 코드처럼)
                var effectiveModifiers = modifiers & ~_modifierState;
                Debug.WriteLine($"[ProcessKey] 주입할 실효 수정자: {effectiveModifiers:X}");
                SendKeyRequested?.Invoke(targetVk, true, effectiveModifiers);
                _mappedKeysHeld.Add(mappedKey);

                // 매핑 이벤트 기록
                _typingLogger?.LogKeyEvent(new TypingLogEntry
                {
                    Timestamp = DateTime.Now,
                    ActivationKey = _currentActivationKey,
                    ActivationKeyName = GetKeyName(_currentActivationKey),
                    SourceKey = vkCode,
                    SourceKeyName = GetKeyName(vkCode),
                    TargetKey = targetVk,
                    TargetKeyName = GetKeyName(targetVk),
                    Modifiers = _modifierState,
                    ElapsedMs = elapsedMs,
                    RolloverThreshold = _options.RolloverThresholdMs,
                    RolloverDetected = false,
                    IsRolloverException = isRolloverException,
                    EventType = "mapped",
                    TrainingMode = _options.TrainingMode,
                    MarkedAsMistake = false
                });

                if (_options.TrainingMode && _options.BeepForMistakes)
                {
                    // 훈련 모드에서 성공적인 매핑을 나타내기 위해 비프음 출력
                    Console.Beep(1000, 50);
                }
            }
            else if (isKeyUp && _mappedKeysHeld.Contains(mappedKey))
            {
                Debug.WriteLine($"[ProcessKey] 매핑된 키 UP 전송: targetVk={targetVk}");
                // 수정자 없이 매핑된 키 UP 전송 (원본 C++ 동작)
                SendKeyRequested?.Invoke(targetVk, false, 0);
                _mappedKeysHeld.Remove(mappedKey);
            }

            return true; // 원본 키 차단
        }

        // 훈련 모드: 활성화 키가 눌려있을 때 매핑되지 않은 키에 대해 비프음
        if (_currentActivationKey != 0 && _options.TrainingMode && _options.BeepForMistakes && isKeyDown)
        {
            Debug.WriteLine($"[ProcessKey] 훈련 모드: 활성화 키가 눌려있는 동안 매핑되지 않은 키 {vkCode} 눌림");
            Console.Beep(500, 100);
        }

        Debug.WriteLine($"[ProcessKey] 키 통과시킴: vkCode={vkCode}");
        return false; // 키를 통과시킴
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

    /// <summary>
    /// Mod Switch 토글 상태를 반환합니다.
    /// </summary>
    public bool IsModSwitchToggled => _modSwitchToggled;

    private string GetKeyName(int vkCode)
    {
        return vkCode switch
        {
            0x08 => "Backspace",
            0x09 => "Tab",
            0x0D => "Enter",
            0x1B => "Escape",
            0x14 => "CapsLock",
            0x20 => "Space",
            0x21 => "Page Up",
            0x22 => "Page Down",
            0x23 => "End",
            0x24 => "Home",
            0x25 => "Left",
            0x26 => "Up",
            0x27 => "Right",
            0x28 => "Down",
            0x2D => "Insert",
            0x2E => "Delete",
            0x30 => "0", 0x31 => "1", 0x32 => "2", 0x33 => "3", 0x34 => "4",
            0x35 => "5", 0x36 => "6", 0x37 => "7", 0x38 => "8", 0x39 => "9",
            0x41 => "A", 0x42 => "B", 0x43 => "C", 0x44 => "D", 0x45 => "E",
            0x46 => "F", 0x47 => "G", 0x48 => "H", 0x49 => "I", 0x4A => "J",
            0x4B => "K", 0x4C => "L", 0x4D => "M", 0x4E => "N", 0x4F => "O",
            0x50 => "P", 0x51 => "Q", 0x52 => "R", 0x53 => "S", 0x54 => "T",
            0x55 => "U", 0x56 => "V", 0x57 => "W", 0x58 => "X", 0x59 => "Y",
            0x5A => "Z",
            0x70 => "F1", 0x71 => "F2", 0x72 => "F3", 0x73 => "F4",
            0x74 => "F5", 0x75 => "F6", 0x76 => "F7", 0x77 => "F8",
            0x78 => "F9", 0x79 => "F10", 0x7A => "F11", 0x7B => "F12",
            0xA2 => "Left Ctrl",
            0xA3 => "Right Ctrl",
            0xBA => ";", 0xBB => "=", 0xBC => ",", 0xBD => "-",
            0xBE => ".", 0xBF => "/", 0xC0 => "`", 0xDB => "[",
            0xDC => "\\", 0xDD => "]", 0xDE => "'",
            0xFF => "Fn",
            _ => $"VK_{vkCode:X2}"
        };
    }
}
