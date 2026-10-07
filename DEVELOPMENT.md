# Applet.WindowMover.at365 開発ガイド

利用方法は[README.md](README.md)、実測結果と未確認事項は[VERIFICATION.md](VERIFICATION.md)を参照してください。

## ビルドと配置

.NET 10 SDK と、隣接する `../AppDock.at365/dotnet/AppDock.SDK` が必要です。

```powershell
dotnet build .\Applet.WindowMover.at365.slnx -c Release
.\publish.bat
```

発行先は `publish/Applet.WindowMover.at365/` です。別の出力先は `publish.bat -OutputDirectory "A:\任意のフォルダー"` で指定できます。

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

## ウィンドウ操作の実装

閉じる・最小化・最大化・復元は `WM_SYSCOMMAND` を非同期で送信します。コマンドの完了は要求の送信完了を表し、対象アプリの操作完了や終了を強制しません。閉じる操作は Alt+F4 のキー送信やプロセス強制終了を行わず、対象ウィンドウへ直接通常の終了要求を送ります。

DWMの不可視枠補正を含むWatchの移動計算を引き継いでいます。常駐タイマーやフック、独自の保存設定はありません。

## 文書の更新

READMEには動作環境・導入・操作・設定・利用上の制約を記載します。開発環境・ビルド・テスト・発行・開発者用配置・実装の説明はこのファイル、実測結果と未検証事項はVERIFICATION.mdへ記載します。共通方針は[AppDockのドキュメント方針](../AppDock.at365/docs/documentation.md)を参照してください。
