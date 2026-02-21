````skill
---
name: github-issue-progress
description: ghコマンドを使用してGitHub Issueを参照し、プロジェクトの現在の進捗状態を確認・集計・レポートするスキル。Issue一覧の取得、ラベル・マイルストーン・担当者によるフィルタリング、進捗サマリーの生成を行う。スプリント進捗確認、リリース準備状況確認、チーム作業状況把握時に使用する。
---

# GitHub Issue 進捗確認スキル

`gh` CLI を使用してGitHub Issueを参照し、プロジェクトの現在の進捗状態を体系的に確認・集計するスキル。

## When to Use This Skill

- **スプリント進捗確認**: 現在のスプリントにおけるIssueの完了状況を把握する
- **リリース準備確認**: マイルストーン単位での未完了Issueを特定する
- **チーム作業状況把握**: 担当者別のIssue状況を確認する
- **ブロッカー特定**: 進行を妨げるIssueを早期に発見する
- **定期レポート生成**: 週次・月次などの進捗レポートを作成する

---

## 前提条件

### gh CLI のセットアップ確認

```bash
# gh コマンドの存在確認
gh --version

# 認証状態の確認
gh auth status
```

認証が完了していない場合:

```bash
gh auth login
```

### リポジトリコンテキストの確認

```bash
# カレントリポジトリの確認
gh repo view --json nameWithOwner -q .nameWithOwner
```

---

## Issue 取得コマンド集

### 基本的なIssue一覧取得

```bash
# オープンなIssue一覧（デフォルト30件）
gh issue list

# クローズ済みIssue一覧
gh issue list --state closed

# すべてのIssue（オープン＋クローズ）
gh issue list --state all

# 件数を増やして取得（最大1000件）
gh issue list --limit 1000 --state all
```

### フィルタリング

```bash
# ラベルで絞り込み
gh issue list --label "bug"
gh issue list --label "enhancement" --label "priority:high"

# マイルストーンで絞り込み
gh issue list --milestone "v1.0"
gh issue list --milestone "Sprint-3"

# 担当者で絞り込み
gh issue list --assignee "@me"
gh issue list --assignee "username"

# 担当者なし（未アサイン）
gh issue list --assignee ""

# 組み合わせ
gh issue list --milestone "Sprint-3" --state all
```

### JSON形式での取得（集計・分析用）

```bash
# 利用可能なフィールド一覧確認
gh issue list --json --help 2>&1 | head -30

# 主要フィールドを JSON で取得
gh issue list --state all --limit 500 --json \
  number,title,state,labels,assignees,milestone,createdAt,closedAt,url

# jq で加工：オープンIssueのみ抽出
gh issue list --state all --limit 500 --json \
  number,title,state,labels,assignees,milestone \
  | jq '[.[] | select(.state == "OPEN")]'

# jq でラベル別集計
gh issue list --state all --limit 500 --json number,state,labels \
  | jq 'group_by(.labels[0].name) | map({label: .[0].labels[0].name, count: length})'
```

---

## 進捗状態の確認手順

### ステップ 1: 全体サマリーの取得

```bash
# オープン件数
OPEN=$(gh issue list --state open --limit 1 --json number | jq 'length')

# クローズ件数
CLOSED=$(gh issue list --state closed --limit 1000 --json number | jq 'length')

echo "Open: $OPEN / Closed: $CLOSED"
```

より正確な件数取得:

```bash
# state:open と state:closed を別々に取得して集計
gh issue list --state open --limit 1000 --json number | jq 'length'
gh issue list --state closed --limit 1000 --json number | jq 'length'
```

### ステップ 2: マイルストーン別進捗

```bash
# マイルストーン一覧
gh api repos/{owner}/{repo}/milestones --jq '.[] | {title: .title, open: .open_issues, closed: .closed_issues}'

# カレントリポジトリで実行
gh api "repos/$(gh repo view --json nameWithOwner -q .nameWithOwner)/milestones" \
  --jq '.[] | "\(.title): open=\(.open_issues), closed=\(.closed_issues), due=\(.due_on)"'
```

### ステップ 3: ラベル別進捗

```bash
# ラベル別のオープンIssue数
gh issue list --state open --limit 1000 --json labels \
  | jq '[.[].labels[].name] | group_by(.) | map({label: .[0], count: length}) | sort_by(-.count)'
```

### ステップ 4: 担当者別進捗

```bash
# 担当者別のオープンIssue一覧
gh issue list --state open --limit 1000 --json number,title,assignees \
  | jq 'group_by(.assignees[0].login) 
      | map({assignee: (.[0].assignees[0].login // "unassigned"), issues: map({number: .number, title: .title}), count: length})'
```

### ステップ 5: 個別Issueの詳細確認

```bash
# Issue詳細（コメント含む）
gh issue view <issue-number>

# JSON形式で詳細取得
gh issue view <issue-number> --json \
  number,title,state,body,labels,assignees,milestone,comments,createdAt,closedAt
```

---

## 進捗レポートの生成

### 標準進捗レポート生成スクリプト

以下のスクリプトで標準的な進捗レポートを出力する:

```bash
#!/bin/bash
set -euo pipefail

REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner)
DATE=$(date '+%Y-%m-%d')

echo "# 進捗レポート: ${REPO}"
echo "生成日時: ${DATE}"
echo ""

# --- 全体サマリー ---
echo "## 全体サマリー"
OPEN=$(gh issue list --state open --limit 1000 --json number | jq 'length')
CLOSED=$(gh issue list --state closed --limit 1000 --json number | jq 'length')
TOTAL=$((OPEN + CLOSED))
if [ "$TOTAL" -gt 0 ]; then
  PCT=$((CLOSED * 100 / TOTAL))
else
  PCT=0
fi
echo "- オープン: ${OPEN}"
echo "- クローズ: ${CLOSED}"
echo "- 完了率: ${PCT}% (${CLOSED}/${TOTAL})"
echo ""

# --- マイルストーン別サマリー ---
echo "## マイルストーン別進捗"
gh api "repos/${REPO}/milestones?state=open&per_page=20" \
  --jq '.[] | "- **\(.title)**: \(.closed_issues)/\(.open_issues + .closed_issues) 完了 (due: \(.due_on // "未設定"))"'
echo ""

# --- ラベル別オープンIssue ---
echo "## ラベル別 オープンIssue"
gh issue list --state open --limit 1000 --json labels \
  | jq -r '[.[].labels[].name] | group_by(.) | map("- \(.[0]): \(length)件") | .[]'
echo ""

# --- 未アサインのオープンIssue ---
echo "## 未アサインのオープンIssue"
gh issue list --state open --limit 1000 --json number,title,assignees \
  | jq -r '.[] | select(.assignees | length == 0) | "- #\(.number) \(.title)"'
echo ""

echo "---"
echo "レポート完了"
```

---

## よく使うフィルタパターン

### ブロッカー・高優先度Issueの確認

```bash
# "blocked" または "priority:high" ラベル
gh issue list --label "blocked"
gh issue list --label "priority:high" --state open

# 複数ラベル（AND条件）
gh issue list --label "bug" --label "priority:critical"
```

### 直近の更新確認

```bash
# 更新日時と一緒に表示
gh issue list --state open --limit 50 --json number,title,updatedAt \
  | jq 'sort_by(.updatedAt) | reverse | .[] | "\(.updatedAt[:10]) #\(.number) \(.title)"' -r
```

### 停滞Issueの特定（7日以上更新なし）

```bash
THRESHOLD=$(date -d '7 days ago' '+%Y-%m-%dT%H:%M:%SZ' 2>/dev/null \
  || date -v-7d '+%Y-%m-%dT%H:%M:%SZ')  # macOS対応

gh issue list --state open --limit 1000 --json number,title,updatedAt \
  | jq --arg t "$THRESHOLD" \
    '[.[] | select(.updatedAt < $t)] | sort_by(.updatedAt) | .[] | "#\(.number) \(.title) (last: \(.updatedAt[:10]))"' -r
```

### Issue検索（キーワード）

```bash
# タイトル・本文からキーワード検索
gh issue list --search "authentication" --state open
gh issue list --search "label:bug is:open" 
```

---

## 出力レポートフォーマット

レポートは以下のテンプレートで構造化する:

```markdown
# 進捗レポート: {リポジトリ名}
生成日時: {YYYY-MM-DD}

## 全体サマリー
- オープン: {N}件
- クローズ: {N}件  
- 完了率: {N}% ({closed}/{total})

## マイルストーン別進捗
| マイルストーン | 完了 | 未完了 | 完了率 | 期限 |
|---|---|---|---|---|
| {name} | {closed} | {open} | {pct}% | {due_on} |

## ラベル別 オープンIssue
| ラベル | 件数 |
|---|---|
| {label} | {count} |

## 未アサインのオープンIssue
- #{number} {title}

## ブロッカー / 高優先度
- #{number} {title} [@{assignee}]

## 停滞Issue（{N}日以上更新なし）
- #{number} {title}（最終更新: {date}）
```

---

## エラーハンドリング

### 認証エラー

```bash
# エラー: "To get started with GitHub CLI, please run:  gh auth login"
gh auth login --web
# または
gh auth login --with-token <<< "$GITHUB_TOKEN"
```

### レート制限

```bash
# API レート制限の確認
gh api rate_limit --jq '.resources.core | {limit, remaining, reset: (.reset | todate)}'
```

### リポジトリが見つからない場合

```bash
# 明示的にリポジトリを指定
gh issue list --repo owner/repo-name --state open
```

---

## Tips

- `--limit` のデフォルトは **30件** のため、全件取得には `--limit 1000` を指定する
- `gh issue list` はページネーションを自動処理しないため、大規模リポジトリでは API 直叩きを検討:
  ```bash
  gh api "repos/{owner}/{repo}/issues?state=all&per_page=100&page=1"
  ```
- JSON フィールドの選択肢は `gh issue list --json` のみ実行すると一覧表示される
- `jq` がない環境では `--template` オプションで Go テンプレートを使用できる:
  ```bash
  gh issue list --template '{{range .}}#{{.number}} {{.title}}{{"\n"}}{{end}}'
  ```
````
