---
name: wpf-native-ui-e2e-readiness
description: WPFネイティブUI向けE2E自動化とリリース判定の実践スキル。WinAppCliを第一優先として、堅牢なシナリオ設計・CI/CD統合・失敗時の切り分けまでを標準化する。Windowsクライアントの出荷準備確認時に使用する。
license: MIT
---

# WPF Native UI E2E Automation & Delivery Readiness Skill

WPFネイティブUIアプリのE2Eテストを、**WinAppCli優先**で実装・安定化し、CI/CDのリリースゲートに組み込むためのスキル。

## スコープ境界

- 対象: WPFネイティブUI（デスクトップウィンドウ、ダイアログ、入力、保存、通知）
- 非対象: ブラウザUI E2E（Web系は別スキルで扱う）
- 補足: Playwrightは本スキルの主対象外（Web UI向け）

---

## トリガー（このスキルを使う場面）

- WPFアプリでE2E自動テストを新規導入したい
- リリース前に「主要ユーザーフローが壊れていない」ことを機械的に保証したい
- 手動回帰が重く、CIで自動判定したい
- flaky（不安定）テストが多く、再現性を上げたい
- UI変更（画面改修、ダイアログ追加、入力制御変更）の影響を安全に検証したい

---

## 必須入力

- テスト対象アプリの起動方法（exeパス、引数、設定ファイル）
- クリティカルユーザーフロー一覧（3〜10本）
- 画面要素識別方針（AutomationId付与ルール）
- テストデータ初期化手順（DB/ファイル/設定）
- CI実行条件（Windows runner、実行時間上限、アーティファクト保存先）

---

## 期待出力

1. 自動化対象シナリオ一覧（Smoke/Regression）
2. WinAppCliベースのE2Eテスト実装
3. 失敗時アーティファクト（スクリーンショット、ログ、動画/操作ログ）
4. CIジョブ定義（PR用Smoke、main用Regression）
5. リリース可否レポート（Pass率、失敗内訳、ブロッカー有無）

---

## Do / Don't

### Do

- **AutomationIdを最優先**で要素特定する
- クリティカルフローから先に自動化（ログイン、作成、編集、保存、終了）
- 待機は固定sleepではなく、**状態待ち（表示/活性/非表示）**を使う
- 失敗時に証跡を必ず採取（画面・ログ・例外）
- テストデータを毎回初期化して再現性を確保する

### Don't

- 見た目文字列や座標クリックに依存しすぎない
- 1本のテストに複数責務を詰め込まない
- ローカルだけ通る前提（CI差分未考慮）のままマージしない
- flakyテストを放置したままリリースゲートに入れない
- UAC/権限差分を無視しない

---

## 推奨ツール優先順位（WPFネイティブUI）

1. **WinAppCli（第一優先）**
   - 理由: ネイティブUI自動化を主目的に据えた運用がしやすく、E2Eパイプラインに組み込みやすい
2. **FlaUI (UIA3)**
   - 理由: C#実装との親和性が高く、詳細制御・拡張性に優れる
3. **WinAppDriver + Appium (Windows Driver)**
   - 理由: 既存Appium資産がある場合に有効
4. **補助ツール（Inspect.exe / Accessibility Insights）**
   - 理由: 要素特定・アクセシビリティ属性確認に必須

---

## WPF Native UI E2E 標準ワークフロー

### Step 1. クリティカルフロー定義
- 業務影響の高い順にシナリオを選定
- 例: 起動 → ログイン → データ作成 → 保存 → 再読込 → 終了

### Step 2. テスタビリティ契約
- 各画面の主要要素にAutomationIdを付与
- 命名規約を統一（`Screen_Control_Purpose`）

### Step 3. 実行環境固定化
- 画面解像度、表示倍率、OS言語、フォントを固定
- 初期データ投入スクリプトを準備
- テスト専用設定（外部連携OFF/スタブ）を用意

### Step 4. WinAppCliでSmoke作成
- まず1本、E2Eの最小成功経路を通す
- 失敗時にスクリーンショット・操作ログを取得する設定を有効化

### Step 5. 回帰シナリオ拡張
- 正常系→異常系→境界値の順で追加
- テストケースをタグ分割（`smoke`, `regression`, `slow`）

### Step 6. 安定化（flaky削減）
- 固定waitを削除し、要素状態待ちに置換
- モーダル・トースト・非同期読込の待機点を明示
- 失敗再実行ポリシー（例: 1回だけ再試行）を定義

### Step 7. ローカル品質ゲート
- PR前に smoke を必須実行
- 失敗時はログ・証跡付きで原因分類（テスト不備/製品不具合/環境差分）

### Step 8. CI/CD統合
- PR: smokeのみ（短時間）
- main/nightly: regression（フル）
- 失敗時アーティファクトを必ず保存

### Step 9. リリース判定
- 必須シナリオ100%成功
- Critical不具合0件
- flaky率が閾値内（例: 2%未満）

---

## CI/CD 組み込みヒント（GitHub Actions例）

```yaml
name: wpf-e2e
on:
  pull_request:
  push:
    branches: [ main ]

jobs:
  e2e-smoke:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - name: Build WPF App
        run: dotnet build .\YourApp.sln -c Release
      - name: Run WinAppCli Smoke
        run: winappcli run --suite smoke --app ".\src\YourApp\bin\Release\net8.0-windows\YourApp.exe"
      - name: Upload artifacts
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: wpf-e2e-artifacts
          path: |
            .\artifacts\e2e\screenshots\
            .\artifacts\e2e\logs\
```

> 実際の `winappcli` コマンド引数はプロジェクト標準に合わせて調整すること。

---

## 失敗対応・トラブルシューティングプレイブック

| 症状 | よくある原因 | 対応 |
|---|---|---|
| 要素が見つからない | AutomationId未設定/変更 | AutomationIdを契約化し、文字列セレクタ依存を削減 |
| CIだけ失敗する | 解像度・言語・権限差分 | runner環境固定、起動権限統一、環境情報をログ出力 |
| たまに失敗する | 非同期待機不足 | 固定sleepを状態待ちへ置換、待機タイムアウト見直し |
| クリック不発 | モーダル/ローディング被り | 前提状態を明示し、ブロッキング要素の消失待ちを追加 |
| 入力値が化ける | IME/キーボードレイアウト差異 | テスト時IME固定、直接値設定APIを優先 |
| 起動失敗 | パス不整合/依存DLL不足 | 実行前に存在チェック、依存関係検証ステップを追加 |
| 権限エラー | 管理者権限/UAC干渉 | 実行ユーザー権限を統一、UAC前提を明文化 |

---

## 完了判定（Definition of Done）

- [ ] クリティカルフローのSmokeがCIで常時成功
- [ ] Regressionの失敗が再現可能で原因分類済み
- [ ] 失敗時アーティファクトが自動保存される
- [ ] AutomationId命名規約がドキュメント化されている
- [ ] リリースゲート基準（成功率/ブロッカー）がチーム合意済み

---

## Keywords

WPF, Windows Client, Native UI, E2E, WinAppCli, UI Automation, Delivery Readiness, CI/CD, Flaky Test, Release Gate
