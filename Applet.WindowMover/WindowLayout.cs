namespace Applets.WindowMover;

internal readonly record struct WindowRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;
    public int Height => Bottom - Top;
    public int CenterX => Left + Width / 2;
    public int CenterY => Top + Height / 2;
    public WindowRect Offset(int x) => new(Left + x, Top, Right + x, Bottom);
    public static WindowRect FromSize(int x, int y, int width, int height) => new(x, y, x + width, y + height);
}

// Port of Watch/Native/NativeHelper's center/edge calculations, in physical pixels.
internal static class WindowLayout
{
    public static WindowRect CenterInWorkArea(WindowRect rect, WindowRect frame, WindowRect area)
    {
        // Center the visible frame, preserving the full window size and asymmetric borders.
        var x = area.Left + (area.Width - frame.Width) / 2 - (frame.Left - rect.Left);
        var y = area.Top + (area.Height - frame.Height) / 2 - (frame.Top - rect.Top);
        return WindowRect.FromSize(x, y, rect.Width, rect.Height);
    }

    public static WindowRect? Center(WindowRect rect, WindowRect frame, WindowRect area, bool left)
    {
        if (rect.Width > area.Width) return null;
        var diff = rect.CenterX - area.CenterX;
        if (left ? diff <= 0 : diff >= 0) return null;
        var (top, bottom) = FitVertical(rect, frame, area);
        var extraHeight = rect.Height - frame.Height;
        var height = top && bottom ? area.Height + extraHeight : rect.Height;
        var y = top ? area.Top : bottom ? area.Bottom - height + extraHeight : rect.Top;
        return WindowRect.FromSize(rect.Left - diff, y, rect.Width, height);
    }

    public static WindowRect? Edge(WindowRect rect, WindowRect frame, WindowRect area,
        bool left, bool right, bool top = false, bool bottom = false)
    {
        left |= rect.Width > area.Width;
        right |= rect.Width > area.Width;
        top |= rect.Height > area.Height;
        bottom |= rect.Height > area.Height;
        var fit = FitVertical(rect, frame, area);
        top |= fit.Top;
        bottom |= fit.Bottom;
        var extraWidth = rect.Width - frame.Width;
        var extraHeight = rect.Height - frame.Height;
        var width = left && right ? area.Width + extraWidth : rect.Width;
        var height = top && bottom ? area.Height + extraHeight : rect.Height;
        var x = left && right ? area.Left + (area.Width - width) / 2
            : left ? area.Left - extraWidth / 2
            : right ? area.Right - width + extraWidth / 2 : rect.Left;
        var y = top ? area.Top : bottom ? area.Bottom - height + extraHeight : rect.Top;
        return WindowRect.FromSize(x, y, width, height);
    }

    public static bool IsAdjacent(WindowRect current, WindowRect candidate) =>
        (candidate.Left != current.Left || candidate.Top != current.Top)
        && (candidate.Top == current.Top || candidate.Bottom == current.Bottom);

    private static (bool Top, bool Bottom) FitVertical(WindowRect rect, WindowRect frame, WindowRect area) => (
        (frame.Height <= area.Height && rect.Top < area.Top)
            || (rect.Top > area.Top - 60 && rect.Top < area.Top + 60),
        (frame.Height <= area.Height && rect.Bottom > area.Bottom)
            || (rect.Bottom > area.Bottom - 70 && rect.Bottom < area.Bottom + 70));
}
