using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

/// <summary>
/// Embed Unity HWND under Explorer WorkerW (same approach as AIdeaDesktop desktop_win.py).
/// Only works in standalone Windows player — Editor will no-op.
/// </summary>
public static class DesktopWinHost
{
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    const int GWL_STYLE = -16;
    const int GWL_EXSTYLE = -20;
    const uint WS_CHILD = 0x40000000;
    const uint WS_VISIBLE = 0x10000000;
    const uint WS_CLIPSIBLINGS = 0x04000000;
    const uint WS_POPUP = 0x80000000;
    const uint WS_EX_TOOLWINDOW = 0x00000080;
    const uint WS_EX_NOACTIVATE = 0x08000000;
    const int HWND_BOTTOM = 1;
    const uint SWP_NOACTIVATE = 0x0010;
    const uint SWP_SHOWWINDOW = 0x0040;
    const uint SWP_FRAMECHANGED = 0x0020;
    const int SW_SHOWNOACTIVATE = 4;
    const uint SMTO_NORMAL = 0;

    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindowW(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindowExW(IntPtr parent, IntPtr childAfter, string cls, string window);

    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetClassNameW(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    static extern IntPtr SendMessageTimeoutW(
        IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam,
        uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    static string ClassName(IntPtr hwnd)
    {
        var sb = new StringBuilder(256);
        GetClassNameW(hwnd, sb, 256);
        return sb.ToString();
    }

    static void SpawnWorkerLayer()
    {
        var progman = FindWindowW("Progman", null);
        if (progman == IntPtr.Zero)
            throw new InvalidOperationException("找不到 Progman 窗口");

        foreach (var pair in new[] { (0, 0), (0xD, 0), (0xD, 1) })
        {
            SendMessageTimeoutW(
                progman, 0x052C,
                (IntPtr)pair.Item1, (IntPtr)pair.Item2,
                SMTO_NORMAL, 1000, out _);
        }
    }

    static List<(IntPtr hwnd, bool hasShell)> ListWorkerWindows()
    {
        var found = new List<(IntPtr, bool)>();
        EnumWindows((hwnd, _) =>
        {
            if (ClassName(hwnd) == "WorkerW")
            {
                var shell = FindWindowExW(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                found.Add((hwnd, shell != IntPtr.Zero));
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    public static IntPtr FindWorkerW()
    {
        SpawnWorkerLayer();
        var workers = ListWorkerWindows();
        Debug.Log($"[DesktopWinHost] WorkerW candidates: {workers.Count}");

        IntPtr shellHost = IntPtr.Zero;
        foreach (var (h, shell) in workers)
        {
            if (shell) { shellHost = h; break; }
        }

        if (shellHost != IntPtr.Zero)
        {
            var behind = FindWindowExW(IntPtr.Zero, shellHost, "WorkerW", null);
            if (behind != IntPtr.Zero)
            {
                Debug.Log($"[DesktopWinHost] WorkerW behind icons: 0x{behind.ToInt64():X}");
                return behind;
            }
        }

        for (int i = workers.Count - 1; i >= 0; i--)
        {
            if (!workers[i].hasShell)
            {
                Debug.Log($"[DesktopWinHost] Fallback empty WorkerW: 0x{workers[i].hwnd.ToInt64():X}");
                return workers[i].hwnd;
            }
        }

        var progman = FindWindowW("Progman", null);
        Debug.LogWarning($"[DesktopWinHost] No WorkerW; using Progman 0x{progman.ToInt64():X}");
        return progman;
    }

    public static Vector2Int PrimaryScreenSize()
    {
        return new Vector2Int(GetSystemMetrics(0), GetSystemMetrics(1));
    }

    public static IntPtr FindUnityHwnd()
    {
        uint pid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        IntPtr found = IntPtr.Zero;
        EnumWindows((hwnd, _) =>
        {
            GetWindowThreadProcessId(hwnd, out uint windowPid);
            if (windowPid != pid) return true;
            var cls = ClassName(hwnd);
            // Unity player main window class is typically "UnityWndClass"
            if (cls.Contains("Unity") || cls == "UnityWndClass")
            {
                found = hwnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);

        if (found == IntPtr.Zero)
            found = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
        return found;
    }

    public static IntPtr EmbedAsWallpaper(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            throw new InvalidOperationException("Unity HWND 无效");

        var worker = FindWorkerW();
        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, ex | unchecked((int)(WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE)));

        int style = GetWindowLong(hwnd, GWL_STYLE);
        style = (style | unchecked((int)(WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS))) & ~unchecked((int)WS_POPUP);
        SetWindowLong(hwnd, GWL_STYLE, style);

        SetParent(hwnd, worker);
        var size = PrimaryScreenSize();
        SetWindowPos(
            hwnd, (IntPtr)HWND_BOTTOM,
            0, 0, size.x, size.y,
            SWP_SHOWWINDOW | SWP_FRAMECHANGED | SWP_NOACTIVATE);
        ShowWindow(hwnd, SW_SHOWNOACTIVATE);

        Debug.Log($"[DesktopWinHost] Embedded hwnd=0x{hwnd.ToInt64():X} worker=0x{worker.ToInt64():X} {size.x}x{size.y}");
        return worker;
    }

    public static void KeepBehindIcons(IntPtr hwnd, IntPtr worker)
    {
        if (hwnd == IntPtr.Zero || worker == IntPtr.Zero) return;
        if (!IsWindow(hwnd) || !IsWindow(worker)) return;

        if (GetParent(hwnd) != worker)
        {
            Debug.Log($"[DesktopWinHost] Reparent 0x{hwnd.ToInt64():X} -> 0x{worker.ToInt64():X}");
            SetParent(hwnd, worker);
            var size = PrimaryScreenSize();
            SetWindowPos(
                hwnd, (IntPtr)HWND_BOTTOM,
                0, 0, size.x, size.y,
                SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }
        else if (!IsWindowVisible(hwnd))
        {
            ShowWindow(hwnd, SW_SHOWNOACTIVATE);
        }
    }

    public static bool HotkeyQuitPressed()
    {
        // Ctrl + Shift + Q
        return (GetAsyncKeyState(0x11) & 0x8000) != 0
            && (GetAsyncKeyState(0x10) & 0x8000) != 0
            && (GetAsyncKeyState(0x51) & 0x8000) != 0;
    }
#else
    public static IntPtr FindWorkerW() => IntPtr.Zero;
    public static Vector2Int PrimaryScreenSize() => new Vector2Int(Screen.width, Screen.height);
    public static IntPtr FindUnityHwnd() => IntPtr.Zero;
    public static IntPtr EmbedAsWallpaper(IntPtr hwnd) => IntPtr.Zero;
    public static void KeepBehindIcons(IntPtr hwnd, IntPtr worker) { }
    public static bool HotkeyQuitPressed() => false;
#endif
}
