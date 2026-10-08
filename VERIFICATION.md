# 検証記録

## 2026-10-08: 実利用先へのdeploy

- 配置後の実利用について、ユーザーが正常動作を確認したと報告（2026-10-08）。

- ユーザーの明示指示により、AppDockと全6Appletの`deploy.bat`を引数なしで実行し、7件すべて終了コード0。配置先は`A:\00.ESSENTIAL\00.MainTools\AppDock.at365`。5つの.NET Appletは現ソース/SDKで`publish.bat`を先に実行し、Gmailはdeploy内で再発行した。
- AppDock0.16.2、Gmail0.5.1、WallpaperSlideshow0.3.0、Watch0.1.1（native）、WebBrowserTools0.2.4、WindowMover0.2.1、WindowsTools0.1.1を配置。Watchの古いDLL版manifestを配置せず、現ソースのnative版へ更新。
- 配置対象21ファイルのSHA256はすべて発行元と一致。現ソースと配置manifestの版/runtime/entry、minimumHostVersionも照合。settings.json・avatar.png・Gmail accounts.jsonの3ファイルは配置前後のハッシュ不変。
- 配置前後とも関連プロセスなし。実利用アプリは起動していないため、次回起動で反映する。旧ファイル退避は行わず、設定・認証領域を配置スクリプトで変更していない。結果は`../AppDock.at365/artifacts/deploy-2026-10-08-result.json`（本体では`artifacts/deploy-2026-10-08-result.json`）。

## 2026-10-08: 依存パッケージ確認

- 外部NuGet PackageReferenceなし。slnxの`dotnet list package --outdated`も更新なし。依存定義や製品コード・版の変更は不要。参照するAppDockのnpm更新詳細は[本体検証記録](../AppDock.at365/VERIFICATION.md)を参照。
- 現AppDock SDK/RuntimeでRelease build警告0/エラー0、既存RegressionTests成功。実アプリ/ハードウェアに作用するnative検証、publish/deployは今回実施していない。

2026-10-06 / Windows / .NET SDK 10.0.401

## v0.2.0: ウィンドウ操作コマンド追加

- Release ビルド: 警告0件、エラー0件。`publish.bat` による v0.2.0 の発行成功。
- 回帰テスト: 23/23 成功。既存17件に加え、状態操作の対象固定／無効対象の除外／中央移動の両軸・枠補正・負座標・過大サイズ／最大化中の中央移動抑止を確認。
- 発行DLL + 既存AppDock実ランナー: 空設定でのコマンドID8件とパネル登録、トレイ0件を確認。
- テスト専用ウィンドウ: 従来の左右移動と高さ調整に加えて、中央移動、最大化、元のサイズ・位置への復元、最小化を確認。
- 通常の終了要求: 対象のFormClosingで1回目をキャンセルできること、2回目を許可すると閉じることを確認。プロセスの強制終了は行わない。
- deactivate、別プロセスでの再有効化、EOF終了も成功。実利用先への配置は未実施。

## v0.1.0: 初期実装

- Release ビルド: 警告0件、エラー0件。
- 回帰テスト: 17/17 成功。左右の中央→端→隣画面、負の座標、枠補正、全高、上下吸着と境界値、過大サイズ、片画面、段違い／幅の違う画面、無効対象、対象HWNDの固定、移動を拒否するアプリを検証。
- `publish.bat`: 成功。DLL、deps.json、extension.json を生成。
- 発行 DLL + 既存 AppDock.ExtensionHost.exe: 初回の空設定で有効化、コマンドID3件、パネル登録、トレイ0件を確認。
- Win32 実動作: テスト専用の枠なしウィンドウで左右の中央移動／端移動／全高を確認。カーソル位置・最前面ウィンドウの維持を確認。
- 終了: deactivate後の正常終了、別プロセスでの再有効化、stdinのEOFによる正常終了を確認。
- `deploy.bat`: 空白を含むテスト用ホストパスに配置し、DLL・deps.json・manifestのSHA-256一致を確認。SDKを同梱しない配置済みDLLも実ランナーで正常に読み込み、有効化・終了を確認。
- バッチ2本: CP932の往復変換とCRLFを確認。PowerShellスクリプト2本: 構文エラーなし。Windows PowerShellからの日本語メッセージのためUTF-8 BOM付きで保存。

最初のビルド／発行はサンドボックスの SDK・NuGet 設定読み取り制限で失敗し、許可された環境で再実行して成功。実動作テストでは Windows のフォーカス制限があったため、テスト側に限って入力スレッドを一時接続してテスト画面を最前面にした後に検証。製品コードにはフォーカスを奪う処理を追加していない。

## 検証の限界

- AppDock の Electron 画面でのコマンド選択、グローバルホットキーの登録・キー入力経由の実行、パッケージ済み AppDock EXE 全体での動作は未検証。既存の AppDock API と実ランナーを用いた検証まで。
- 物理的な複数モニター間の移動、異なるDPI、モニター抜き差し、RDP、最大化／全画面／管理者権限の各アプリ、長期常駐は未検証。複数モニターの配置計算は回帰テストで検証。
- 実利用の AppDock への配置や設定変更は実施していない。
