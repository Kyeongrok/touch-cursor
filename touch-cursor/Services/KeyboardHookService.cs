// Copyright © 2025. Ported to C# from original C++ TouchCursor by Martin Stone.
// Original project licensed under GNU GPL v3.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

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

    public KeyboardHookService(KeyMappingService mappingService)
    {
        _mappingService = mappingService;
        _proc = HookCallback;
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

            // Ignore our own injected events
            if (hookStruct.dwExtraInfo == INJECTED_FLAG)
            {
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }

            var vkCode = (int)hookStruct.vkCode;
            var isKeyDown = wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN;
            var isKeyUp = wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP;

            Debug.WriteLine($"[HookCallback] vkCode={vkCode}, wParam={wParam}, isKeyDown={isKeyDown}, isKeyUp={isKeyUp}, sendingModifiers={_sendingModifiers}");

            // Block modifier key events while we're sending them via SendInput
            if (_sendingModifiers && IsModifierKey(vkCode))
            {
                Debug.WriteLine($"[HookCallback] BLOCKING modifier key {vkCode} during SendInput");
                return (IntPtr)1;
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

    public void SendKey(int vkCode, bool isDown, int modifierFlags = 0)
    {
        Debug.WriteLine($"[SendKey] vkCode={vkCode}, isDown={isDown}, modifierFlags={modifierFlags:X}");
        var inputs = new List<INPUT>();

        // Only press/release modifiers when pressing the main key
        if (isDown && modifierFlags != 0)
        {
            Debug.WriteLine("[SendKey] Pressing modifiers");
            _sendingModifiers = true;
            // Press modifiers first (use specific left modifier keys)
            if ((modifierFlags & 0x00010000) != 0) // Shift
                inputs.Add(CreateKeyInput(0xA0, true)); // VK_LSHIFT
            if ((modifierFlags & 0x00020000) != 0) // Ctrl
                inputs.Add(CreateKeyInput(0xA2, true)); // VK_LCONTROL
            if ((modifierFlags & 0x00040000) != 0) // Alt
                inputs.Add(CreateKeyInput(0xA4, true)); // VK_LMENU
            if ((modifierFlags & 0x00080000) != 0) // Win
                inputs.Add(CreateKeyInput(0x5B, true)); // VK_LWIN
        }

        // Press/release the main key
        inputs.Add(CreateKeyInput(vkCode, isDown));

        // Release modifiers when releasing the main key
        if (!isDown && modifierFlags != 0)
        {
            Debug.WriteLine("[SendKey] Releasing modifiers");
            _sendingModifiers = true;
            // Release modifiers (reverse order, use specific left modifier keys)
            if ((modifierFlags & 0x00080000) != 0) // Win
                inputs.Add(CreateKeyInput(0x5B, false)); // VK_LWIN
            if ((modifierFlags & 0x00040000) != 0) // Alt
                inputs.Add(CreateKeyInput(0xA4, false)); // VK_LMENU
            if ((modifierFlags & 0x00020000) != 0) // Ctrl
                inputs.Add(CreateKeyInput(0xA2, false)); // VK_LCONTROL
            if ((modifierFlags & 0x00010000) != 0) // Shift
                inputs.Add(CreateKeyInput(0xA0, false)); // VK_LSHIFT
        }

        if (inputs.Count > 0)
        {
            Debug.WriteLine($"[SendKey] Sending {inputs.Count} input(s) via SendInput");
            var inputArray = inputs.ToArray();
            var result = SendInput((uint)inputs.Count, inputArray, Marshal.SizeOf(typeof(INPUT)));
            var lastError = Marshal.GetLastWin32Error();
            Debug.WriteLine($"[SendKey] SendInput result: {result}, LastError: {lastError}, StructSize: {Marshal.SizeOf(typeof(INPUT))}");
            _sendingModifiers = false;
        }
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
