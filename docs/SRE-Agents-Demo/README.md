# SRE-Agents-Demo

Azure SRE Agent のトリガー処理で使用している SubAgent・SKILL の構成ドキュメントです。  
Application Insights からエラーを検知し、GitHub Issue に自動登録するまでの一連のワークフローを定義しています。

## 処理フロー

```mermaid
flowchart TD
    A["⏰ trigger.md\n(Daily スケジュール)"] -->|起動| B["🔍 ApplicationInsightDataGet\n(SubAgent)"]
    B -->|"SKILL呼び出し"| C["📊 WorkspaceDataGet\n(SKILL)"]
    C -->|"KQL構築・実行"| D{{"QueryLogAnalytics\nツール"}}
    D -->|"ResourceId指定"| D1["QueryLogAnalyticsByResourceId"]
    D -->|"WorkspaceId指定"| D2["QueryLogAnalyticsByWorkspaceId"]
    D1 -->|"結果返却"| C
    D2 -->|"結果返却"| C
    C -->|"未加工データ返却"| B
    B -->|"エラーなし"| E["✅ 終了\n(異常なし)"]
    B -->|"エラーあり\n(handoff)"| F["📝 GitHubIssueCheckAndSubmit\n(SubAgent)"]
    F -->|"エラー情報整形\nSKILL呼び出し"| G["🐙 GitHubIssueUpdateFromSre\n(SKILL)"]
    G -->|"類似Issue検索"| H{{"GitHub Connector\nツール"}}
    H -->|"search/list"| H1["GitHubConnector_search_issues\nGitHubConnector_list_issues"]
    H1 -->|"重複あり"| I["⏭️ Skip\n(登録済み)"]
    H1 -->|"重複なし"| H2["GitHubConnector_issue_write"]
    H2 -->|"Issue登録完了"| J["✅ 終了\n(Issue登録済み)"]

    style A fill:#4a90d9,color:#fff
    style B fill:#f5a623,color:#fff
    style F fill:#f5a623,color:#fff
    style C fill:#7ed321,color:#fff
    style G fill:#7ed321,color:#fff
    style E fill:#9b9b9b,color:#fff
    style I fill:#9b9b9b,color:#fff
    style J fill:#50e3c2,color:#fff
```

## ファイル構成

| ファイル | 種別 | 役割 |
|---|---|---|
| `trigger.md` | **トリガー定義** | Daily スケジュールで起動。Application Insight `api-set-agent-lab-takas` の直近1日間のエラーを確認し、エラー発生時は `GitHubIssueCheckAndSubmit` へ引き渡す |
| `application-insight-data-get.yml` | **SubAgent** | `ApplicationInsightDataGet` — Log Analytics ワークスペース (`law-ai-takas-jpe`) から直近1日間の Exception 情報を収集。SKILL `WorkspaceDataGet` を使用。完了後 `GitHubIssueCheckAndSubmit` へ handoff |
| `github-issue-check-and-submit.yml` | **SubAgent** | `GitHubIssueCheckAndSubmit` — 受け取ったエラー情報を整形し、SKILL `GitHubIssueUpdateFromSre` で `Takas0522/change-log-lab` リポジトリに Issue を登録 |
| `workspace-data-get.md` | **SKILL** | `WorkspaceDataGet` — KQL を構築し、`QueryLogAnalyticsByResourceId` / `QueryLogAnalyticsByWorkspaceId` ツールで Log Analytics からデータを取得して未加工のまま返却 |
| `git-hub-issue-update-from-sre.md` | **SKILL** | `GitHubIssueUpdateFromSre` — GitHub Connector ツール群で類似 Issue を検索し、重複がなければ新規 Issue を登録 |

## SubAgent と SKILL の関係

- **SubAgent（`.yml`）**: `azuresre.ai/v1` API で定義された自律型エージェント。`handoffs` で次の SubAgent へ遷移し、`allowed_skills` で使用可能な SKILL を制限する
- **SKILL（`.md`）**: frontmatter に `name`, `description`, `tools` を持つ再利用可能な処理単位。データ取得や外部サービス連携など単一責任の作業を担う

## 使用ツール一覧

| ツール名 | 使用元 SKILL |
|---|---|
| `QueryLogAnalyticsByResourceId` | WorkspaceDataGet |
| `QueryLogAnalyticsByWorkspaceId` | WorkspaceDataGet |
| `GitHubConnector_issue_read` | GitHubIssueUpdateFromSre |
| `GitHubConnector_issue_write` | GitHubIssueUpdateFromSre |
| `GitHubConnector_list_issues` | GitHubIssueUpdateFromSre |
| `GitHubConnector_search_issues` | GitHubIssueUpdateFromSre |
