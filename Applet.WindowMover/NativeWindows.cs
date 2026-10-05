using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Applets.WindowMover;

internal sealed class NativeWindows : IWindowSystem
{
    public nint ForegroundWindow() => GetForegroundWindow();
    public bool IsMaximized(nint window) => IsZoomed(window);
    public void RequestAction(nint window, WindowAction action)
    {
        var command = action switch {
            WindowAction.Close => 0xF060,
            WindowAction.Minimize => 0xF020,
            WindowAction.Maximize => 0xF030,
            WindowAction.Restore => 0xF120,
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };
        // Normal system-menu request, never terminate a process or simulate keys.
        // Posting lets the target show a save prompt or refuse closure without blocking AppDock.
        if (!PostMessage(window, 0x0112, command, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    public bool IsTarget(nint window)
    {
        if (window == 0 || window == GetDesktopWindow() || window == GetShellWindow()
            || !IsWindow(window) || !IsWindowVisible(window) || IsIconic(window)) return false;
        var name = new StringBuilder(256);
        GetClassName(window, name, name.Capacity);
        if (name.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd") return false;
        return GetWindowText(window, new StringBuilder(2048), 2048) > 0;
    }

    public WindowRect GetRect(nint window)
    {
        if (!GetWindowRect(window, out var rect)) throw new Win32Exception(Marshal.GetLastWin32Error());
        return rect.Value;
    }
    public WindowRect GetFrame(nint window, WindowRect fallback) =>
        DwmGetWindowAttribute(window, 9, out var rect, Marshal.SizeOf<Rect>()) == 0 ? rect.Value : fallback;

    public WindowRect WorkingArea(WindowRect rect)
    {
        var monitor = MonitorFromPoint(new Point { X = rect.CenterX, Y = rect.CenterY }, 2);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info)) throw new Win32Exception(Marshal.GetLastWin32Error());
        return info.Work.Value;
    }
    public void Move(nint window, WindowRect rect)
    {
        // Do not change foreground/Z-order when invoked from a global shortcut.
        if (!SetWindowPos(window, 0, rect.Left, rect.Top, rect.Width, rect.Height, 0x0014))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    public static IDisposable PhysicalCoordinates()
    {
        var previous = SetThreadDpiAwarenessContext(new nint(-4)); // Per-monitor V2.
        if (previous == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        return new DpiScope(previous);
    }
    private sealed class DpiScope(nint previous) : IDisposable
    {
        public void Dispose() => SetThreadDpiAwarenessContext(previous);
    }
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct Rect
    {
        public int Left, Top, Right, Bottom;
        public readonly WindowRect Value => new(Left, Top, Right, Bottom);
    }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor, Work;
        public uint Flags;
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint GetDesktopWindow();
    [DllImport("user32.dll")] private static extern nint GetShellWindow();
    [DllImport("user32.dll")] private static extern bool IsWindow(nint window);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern bool IsIconic(nint window);
    [DllImport("user32.dll")] private static extern bool IsZoomed(nint window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(nint window, StringBuilder text, int count);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint window, StringBuilder text, int count);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool GetWindowRect(nint window, out Rect rect);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint window, int attribute, out Rect rect, int size);
    [DllImport("user32.dll")] private static extern nint MonitorFromPoint(Point point, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll", SetLastError = true)] private static extern nint SetThreadDpiAwarenessContext(nint context);
}
