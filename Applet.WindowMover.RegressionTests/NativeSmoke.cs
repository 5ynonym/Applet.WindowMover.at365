using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Applets.WindowMover;

internal static class NativeSmoke
{
    public static int Protocol(string runner, string assembly)
    {
        try
        {
            using var peer = new Runner(runner, assembly);
            var contributions = peer.Request("activate", new { id = "at365.window-mover", settings = new { } });
            var ids = contributions.GetProperty("commands").EnumerateArray().Select(c => c.GetProperty("id").GetString()).Order().ToArray();
            Require(ids.SequenceEqual(new[] { "at365.window-mover.center", "at365.window-mover.close", "at365.window-mover.full-height", "at365.window-mover.left", "at365.window-mover.maximize", "at365.window-mover.minimize", "at365.window-mover.restore", "at365.window-mover.right" }), "command registration");
            Require(peer.PanelSeen && contributions.GetProperty("tray").GetArrayLength() == 0, "panel and tray registration");
            peer.Request("deactivate", new { });
            peer.CloseInput();
            using var eof = new Runner(runner, assembly);
            eof.Request("activate", new { id = "at365.window-mover", settings = new { } });
            eof.CloseInput();
            Console.WriteLine("PASS published DLL: activation, 8 command IDs, panel, no tray, deactivate, restart, EOF shutdown");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    // Uses only a disposable window owned by this test; never moves a user's window.
    public static int Run(string runner, string assembly)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        var previous = GetForegroundWindow();
        try
        {
            using var dpi = NativeWindows.PhysicalCoordinates();
            using var form = new Form {
                Text = "WindowMover isolated regression window", StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false,
                Bounds = new Rectangle(100, 200, 600, 400)
            };
            form.Show();
            var native = new NativeWindows();
            var area = native.WorkingArea(native.GetRect(form.Handle));
            var width = Math.Min(600, area.Width / 3);
            var height = Math.Min(400, area.Height / 2);
            form.SetBounds(area.Left + 100, area.Top + 150, width, height);
            form.Activate();
            SetForegroundWindow(form.Handle);
            if (GetForegroundWindow() != form.Handle)
            {
                var foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
                var testThread = GetCurrentThreadId();
                if (foregroundThread != 0 && AttachThreadInput(testThread, foregroundThread, true))
                {
                    try { SetForegroundWindow(form.Handle); }
                    finally { AttachThreadInput(testThread, foregroundThread, false); }
                }
            }
            Console.WriteLine($"Test HWND={form.Handle}; foreground={GetForegroundWindow()}; visible={form.Visible}");
            PumpUntil(() => GetForegroundWindow() == form.Handle);
            using var peer = new Runner(runner, assembly);
            var contributions = peer.Request("activate", new { id = "at365.window-mover", settings = new { } });
            var ids = contributions.GetProperty("commands").EnumerateArray().Select(c => c.GetProperty("id").GetString()).Order().ToArray();
            Require(ids.SequenceEqual(new[] { "at365.window-mover.center", "at365.window-mover.close", "at365.window-mover.full-height", "at365.window-mover.left", "at365.window-mover.maximize", "at365.window-mover.minimize", "at365.window-mover.restore", "at365.window-mover.right" }), "command registration");
            Require(contributions.GetProperty("tray").GetArrayLength() == 0, "no tray registrations");
            Require(peer.PanelSeen, "panel registration");
            Console.WriteLine("PASS published DLL activation, 8 commands, panel, no tray");
            var cursor = Cursor.Position;
            void Execute(string suffix, bool preservesForeground = true)
            {
                Require(GetForegroundWindow() == form.Handle, "test window must own foreground before moving");
                peer.Request("command.execute", new { id = "at365.window-mover." + suffix });
                Application.DoEvents();
                if (preservesForeground) Require(GetForegroundWindow() == form.Handle, "foreground preserved");
                Require(Cursor.Position == cursor, "cursor preserved");
            }
            Execute("right");
            var rect = native.GetRect(form.Handle);
            Require(Math.Abs(rect.CenterX - area.CenterX) <= 1 && rect.Width == width, "right centers window");
            Execute("right");
            Require(native.GetRect(form.Handle).Right == area.Right, "right edge");
            Execute("left");
            Require(Math.Abs(native.GetRect(form.Handle).CenterX - area.CenterX) <= 1, "left centers window");
            Execute("left");
            Require(native.GetRect(form.Handle).Left == area.Left, "left edge");
            Execute("full-height");
            rect = native.GetRect(form.Handle);
            Require(rect.Top == area.Top && rect.Bottom == area.Bottom && rect.Width == width, "full height");
            Console.WriteLine("PASS native left/right/full-height; foreground and cursor preserved");
            form.SetBounds(area.Left + 100, area.Top + 150, width, height);
            Execute("center");
            rect = native.GetRect(form.Handle);
            Require(Math.Abs(rect.CenterX - area.CenterX) <= 1 && Math.Abs(rect.CenterY - area.CenterY) <= 1,
                "center on both axes");
            Require(rect.Width == width && rect.Height == height, "center preserves size");
            var normalBounds = rect;
            Execute("maximize");
            PumpUntil(() => form.WindowState == FormWindowState.Maximized);
            Execute("restore");
            PumpUntil(() => form.WindowState == FormWindowState.Normal);
            Require(native.GetRect(form.Handle) == normalBounds, "restore normal bounds");
            Execute("minimize", false);
            PumpUntil(() => form.WindowState == FormWindowState.Minimized);
            // A minimized window is no longer foreground; restore the test fixture ourselves.
            form.WindowState = FormWindowState.Normal;
            form.Activate();
            SetForegroundWindow(form.Handle);
            PumpUntil(() => GetForegroundWindow() == form.Handle);
            var closeRequests = 0;
            var allowClose = false;
            form.FormClosing += (_, e) => { closeRequests++; e.Cancel = !allowClose; };
            Execute("close", false);
            PumpUntil(() => closeRequests == 1);
            Require(!form.IsDisposed && form.Visible, "target can cancel normal close request");
            allowClose = true;
            Execute("close", false);
            PumpUntil(() => form.IsDisposed);
            Require(closeRequests == 2, "target accepted second close request");
            Console.WriteLine("PASS center, maximize/restore, minimize, cancelable close, accepted close");
            peer.Request("deactivate", new { });
            peer.CloseInput();
            Console.WriteLine("PASS deactivate and process exit");
            using var eof = new Runner(runner, assembly);
            eof.Request("activate", new { id = "at365.window-mover", settings = new { } });
            eof.CloseInput();
            Console.WriteLine("PASS reactivation in a new process and EOF shutdown");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally { if (previous != 0) SetForegroundWindow(previous); }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void PumpUntil(Func<bool> condition)
    {
        var timer = Stopwatch.StartNew();
        while (!condition())
        {
            if (timer.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("Native smoke timed out");
            Application.DoEvents();
            Thread.Sleep(10);
        }
    }
    private sealed class Runner : IDisposable
    {
        private readonly Process process;
        private readonly Task<string> errors;
        private int sequence;
        public bool PanelSeen { get; private set; }
        public Runner(string executable, string assembly)
        {
            var start = new ProcessStartInfo(executable) {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                StandardInputEncoding = new UTF8Encoding(false), StandardOutputEncoding = Encoding.UTF8
            };
            start.ArgumentList.Add(Path.GetFullPath(assembly));
            start.ArgumentList.Add("Applets.WindowMover.WindowMoverApplet");
            process = Process.Start(start) ?? throw new InvalidOperationException("Runner did not start");
            errors = process.StandardError.ReadToEndAsync();
        }
        public JsonElement Request(string method, object parameters)
        {
            var id = "test-" + ++sequence;
            Send(new { jsonrpc = "2.0", id, method, @params = parameters });
            while (true)
            {
                var read = process.StandardOutput.ReadLineAsync();
                PumpUntil(() => read.IsCompleted);
                var line = read.GetAwaiter().GetResult() ?? throw new InvalidOperationException("Runner closed: " + errors.GetAwaiter().GetResult());
                using var document = JsonDocument.Parse(line);
                var message = document.RootElement;
                if (message.TryGetProperty("method", out var request))
                {
                    Require(request.GetString() == "host.ui.panel", "unexpected host API call");
                    PanelSeen = true;
                    Send(new { jsonrpc = "2.0", id = message.GetProperty("id").GetString(), result = (object?)null });
                    continue;
                }
                Require(message.GetProperty("id").GetString() == id, "RPC response ID");
                if (message.TryGetProperty("error", out var error)) throw new InvalidOperationException(error.ToString());
                return message.GetProperty("result").Clone();
            }
        }
        private void Send(object message) { process.StandardInput.WriteLine(JsonSerializer.Serialize(message)); process.StandardInput.Flush(); }
        public void CloseInput()
        {
            process.StandardInput.Close();
            PumpUntil(() => process.HasExited);
            Require(process.ExitCode == 0, "runner exit: " + errors.GetAwaiter().GetResult());
        }
        public void Dispose() { if (!process.HasExited) process.Kill(entireProcessTree: true); process.Dispose(); }
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint from, uint to, bool attach);
}
