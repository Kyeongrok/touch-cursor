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

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)]
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

            if (_mappingService.ProcessKey(vkCode, isKeyDown, isKeyUp))
            {
                // Block this key event
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    public void SendKey(int vkCode, bool isDown, int modifierFlags = 0)
    {
        var inputs = new List<INPUT>();

        // Press modifiers first
        if ((modifierFlags & 0x00010000) != 0) // Shift
            inputs.Add(CreateKeyInput(0x10, true));
        if ((modifierFlags & 0x00020000) != 0) // Ctrl
            inputs.Add(CreateKeyInput(0x11, true));
        if ((modifierFlags & 0x00040000) != 0) // Alt
            inputs.Add(CreateKeyInput(0x12, true));
        if ((modifierFlags & 0x00080000) != 0) // Win
            inputs.Add(CreateKeyInput(0x5B, true));

        // Press/release the main key
        inputs.Add(CreateKeyInput(vkCode, isDown));

        // Release modifiers
        if ((modifierFlags & 0x00080000) != 0) // Win (reverse order)
            inputs.Add(CreateKeyInput(0x5B, false));
        if ((modifierFlags & 0x00040000) != 0) // Alt
            inputs.Add(CreateKeyInput(0x12, false));
        if ((modifierFlags & 0x00020000) != 0) // Ctrl
            inputs.Add(CreateKeyInput(0x11, false));
        if ((modifierFlags & 0x00010000) != 0) // Shift
            inputs.Add(CreateKeyInput(0x10, false));

        if (inputs.Count > 0)
        {
            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
        }
    }

    private INPUT CreateKeyInput(int vkCode, bool isDown)
    {
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            union = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)vkCode,
                    wScan = 0,
                    dwFlags = isDown ? 0 : KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = INJECTED_FLAG
                }
            }
        };
    }

    public void Dispose()
    {
        StopHook();
    }
}
