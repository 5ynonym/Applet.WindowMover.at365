using Applets.WindowMover;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args is ["--native-smoke", var runner, var assembly]) return NativeSmoke.Run(runner, assembly);
        if (args is ["--protocol-smoke", var protocolRunner, var protocolAssembly]) return NativeSmoke.Protocol(protocolRunner, protocolAssembly);
        var tests = new (string Name, Action Test)[] {
            ("system actions target one original HWND without moving", () => {
                foreach (var (command, action) in new[] {
                    (MoveCommand.Close, WindowAction.Close), (MoveCommand.Minimize, WindowAction.Minimize),
                    (MoveCommand.Maximize, WindowAction.Maximize), (MoveCommand.Restore, WindowAction.Restore) })
                {
                    var windows = new FakeWindows(new(100, 200, 700, 800));
                    new WindowMover(windows).Execute(command);
                    Check(windows.Actions.SequenceEqual(new[] { action }) && windows.ForegroundReads == 1 && windows.Moves == 0);
                }
            }),
            ("all commands ignore invalid targets", () => {
                foreach (var command in Enum.GetValues<MoveCommand>())
                {
                    var windows = new FakeWindows(new(100, 200, 700, 800)) { Valid = false };
                    new WindowMover(windows).Execute(command);
                    Check(windows.Actions.Count == 0 && windows.Moves == 0);
                }
            }),
            ("center both axes preserving size", () => {
                var windows = new FakeWindows(new(100, 200, 700, 800));
                new WindowMover(windows).Execute(MoveCommand.Center);
                Equal(new(660, 220, 1260, 820), windows.Rect);
            }),
            ("center visible frame on negative-coordinate monitor", () => Equal(new(-1268, 220, -652, 828),
                WindowLayout.CenterInWorkArea(new(-1808, 100, -1192, 708), new(-1800, 100, -1200, 700), new(-1920, 0, 0, 1040)))),
            ("center leaves maximized window unchanged", () => {
                var windows = new FakeWindows(new(0, 0, 1920, 1040)) { Maximized = true };
                new WindowMover(windows).Execute(MoveCommand.Center);
                Check(windows.Moves == 0 && windows.Actions.Count == 0);
            }),
            ("center oversized window preserves size", () => Equal(new(-140, -80, 2060, 1120),
                WindowLayout.CenterInWorkArea(new(0, 0, 2200, 1200), new(0, 0, 2200, 1200), new(0, 0, 1920, 1040)))),
            ("left: center, edge, adjacent monitor", () => {
                var windows = new FakeWindows(new(1200, 200, 1800, 800));
                var mover = new WindowMover(windows);
                mover.Execute(MoveCommand.Left); Equal(new(660, 200, 1260, 800), windows.Rect);
                mover.Execute(MoveCommand.Left); Equal(new(0, 200, 600, 800), windows.Rect);
                mover.Execute(MoveCommand.Left); Equal(new(-600, 200, 0, 800), windows.Rect);
            }),
            ("right: center, edge, adjacent monitor", () => {
                var windows = new FakeWindows(new(100, 200, 700, 800));
                var mover = new WindowMover(windows);
                mover.Execute(MoveCommand.Right); Equal(new(660, 200, 1260, 800), windows.Rect);
                mover.Execute(MoveCommand.Right); Equal(new(1320, 200, 1920, 800), windows.Rect);
                mover.Execute(MoveCommand.Right); Equal(new(1920, 200, 2520, 800), windows.Rect);
            }),
            ("negative coordinates center", () => Equal(new(-1260, 200, -660, 800),
                WindowLayout.Center(new(-700, 200, -100, 800), new(-700, 200, -100, 800), new(-1920, 0, 0, 1040), true))),
            ("visible frame compensation at left edge", () => Equal(new(-8, 200, 608, 816),
                WindowLayout.Edge(new(400, 200, 1016, 816), new(408, 200, 1008, 808), new(0, 0, 1920, 1040), true, false))),
            ("full height preserves horizontal position and width", () => {
                var windows = new FakeWindows(new(350, 200, 950, 800));
                new WindowMover(windows).Execute(MoveCommand.FullHeight);
                Equal(new(350, 0, 950, 1040), windows.Rect);
            }),
            ("full height includes invisible bottom frame", () => Equal(new(350, 0, 966, 1048),
                WindowLayout.Edge(new(350, 200, 966, 816), new(358, 200, 958, 808), new(0, 0, 1920, 1040), false, false, true, true))),
            ("top and bottom proximity expands height", () => Equal(new(660, 0, 1260, 1040),
                WindowLayout.Center(new(1100, 30, 1700, 1020), new(1100, 30, 1700, 1020), new(0, 0, 1920, 1040), true))),
            ("strict padding threshold", () => Equal(new(660, 60, 1260, 970),
                WindowLayout.Center(new(1100, 60, 1700, 970), new(1100, 60, 1700, 970), new(0, 0, 1920, 1040), true))),
            ("oversized window fits work area", () => {
                var windows = new FakeWindows(new(-100, -200, 2100, 1300));
                new WindowMover(windows).Execute(MoveCommand.Left);
                Equal(new(0, 0, 1920, 1040), windows.Rect);
            }),
            ("twice-monitor-wide window unchanged", () => {
                var windows = new FakeWindows(new(-1920, 100, 1920, 700));
                new WindowMover(windows).Execute(MoveCommand.Left); Check(windows.Moves == 0);
            }),
            ("no monitor at edge does not wrap", () => {
                var windows = new FakeWindows(new(0, 200, 600, 800)) { SingleMonitor = true };
                new WindowMover(windows).Execute(MoveCommand.Left); Equal(new(0, 200, 600, 800), windows.Rect);
            }),
            ("vertically staggered monitor rejected", () => Check(!WindowLayout.IsAdjacent(new(0, 0, 1920, 1040), new(1920, 200, 3840, 1240)))),
            ("bottom-aligned monitor accepted", () => Check(WindowLayout.IsAdjacent(new(0, 0, 1920, 1040), new(1920, 200, 3840, 1040)))),
            ("different monitor width", () => {
                var windows = new FakeWindows(new(1320, 200, 1920, 800)) { RightArea = new(1920, 0, 3200, 1040) };
                new WindowMover(windows).Execute(MoveCommand.Right); Equal(new(1920, 200, 2520, 800), windows.Rect);
            }),
            ("invalid target unchanged", () => {
                var windows = new FakeWindows(new(100, 200, 700, 800)) { Valid = false };
                new WindowMover(windows).Execute(MoveCommand.Left); Check(windows.Moves == 0);
            }),
            ("single target and two SetWindowPos calls", () => {
                var windows = new FakeWindows(new(1100, 200, 1700, 800));
                new WindowMover(windows).Execute(MoveCommand.Left); Check(windows.ForegroundReads == 1 && windows.Moves == 2);
            }),
            ("application rejecting moves terminates", () => {
                var windows = new FakeWindows(new(1100, 200, 1700, 800)) { RejectMoves = true };
                new WindowMover(windows).Execute(MoveCommand.Left); Check(windows.Moves <= 6);
                Equal(new(1100, 200, 1700, 800), windows.Rect);
            })
        };
        var failures = 0;
        foreach (var (name, test) in tests)
        {
            try { test(); Console.WriteLine($"PASS {name}"); }
            catch (Exception error) { failures++; Console.Error.WriteLine($"FAIL {name}: {error.Message}"); }
        }
        Console.WriteLine($"{tests.Length - failures}/{tests.Length} passed");
        return failures == 0 ? 0 : 1;
    }
    private static void Check(bool value) { if (!value) throw new InvalidOperationException("Assertion failed"); }
    private static void Equal(WindowRect expected, WindowRect? actual)
    {
        if (expected != actual) throw new InvalidOperationException($"Expected {expected}; actual {actual}");
    }
    private sealed class FakeWindows(WindowRect initial) : IWindowSystem
    {
        public WindowRect Rect = initial;
        public WindowRect RightArea = new(1920, 0, 3840, 1040);
        public bool Valid = true, SingleMonitor, RejectMoves;
        public bool Maximized;
        public List<WindowAction> Actions = [];
        public bool IsMaximized(nint window) => Maximized;
        public void RequestAction(nint window, WindowAction action) { Check(window == 42); Actions.Add(action); }
        public int Moves, ForegroundReads;
        public nint ForegroundWindow() { ForegroundReads++; return 42; }
        public bool IsTarget(nint window) => Valid;
        public WindowRect GetRect(nint window) => Rect;
        public WindowRect GetFrame(nint window, WindowRect fallback) => fallback;
        public WindowRect WorkingArea(WindowRect rect) => SingleMonitor ? new(0, 0, 1920, 1040)
            : rect.CenterX < 0 ? new(-1920, 0, 0, 1040) : rect.CenterX >= 1920 ? RightArea : new(0, 0, 1920, 1040);
        public void Move(nint window, WindowRect rect) { Check(window == 42); Moves++; if (!RejectMoves) Rect = rect; }
    }
}
