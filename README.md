# Applet.WindowMover.at365

[Watch.at365](../Watch.at365/README.md) のウィンドウ操作を、[AppDock.at365](../AppDock.at365/README.md) の拡張コマンドとして利用する Windows 用 Applet です。AppDock API v1 / .NET 10 の DLL Applet として動作し、ホストの変更は不要です。

## コマンド

| コマンドID | 表示名 | 動作 |
| --- | --- | --- |
| `at365.window-mover.left` | ウィンドウを左へ移動 | 中央より右なら中央へ、次に左端へ、さらに実行すると左のモニターの右端へ |
| `at365.window-mover.right` | ウィンドウを右へ移動 | 中央より左なら中央へ、次に右端へ、さらに実行すると右のモニターの左端へ |
| `at365.window-mover.full-height` | ウィンドウを上下いっぱいに広げる | 横位置と幅を基本的に保ち、作業領域の上端から下端まで高さを調整 |
| `at365.window-mover.center` | ウィンドウを画面中央へ移動 | サイズを保ち、見えているウィンドウ枠を現在の作業領域の縦横中央へ移動 |
| `at365.window-mover.minimize` | ウィンドウを最小化 | 対象ウィンドウへ通常の最小化要求を送信 |
| `at365.window-mover.maximize` | ウィンドウを最大化 | 対象ウィンドウへ通常の最大化要求を送信 |
| `at365.window-mover.restore` | ウィンドウを元のサイズに戻す | 最大化した最前面ウィンドウを通常のサイズへ復元 |
| `at365.window-mover.close` | ウィンドウを閉じる | 通常の終了要求を送信。保存確認や終了のキャンセルは対象アプリが処理 |

**実行時に最前面のウィンドウが対象です。** AppDock のパレット／パネル／ローカルショートカットから実行すると、AppDock 自身が対象になります。ほかのアプリを操作する場合は「設定 → ショートカット」でキーを割り当て、各コマンドの「グローバル」を有効にしてください。

設定例は左に `F23`、右に `F24`、高さ調整に `Ctrl+Alt+F10` です。AppDock v0.3.1 は Windows キーを含むショートカットに未対応なので、Watch の `Alt+Win+F10/F11/F12` をそのまま使うことはできません。既存 Watch が同じホットキーを登録していると競合するため、片方だけで登録するか別のキーを使ってください。既定のキー割り当ては追加しません。

Watch の作業領域基準、上下端付近の吸着（上60px／下70px）、DWM の不可視枠補正、作業領域を超えるサイズの縮小を引き継いでいます。隣のモニターは元の Watch と同様に現在の作業領域の幅だけ中心点をずらして選び、作業領域の上端または下端が一致する場合に移動します。端のモニターでは折り返しません。任意の上下配置に対するモニター巡回ではありません。

マウスカーソルを変更しません。移動・高さ調整ではフォーカス・Z順も変更しません。閉じる・最小化などの結果として、Windows が別のウィンドウを最前面にすることはあります。デスクトップ、タスクバー、タイトルなし、非表示、最小化中のウィンドウは除外します。最大化・全画面・固定サイズのアプリは、対象アプリ側の制約により移動やサイズ変更が適用されない場合があります。通常権限の AppDock から管理者権限のアプリを操作する場合も、Windows の制限を受けます。

中央移動は最大化中には何もせず、先に「元のサイズに戻す」を実行します。作業領域より大きいウィンドウも中央移動では縮小しません。「元のサイズに戻す」は Windows の通常の復元操作で、左右移動・高さ調整の取り消しや、直前に最小化した別ウィンドウの呼び戻しではありません。

閉じる・最小化・最大化・復元は `WM_SYSCOMMAND` を非同期で送信します。コマンドの完了は要求の送信完了を表し、対象アプリの操作完了や終了を強制しません。閉じる操作は Alt+F4 のキー送信やプロセス強制終了を行わず、対象ウィンドウへ直接通常の終了要求を送ります。

時計・マウスジェスチャー・独自ホットキー登録・Win+左右によるスナップ送信は含みません。ホットキーの登録と解除は AppDock が担当します。常駐タイマーやフック、独自の保存設定はありません。

## ビルドと配置

.NET 10 SDK と、隣接する `../AppDock.at365/dotnet/AppDock.SDK` が必要です。

```powershell
dotnet build .\Applet.WindowMover.at365.slnx -c Release
.\publish.bat
```

発行先は [publish/Applet.WindowMover.at365](publish/Applet.WindowMover.at365/) です。別の出力先は `publish.bat -OutputDirectory "A:\任意のフォルダー"` で指定できます。

配置先の AppDock を終了してから、AppDock の EXE があるフォルダーを指定します。

```powershell
.\deploy.bat "A:\Apps\AppDock.at365"
```

引数がない場合は、無視対象の `deploy.local.txt` の1行目を使います。[deploy.local.txt.example](deploy.local.txt.example) を参考に設定してください。両方未指定なら配置せず終了します。`deploy.bat` は既定の発行先を使います。

`extensions/Applet.WindowMover.at365` に DLL、deps.json、[extension.json](extension.json) を配置します。AppDock.SDK はホストが供給するため配布先へコピーしません。AppDock の設定や既存 Applet は変更しません。AppDock を起動し直し、Applet 一覧から有効にすると、8つのコマンドがパレットとショートカット設定に表示されます。

## 検証

```powershell
dotnet run --project .\Applet.WindowMover.RegressionTests -c Release
```

実際のランナーと発行済み DLL の接続を検証する場合:

```powershell
dotnet run --project .\Applet.WindowMover.RegressionTests -c Release -- --protocol-smoke "..\AppDock.at365\artifacts\dotnet-host\AppDock.ExtensionHost.exe" ".\publish\Applet.WindowMover.at365\Applet.WindowMover.at365.dll"
```

テスト専用ウィンドウの実移動も確認する場合は `--protocol-smoke` を `--native-smoke` に置き換えます。テスト画面に一時的にフォーカスを移すため、操作していない状態で実行してください。テストが作ったウィンドウだけを移動し、終了時に元のフォーカスへ戻します。

実測結果と未検証の範囲は [VERIFICATION.md](VERIFICATION.md) に記載しています。
