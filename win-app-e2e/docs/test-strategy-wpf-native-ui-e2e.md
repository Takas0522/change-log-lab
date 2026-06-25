# WPF Native UI テスト戦略（WinAppCli-first）

## 概要
- 対象: `OrderClientApp`（WPF / SQLite）
- 目的: 主要業務フローを単体・結合・UI・E2Eで段階的に保証し、`wpf-native-ui-e2e-readiness` に沿った自動化基盤を整備する。
- 方針: **ロジックは単体/結合で高速検証、WPF画面はネイティブUI E2E（WinAppCli優先）で最小本数を安定運用**。

## 機能領域別テストマップ

| 機能領域 | 単体テスト | 結合テスト | UIテスト | Native UI E2E |
|---|---|---|---|---|
| 認証（Auth） | `AuthenticationServiceTests`, `AuthorizationServiceTests` | `CriticalWorkflowIntegrationTests.Auth_*`（SQLite初期ユーザー） | （今後）ログイン画面バインディング検証 | `smoke` にログインシナリオ |
| 発注コア（Order Core） | `OrderDomainTests`（金額/遷移/削除） | `OrderPersistenceIntegrationTests`, `CriticalWorkflowIntegrationTests.OrderCore_*` | （今後）一覧・詳細入力のUI操作 | `smoke/regression` に作成・編集・削除・テンプレート |
| 承認/在庫/予算 | ドメイン遷移ロジック | `ApprovalInventoryBudgetIntegrationTests`, `CriticalWorkflowIntegrationTests.CrossModule_*` | （今後）承認画面/予算画面の表示検証 | `smoke` に承認、`regression` に入荷・予算超過 |
| マスタ/分析 | 入力検証（CSV等） | `MasterAnalyticsIntegrationTests`, `CriticalWorkflowIntegrationTests.CrossModule_*` | （今後）商品・仕入先画面の操作検証 | `smoke` に商品/仕入先、`regression` に分析ダッシュボード |
| 運用設定（Ops Settings） | （現時点）未実装機能は契約テスト中心 | 予算設定の永続化は結合で担保 | （今後）設定画面UI | `smoke/regression` に **placeholder シナリオ契約**（バックアップ/会社情報/テーマ） |

## テスト技法の適用
- 同値分割/境界値: ログイン入力、ページングサイズ、閾値/予算、CSV値
- 状態遷移: 発注ステータス（未処理→承認→処理中→入荷待ち→完了）
- ユースケーステスト: 認証→マスタ→発注→承認→入荷→分析の業務横断フロー

## 実行レーン
1. PR品質ゲート: `dotnet test`（unit + integration）
2. 手動/任意E2E: WinAppCli `smoke` / `regression`（GitHub Actions workflow_dispatch）

## リスクと対策
- WinAppCli未導入環境: スクリプトで検知し、実行コマンド契約を明示してスキップ可能
- WPF要素特定の不安定化: AutomationId付与を継続し、文字列依存を縮小
- 未実装の運用設定機能: placeholderシナリオで先に契約化し、実装後に自動化へ昇格
