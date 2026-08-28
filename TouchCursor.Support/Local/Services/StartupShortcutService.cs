using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace TouchCursor.Support.Local.Services;

/// <summary>
/// 시작프로그램 폴더(shell:startup)에 실행 파일 바로가기(.lnk)를 만들어
/// 윈도우 시작 시 자동 실행되도록 등록/해제한다.
/// </summary>
public static class StartupShortcutService
{
    private const string ShortcutFileName = "TouchCursor.lnk";

    /// <summary>현재 사용자 시작프로그램 폴더의 바로가기 경로.</summary>
    public static string ShortcutPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), ShortcutFileName);

    /// <summary>실행 중인 애플리케이션의 exe 경로.</summary>
    public static string? ExecutablePath => Environment.ProcessPath;

    public static bool IsRegistered() => File.Exists(ShortcutPath);

    /// <summary>바로가기를 만들어 시작프로그램에 등록한다.</summary>
    public static void Register()
    {
        var exePath = ExecutablePath;
        if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            throw new InvalidOperationException("실행 파일 경로를 확인할 수 없습니다.");

        var startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        Directory.CreateDirectory(startupDir);

        var link = (IShellLinkW)new ShellLink();
        link.SetPath(exePath);
        link.SetWorkingDirectory(Path.GetDirectoryName(exePath) ?? string.Empty);
        link.SetIconLocation(exePath, 0);
        link.SetDescription("TouchCursor");

        ((IPersistFile)link).Save(ShortcutPath, true);
        Marshal.FinalReleaseComObject(link);
    }

    /// <summary>시작프로그램 등록을 해제한다.</summary>
    public static void Unregister()
    {
        if (File.Exists(ShortcutPath))
            File.Delete(ShortcutPath);
    }

    /// <summary>탐색기로 시작프로그램 폴더를 연다. 바로가기가 있으면 선택된 상태로 연다.</summary>
    public static void OpenFolder()
    {
        var startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        Directory.CreateDirectory(startupDir);

        var arguments = IsRegistered() ? $"/select,\"{ShortcutPath}\"" : $"\"{startupDir}\"";
        Process.Start(new ProcessStartInfo("explorer.exe", arguments) { UseShellExecute = true });
    }

    #region COM Interop (IShellLink)

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink
    {
    }

    [ComImport]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile,
                     int cch, IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath,
                             int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        [PreserveSig] int IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
                  [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    #endregion
}
