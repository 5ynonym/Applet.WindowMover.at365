using AppDock.SDK;

namespace Applets.WindowMover;

public sealed class WindowMoverApplet : IAppDockExtension
{
    private bool active;
    public async Task ActivateAsync(IExtensionContext context, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("WindowMover requires Windows.");
        active = true;
        var commands = new (string Suffix, string Title, MoveCommand Command)[] {
            ("left", "ウィンドウを左へ移動", MoveCommand.Left),
            ("right", "ウィンドウを右へ移動", MoveCommand.Right),
            ("full-height", "ウィンドウを上下いっぱいに広げる", MoveCommand.FullHeight),
            ("center", "ウィンドウを画面中央へ移動", MoveCommand.Center),
            ("minimize", "ウィンドウを最小化", MoveCommand.Minimize),
            ("maximize", "ウィンドウを最大化", MoveCommand.Maximize),
            ("restore", "ウィンドウを元のサイズに戻す", MoveCommand.Restore),
            ("close", "ウィンドウを閉じる", MoveCommand.Close)
        };
        foreach (var command in commands)
            context.Commands.Register(context.ExtensionId + "." + command.Suffix, command.Title, token => {
                token.ThrowIfCancellationRequested();
                if (!active) return Task.CompletedTask;
                // Synchronous scope: DPI context must be restored on the same thread.
                using var dpi = NativeWindows.PhysicalCoordinates();
                new WindowMover(new NativeWindows()).Execute(command.Command);
                return Task.CompletedTask;
            });
        await context.Ui.ShowPanelAsync(new Panel("WindowMover",
            "実行時に最前面のウィンドウが対象です。ほかのアプリを動かすには、設定 → ショートカットで各コマンドにキーを割り当て、「グローバル」を有効にしてください。",
            [new PanelFact("左右移動", "中央 → 端 → 隣のモニターの順に移動します。"),
             new PanelFact("高さ調整", "タスクバーを除いた作業領域に合わせます。"),
             new PanelFact("ウィンドウ操作", "最小化・最大化・元のサイズに戻す・閉じるに対応。閉じる際の保存確認は対象アプリに任せます。"),
             new PanelFact("操作対象", "AppDockの画面から実行した場合はAppDock自身が対象です。")],
            commands.Select(command => new PanelAction(command.Title, context.ExtensionId + "." + command.Suffix)).ToArray()), cancellationToken);
    }

    public Task DeactivateAsync(CancellationToken cancellationToken)
    {
        active = false;
        return Task.CompletedTask;
    }
}
