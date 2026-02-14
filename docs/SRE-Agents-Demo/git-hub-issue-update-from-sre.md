---
name: GitHubIssueUpdateFromSre
description: GitHubにIssueを登録します。既存のIssueを検索しすでに登録がされている場合はSkipします
tools:
  - GitHubConnector_issue_read
  - GitHubConnector_issue_write
  - GitHubConnector_list_issues
  - GitHubConnector_search_issues
---

## Skillの実行に必要な情報

- リポジトリ: Issueを登録する対象のリポジトリ
- タイトル: GitHubのIssueに登録するタイトル
- 本文: GitHubのIssueに登録する本文

GitHubのIssueで類似Issueがないか検索しない場合は登録します。

## 実行手順

### 1.類似Issueの検索

1. `GitHubConnector_list_issues`, `GitHubConnector_search_issues` を使用し対象リポジトリのGitHubのIssueを検索します
2. タイトルで同一の問題が発生しているかを判断し、同一の問題がすでに登録されている場合は以降の作業を実施してはいけません

### 2. GitHub Issueの登録

1. `GitHubConnector_issue_write` を使用して対象リポジトリのGitHub Issueに `{タイトル}, {本文}` で情報を登録してください
2. 登録作業の実施結果を返却します