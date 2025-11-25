// Copyright © 2025. Ported to C# from original C++ TouchCursor by Martin Stone.
// Original project licensed under GNU GPL v3.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using touch_cursor.Models;

namespace touch_cursor.Services;

public class KeyboardHookService : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("kernel32.dll")]
    private static extern uint GetLastError();

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint flags);

    [DllImport("user32.dll")]
    private static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
        [Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pwszBuff,
        int cchBuff, uint wFlags, IntPtr dwhkl);

    [DllImport("user32.dll")]
    private static extern bool GetKeyboardState(byte[] lpKeyState);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct INPUT
    {
        [FieldOffset(0)]
        public uint type;
        [FieldOffset(8)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const IntPtr INJECTED_FLAG = (IntPtr)0x54435552; // 'TCUR'

    private IntPtr _hookID = IntPtr.Zero;
    private readonly LowLevelKeyboardProc _proc;
    private readonly KeyMappingService _mappingService;
    private bool _sendingModifiers = false;
    private readonly TouchCursorOptions _options;
    private IntPtr _englishLayout = IntPtr.Zero;
    private IntPtr _koreanLayout = IntPtr.Zero;
    private bool _lastWasInitialConsonant = false; // 마지막 입력이 초성 자음이었는지

    // 세벌식 최종 모음 키 (VK 코드)
    private static readonly HashSet<int> SebeolFinalVowelKeys = new()
    {
        0x44, // D = ㅣ
        0x46, // F = ㅏ
        0x54, // T = ㅓ
        0x47, // G = ㅡ (Shift+G = ㅒ)
        0x56, // V = ㅗ
        0x42, // B = ㅜ
        0x39, // 9 = ㅜ
        0x35, // 5 = ㅠ
        0x45, // E = ㅕ
        0x43, // C = ㅔ
        0x34, // 4 = ㅛ
        0x36, // 6 = ㅑ, ㅐ
        0x37, // 7 = ㅖ
        0x38, // 8 = ㅢ
        0xBF, // / = ㅗ (VK_OEM_2)
    };

    // 세벌식 최종 초성 자음 키 (VK 코드)
    private static readonly HashSet<int> SebeolFinalInitialConsonantKeys = new()
    {
        0x4B, // K = ㄱ
        0x48, // H
        0x55, // U
        0x59, // Y
        0x49, // I
        0xBA, // ; (VK_OEM_1)
        0x4E, // N = ㅅ
        0x4A, // J = ㅇ
        0x4D, // M = ㅎ
        0x4C, // L = ㅈ
        0x50, // P = ㅍ
        0xDE, // ' = ㅌ (VK_OEM_7)
        0x4F, // O = ㅊ
    };

    public KeyboardHookService(KeyMappingService mappingService, TouchCursorOptions options)
    {
        _mappingService = mappingService;
        _options = options;
        _proc = HookCallback;
        CacheKeyboardLayouts();
    }

    private void CacheKeyboardLayouts()
    {
        // 영문(0x0409) 및 한글(0x0412) 레이아웃 찾기
        var layouts = InputLanguage.InstalledInputLanguages;
        foreach (InputLanguage layout in layouts)
        {
            var cultureLCID = layout.Culture.LCID & 0xFFFF;
            if (cultureLCID == 0x0409) // 영문
            {
                _englishLayout = layout.Handle;
            }
            else if (cultureLCID == 0x0412) // 한글
            {
                _koreanLayout = layout.Handle;
            }
        }
    }

    public void StartHook()
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        if (curModule != null)
        {
            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    public void StopHook()
    {
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var vkCode = (int)hookStruct.vkCode;
            var isKeyDown = wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN;
            var isKeyUp = wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP;

            // Update modifier state BEFORE checking if injected (like original C++ code)
            _mappingService.UpdateModifierState(vkCode, isKeyDown, isKeyUp);

            Debug.WriteLine($"[HookCallback] vkCode={vkCode}, wParam={wParam}, isKeyDown={isKeyDown}, isKeyUp={isKeyUp}, sendingModifiers={_sendingModifiers}");

            // Ignore our own injected events
            if (hookStruct.dwExtraInfo == INJECTED_FLAG)
            {
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }

            // Block modifier key events while we're sending them via SendInput
            if (_sendingModifiers && IsModifierKey(vkCode))
            {
                Debug.WriteLine($"[HookCallback] BLOCKING modifier key {vkCode} during SendInput");
                return (IntPtr)1;
            }

            // Like original C++ code: don't process modifier keys in state machine
            if (IsModifierKey(vkCode))
            {
                Debug.WriteLine($"[HookCallback] Modifier key {vkCode} - letting it through");
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }

            // 세벌식 모음 자동 영문 전환: 초성 없이 모음 키를 누르면 영문 전환
            if (_options.AutoSwitchToEnglishOnNonConsonant && isKeyDown)
            {
                if (IsKoreanLayout())
                {
                    bool isVowelKey = SebeolFinalVowelKeys.Contains(vkCode);
                    bool isInitialConsonantKey = SebeolFinalInitialConsonantKeys.Contains(vkCode);

                    Debug.WriteLine($"[AutoSwitch] vk={vkCode:X2} isVowel={isVowelKey} isInitial={isInitialConsonantKey} lastWasInitial={_lastWasInitialConsonant}");

                    if (isVowelKey && !_lastWasInitialConsonant)
                    {
                        // 초성 없이 모음 입력 → 영문 전환 후 키 재전송
                        Debug.WriteLine($"[AutoSwitch] 초성 없이 모음 입력 - 영문 전환");

                        SwitchToEnglishLayout();

                        // 영문 전환 완료 대기
                        for (int i = 0; i < 10; i++)
                        {
                            System.Threading.Thread.Sleep(10);
                            if (!IsKoreanLayout()) break;
                        }

                        Debug.WriteLine($"[AutoSwitch] 영문 전환 완료, 키 재전송");
                        _lastWasInitialConsonant = false;

                        // 원래 키를 차단하고 영문 키로 재전송
                        SendSingleKey(vkCode, true);
                        SendSingleKey(vkCode, false);
                        return (IntPtr)1; // 원래 키 차단
                    }
                    else if (isInitialConsonantKey)
                    {
                        // 초성 자음 입력
                        _lastWasInitialConsonant = true;
                        Debug.WriteLine($"[AutoSwitch] 초성 자음 입력");
                    }
                    else if (isVowelKey)
                    {
                        // 초성 다음 모음 → 통과, 상태 유지 (키 반복 대응)
                        // _lastWasInitialConsonant는 리셋하지 않음
                        Debug.WriteLine($"[AutoSwitch] 초성 다음 모음 - 통과");
                    }
                    else
                    {
                        // 종성, 숫자, 기호 등 → 상태 리셋
                        _lastWasInitialConsonant = false;
                        Debug.WriteLine($"[AutoSwitch] 기타 입력 - 상태 리셋");
                    }
                }
                else
                {
                    // 영문 모드에서는 상태 초기화
                    _lastWasInitialConsonant = false;
                }
            }

            if (_mappingService.ProcessKey(vkCode, isKeyDown, isKeyUp))
            {
                Debug.WriteLine($"[HookCallback] BLOCKING key event for vkCode={vkCode}");
                // Block this key event
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private bool IsModifierKey(int vkCode)
    {
        return vkCode == 0x10 || vkCode == 0xA0 || vkCode == 0xA1 || // Shift
               vkCode == 0x11 || vkCode == 0xA2 || vkCode == 0xA3 || // Ctrl
               vkCode == 0x12 || vkCode == 0xA4 || vkCode == 0xA5 || // Alt
               vkCode == 0x5B || vkCode == 0x5C; // Win
    }

    private bool IsKoreanLayout()
    {
        // 포커스된 윈도우의 스레드 ID를 가져와서 해당 스레드의 키보드 레이아웃 확인
        var hwnd = GetForegroundWindow();
        var threadId = GetWindowThreadProcessId(hwnd, out _);
        var currentLayout = GetKeyboardLayout(threadId);
        var layoutId = (uint)currentLayout.ToInt64() & 0xFFFF;
        Debug.WriteLine($"[IsKoreanLayout] threadId={threadId}, layoutId={layoutId:X4}");
        return layoutId == 0x0412;
    }

    private bool IsKoreanInitialConsonant(int vkCode, uint scanCode)
    {
        // 초성 자음인지 확인 (종성 제외)
        var keyState = new byte[256];
        GetKeyboardState(keyState);

        var sb = new System.Text.StringBuilder(10);
        var currentLayout = GetKeyboardLayout(0);
        var result = ToUnicodeEx((uint)vkCode, scanCode, keyState, sb, sb.Capacity, 0, currentLayout);

        Debug.WriteLine($"[IsKoreanInitialConsonant] vkCode={vkCode}, result={result}, sbLength={sb.Length}");

        if (result > 0 && sb.Length > 0)
        {
            var ch = sb[0];
            Debug.WriteLine($"[IsKoreanInitialConsonant] char='{ch}' (U+{(int)ch:X4})");

            // 초성 자모: U+1100 ~ U+1112
            // 호환 자모 자음: U+3131 ~ U+314E (두벌식에서 사용)
            bool isInitial = (ch >= 0x1100 && ch <= 0x1112) || (ch >= 0x3131 && ch <= 0x314E);

            // 종성은 제외: U+11A8 ~ U+11C2
            bool isFinal = ch >= 0x11A8 && ch <= 0x11C2;

            Debug.WriteLine($"[IsKoreanInitialConsonant] isInitial={isInitial}, isFinal={isFinal}");

            return isInitial && !isFinal;
        }

        return false;
    }

    private bool IsKoreanVowel(int vkCode, uint scanCode)
    {
        var keyState = new byte[256];
        GetKeyboardState(keyState);

        var sb = new System.Text.StringBuilder(10);
        var currentLayout = GetKeyboardLayout(0);
        var result = ToUnicodeEx((uint)vkCode, scanCode, keyState, sb, sb.Capacity, 0, currentLayout);

        if (result > 0 && sb.Length > 0)
        {
            var ch = sb[0];
            // 중성 자모: U+1161 ~ U+1175
            // 호환 자모 모음: U+314F ~ U+3163 (두벌식에서 사용)
            bool isVowel = (ch >= 0x1161 && ch <= 0x1175) || (ch >= 0x314F && ch <= 0x3163);
            Debug.WriteLine($"[IsKoreanVowel] char='{ch}' (U+{(int)ch:X4}), isVowel={isVowel}");
            return isVowel;
        }

        return false;
    }

    private void SwitchToEnglishLayout()
    {
        // 한/영 키(VK_HANGUL = 0x15)를 시뮬레이션하여 영문으로 전환
        Debug.WriteLine("[AutoSwitch] 한/영 키를 눌러 영문으로 전환");

        const int VK_HANGUL = 0x15;
        SendSingleKey(VK_HANGUL, true);
        SendSingleKey(VK_HANGUL, false);
    }

    public void SendKey(int vkCode, bool isDown, int modifierFlags = 0)
    {
        Debug.WriteLine($"[SendKey] vkCode={vkCode}, isDown={isDown}, modifierFlags={modifierFlags:X}");

        _sendingModifiers = true;

        // Original C++ behavior: send each key event separately with Sleep(1) between them
        // "separate events are not sent in one SendInput() call, because that doesn't work with Remote Desktop Connection"

        // Original C++ behavior: modifiers are only injected on key DOWN, and immediately released
        if (isDown && modifierFlags != 0)
        {
            Debug.WriteLine("[SendKey] Pressing modifiers (will release immediately after key press)");
            // Press modifiers first (use specific left modifier keys)
            if ((modifierFlags & 0x00010000) != 0) // Shift
                SendSingleKey(0xA0, true); // VK_LSHIFT
            if ((modifierFlags & 0x00020000) != 0) // Ctrl
                SendSingleKey(0xA2, true); // VK_LCONTROL
            if ((modifierFlags & 0x00040000) != 0) // Alt
                SendSingleKey(0xA4, true); // VK_LMENU
            if ((modifierFlags & 0x00080000) != 0) // Win
                SendSingleKey(0x5B, true); // VK_LWIN
        }

        // Press/release the main key
        SendSingleKey(vkCode, isDown);

        // Original C++ behavior: immediately release modifiers after key press
        if (isDown && modifierFlags != 0)
        {
            Debug.WriteLine("[SendKey] Releasing modifiers immediately");
            // Release modifiers (reverse order, use specific left modifier keys)
            if ((modifierFlags & 0x00080000) != 0) // Win
                SendSingleKey(0x5B, false); // VK_LWIN
            if ((modifierFlags & 0x00040000) != 0) // Alt
                SendSingleKey(0xA4, false); // VK_LMENU
            if ((modifierFlags & 0x00020000) != 0) // Ctrl
                SendSingleKey(0xA2, false); // VK_LCONTROL
            if ((modifierFlags & 0x00010000) != 0) // Shift
                SendSingleKey(0xA0, false); // VK_LSHIFT
        }

        _sendingModifiers = false;
    }

    private void SendSingleKey(int vkCode, bool isDown)
    {
        var input = CreateKeyInput(vkCode, isDown);
        var result = SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));

        // Sleep(1) seems to be necessary for Remote Desktop Connection
        System.Threading.Thread.Sleep(1);
    }

    private INPUT CreateKeyInput(int vkCode, bool isDown)
    {
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            ki = new KEYBDINPUT
            {
                wVk = (ushort)vkCode,
                wScan = 0,
                dwFlags = isDown ? 0 : KEYEVENTF_KEYUP,
                time = 0,
                dwExtraInfo = INJECTED_FLAG
            }
        };
    }

    public void Dispose()
    {
        StopHook();
    }
}
