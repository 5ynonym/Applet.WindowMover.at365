namespace Applets.WindowMover;

internal enum MoveCommand { Left, Right, FullHeight, Center, Close, Minimize, Maximize, Restore }
internal enum WindowAction { Close, Minimize, Maximize, Restore }
internal interface IWindowSystem
{
    nint ForegroundWindow();
    bool IsTarget(nint window);
    WindowRect GetRect(nint window);
    WindowRect GetFrame(nint window, WindowRect fallback);
    WindowRect WorkingArea(WindowRect rect);
    void Move(nint window, WindowRect rect);
    void RequestAction(nint window, WindowAction action);
    bool IsMaximized(nint window);
}

internal sealed class WindowMover(IWindowSystem windows)
{
    public void Execute(MoveCommand command)
    {
        // Keep the original HWND for the entire command, even if foreground changes.
        var window = windows.ForegroundWindow();
        if (!windows.IsTarget(window)) return;
        switch (command)
        {
            case MoveCommand.Close: windows.RequestAction(window, WindowAction.Close); return;
            case MoveCommand.Minimize: windows.RequestAction(window, WindowAction.Minimize); return;
            case MoveCommand.Maximize: windows.RequestAction(window, WindowAction.Maximize); return;
            case MoveCommand.Restore: windows.RequestAction(window, WindowAction.Restore); return;
            case MoveCommand.Center:
                // Keep maximized state intact; Restore is a separate explicit command.
                if (windows.IsMaximized(window)) return;
                var bounds = windows.GetRect(window);
                Apply(window, WindowLayout.CenterInWorkArea(bounds, windows.GetFrame(window, bounds), windows.WorkingArea(bounds)));
                return;
        }
        if (command == MoveCommand.FullHeight) { Edge(window, false, false, true); return; }
        var left = command == MoveCommand.Left;
        var rect = windows.GetRect(window);
        var centered = WindowLayout.Center(rect, windows.GetFrame(window, rect), windows.WorkingArea(rect), left);
        if (centered is { } target)
        {
            Apply(window, target);
            var after = windows.GetRect(window);
            if (after.CenterX != rect.CenterX || after.CenterY != rect.CenterY) return;
        }
        if (!Edge(window, left, !left, false)) Edge(window, !left, left, false, left ? -1 : 1);
    }

    private bool Edge(nint window, bool left, bool right, bool fullHeight, int monitor = 0)
    {
        var rect = windows.GetRect(window);
        var area = windows.WorkingArea(rect);
        if (rect.Width >= area.Width * 2) return false;
        var frame = windows.GetFrame(window, rect);
        if (monitor != 0)
        {
            var shifted = rect.Offset(monitor * area.Width);
            var next = windows.WorkingArea(shifted);
            if (!WindowLayout.IsAdjacent(area, next)) return false;
            rect = shifted;
            area = next;
        }
        var target = WindowLayout.Edge(rect, frame, area, left, right, fullHeight, fullHeight);
        if (target is not { } bounds) return false;
        Apply(window, bounds);
        var after = windows.GetRect(window);
        // Compare actual result: applications can constrain or reject repositioning.
        return after != rect;
    }

    private void Apply(nint window, WindowRect target)
    {
        windows.Move(window, target);
        // Watch applies twice to avoid exposing the taskbar for fullscreen windows.
        windows.Move(window, target);
    }
}
