# Copilot CLI Orchestrator

GitHub Copilot CLIのカスタムエージェントのみで構成された開発プロセスオーケストレータ。

## 構成

```
.github/agents/
  orchestrator.agent.md     ← オーケストレータ（エントリポイント）
  01-requirements.agent.md  ← 要件定義
  02-task-planner.agent.md  ← タスク作成
  03-task-summary.agent.md  ← サマリ・クリティカルパス分析
  04-1-test-scenario.agent.md ← テストシナリオ作成
  04-2-test-writer.agent.md   ← テスト作成 (TDD Red)
  04-3-implementer.agent.md   ← 実装 (TDD Green)
  05-integration-test.agent.md ← 結合テスト・UATシナリオ
```

## 使い方

```bash
copilot --agent=orchestrator \
  -p "プロジェクト名: TODOアプリ\n説明: React + Express でTODOの作成・編集・削除・完了管理ができるWebアプリ" \
  --allow-all-tools
```

## ワークフロー

```
Phase 1        Phase 2        Phase 3        Phase 4                   Phase 5
要件定義  ───→ タスク作成 ───→ サマリ作成 ───→ 開発作業 ────────────→ 結合テスト
                                              │
                                              ├─ Task A → 4-1 → 4-2 → 4-3
                                              ├─ Task B → 4-1 → 4-2 → 4-3
                                              └─ Task C → 4-1 → 4-2 → 4-3
```

## 成果物

実行後 `work/output/` に以下が生成される:

```
work/
├── status.yaml                   ← 作業状況ファイル
└── output/
    ├── 01-requirements/          ← ビジネス要件・システム要件
    ├── 02-tasks/                 ← 開発タスク (task-001.md, ...)
    ├── 03-summary/               ← サマリ・実行順序
    ├── 04-development/           ← 開発成果物
    │   └── task-001/
    │       ├── test-scenario.md
    │       ├── tests/
    │       └── src/
    └── 05-integration/           ← 結合テスト・UATシナリオ
```

## アーキテクチャ

- **オーケストレータ**: `work/status.yaml` を管理し、`copilot --agent=<sub-agent> -p "..." --allow-all-tools` でサブエージェントをシェルコマンドとして同期実行する
- **サブエージェント**: 各フェーズの作業を実行し、完了後に `work/status.yaml` を更新する
- **オーケストレータは実装コードを一切変更しない** — ステータス管理とサブエージェント呼び出しのみ
