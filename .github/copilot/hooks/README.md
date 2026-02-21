# Copilot カスタムフック

このディレクトリには、GitHub Copilot / VS Code Agent Hooksの設定とスクリプトが含まれています。

## 概要

VS Code Agent Hooksを使用して、エージェントセッションのライフサイクルイベントでカスタムシェルコマンドを実行します。収集したデータをタイムスタンプ付きディレクトリに自動保存します。

## ファイル構成

- `hooks.json` - フック定義ファイル（公式仕様準拠）
- `send-to-blob.sh` - データ保存スクリプト
- `test-*.json` - テストデータファイル
- `test-hook.sh` - テストスクリプト
- `monitor-hooks.sh` - リアルタイムモニター
- `stats-hooks.sh` - 統計レポート生成
- `README.md` - このファイル

## フックの種類

VS Codeは8つのライフサイクルイベントをサポートしています：

### 1. SessionStart
- **トリガー**: 新しいエージェントセッションが開始された時
- **用途**: リソースの初期化、セッション開始のログ記録、プロジェクト状態の検証
- **入力フィールド**: `source` (常に "new")
- **出力**: `additionalContext` でエージェントの会話にコンテキストを注入可能

### 2. UserPromptSubmit
- **トリガー**: ユーザーがプロンプトを送信した時
- **用途**: ユーザーリクエストの監査、システムコンテキストの注入
- **入力フィールド**: `prompt` (ユーザーが送信したテキスト)

### 3. PreToolUse
- **トリガー**: エージェントがツールを呼び出す前
- **用途**: 危険な操作のブロック、承認要求、ツール入力の変更
- **入力フィールド**: `tool_name`, `tool_input`, `tool_use_id`
- **出力**: `permissionDecision` ("allow", "deny", "ask"), `updatedInput`, `additionalContext`

### 4. PostToolUse
- **トリガー**: ツールが正常に完了した後
- **用途**: フォーマッターの実行、結果のログ記録、フォローアップアクションのトリガー
- **入力フィールド**: `tool_name`, `tool_input`, `tool_use_id`, `tool_response`
- **出力**: `decision` ("block"), `additionalContext`

### 5. PreCompact
- **トリガー**: 会話コンテキストが圧縮される前
- **用途**: 重要なコンテキストのエクスポート、切り捨て前の状態保存
- **入力フィールド**: `trigger` (圧縮がトリガーされた方法、例: "auto")

### 6. SubagentStart
- **トリガー**: サブエージェントが生成された時
- **用途**: ネストされたエージェント使用の追跡、サブエージェントリソースの初期化
- **入力フィールド**: `agent_id`, `agent_type` (例: "Plan")
- **出力**: `additionalContext` でサブエージェントの会話にコンテキストを注入可能

### 7. SubagentStop
- **トリガー**: サブエージェントが完了した時
- **用途**: 結果の集約、サブエージェントリソースのクリーンアップ
- **入力フィールド**: `agent_id`, `agent_type`, `stop_hook_active`
- **出力**: `decision` ("block") でサブエージェントの停止を防止可能

### 8. Stop
- **トリガー**: エージェントセッションが終了する時
- **用途**: レポート生成、リソースのクリーンアップ、通知の送信
- **入力フィールド**: `stop_hook_active`
- **出力**: `decision` ("block") でエージェントの停止を防止可能

## 共通入力フィールド

すべてのフックはstdin経由で以下のフィールドを受け取ります：

```json
{
  "timestamp": "2026-02-18T10:30:00.000Z",
  "cwd": "/path/to/workspace",
  "sessionId": "session-identifier",
  "hookEventName": "PreToolUse",
  "transcript_path": "/path/to/transcript.json"
}
```

## 共通出力フォーマット

フックはstdout経由でJSONを返し、エージェントの動作に影響を与えることができます：

```json
{
  "continue": true,
  "stopReason": "Security policy violation",
  "systemMessage": "Operation blocked by security hook"
}
```

### 終了コード

- `0`: 成功 - stdoutをJSONとして解析
- `2`: ブロッキングエラー - 処理を停止し、モデルにエラーを表示
- その他: 非ブロッキング警告 - ユーザーに警告を表示し、処理を継続

## セットアップ

### 1. フックファイルの配置

VS Codeは以下の場所でフック設定ファイルを検索します：

- **ワークスペース**: `.github/hooks/*.json` - チームで共有するプロジェクト固有のフック ✓（**このプロジェクト - 重要！**）
- ワークスペース: `.claude/settings.local.json` - ローカルワークスペースフック（コミット不要）
- ワークスペース: `.claude/settings.json` - ワークスペースレベルのフック
- ユーザー: `~/.claude/settings.json` - すべてのワークスペースに適用される個人フック

**重要**: このプロジェクトでは、フック設定ファイルは以下の2箇所に配置されています：
- **実行用**: `.github/hooks/hooks.json` と `.github/hooks/send-to-blob.sh` ← VS Codeがここを認識
- **開発用**: `.github/copilot/hooks/` ← テストスクリプトや追加ファイルを管理

### 1.1. VS Codeでのフック認識

フックを有効にするには：
1. VS Codeを再起動
2. チャットビューで右クリック → **Diagnostics** を選択
3. "hooks" セクションでフックが認識されているか確認
4. `/hooks` コマンドでフック一覧を確認

### 2. 出力ディレクトリの設定（オプション）

デフォルトでは **ワークスペースルート**の `.copilot-hooks/` にデータが保存されます。
環境変数 `COPILOT_OUTPUT_DIR` で変更可能：

```bash
export COPILOT_OUTPUT_DIR="/custom/path/copilot-hooks"
```

### 3. スクリプトに実行権限を付与

```bash
chmod +x .github/copilot/hooks/send-to-blob.sh
chmod +x .github/copilot/hooks/test-hook.sh
chmod +x .github/copilot/hooks/monitor-hooks.sh
chmod +x .github/copilot/hooks/stats-hooks.sh
```

### 4. 必要なツールの確認

スクリプトは以下のツールを使用します：
- `jq` - JSON処理
- `uuidgen` または `/proc/sys/kernel/random/uuid` - ユニークID生成

インストールコマンド（Debian/Ubuntu）：
```bash
sudo apt-get install jq uuid-runtime
```

## 使用方法

### 自動実行

VS Code Copilotエージェントを使用すると、設定されたライフサイクルイベントで自動的にフックが実行されます。

### フック設定の確認

VS Codeで `/hooks` コマンドを使用して、インタラクティブUIでフックを設定・確認できます：

1. チャット入力で `/hooks` と入力してEnterを押す
2. リストからフックイベントタイプを選択
3. 既存のフックを編集するか、新しいフックを追加
4. フック設定ファイルを選択または作成

### フック診断の表示

どのフックがロードされているか、設定エラーがないかを確認：

1. チャットビューで右クリック → Diagnostics を選択
2. hooksセクションを確認

### フック出力の表示

フックの出力とエラーを確認：

1. Output パネルを開く
2. チャンネルリストから "GitHub Copilot Chat Hooks" を選択

### 手動テスト

スクリプトを手動でテストする場合：

```bash
# すべてのテストを実行
.github/copilot/hooks/test-hook.sh

# 個別のテスト
echo '{"timestamp":"2026-02-18T10:00:00.000Z","cwd":"/workspace","sessionId":"test","hookEventName":"SessionStart","transcript_path":"/tmp/test.json","source":"new"}' | \
  .github/copilot/hooks/send-to-blob.sh

# リアルタイムモニター
.github/copilot/hooks/monitor-hooks.sh

# 統計レポート
.github/copilot/hooks/stats-hooks.sh
```

## 出力

### ローカルストレージ

デフォルトでは `/tmp/copilot-hooks/` にJSONファイルが保存されます。
環境変数 `COPILOT_OUTPUT_DIR` で変更可能：

```bash
export COPILOT_OUTPUT_DIR="/var/log/copilot-hooks"
```

## データ形式

保存されるJSONデータの形式：

### 権限エラー
- フックスクリプトに実行権限があるか確認：`chmod +x script.sh`
- スクリプトのshebang（`#!/bin/bash`）が正しいか確認

### タイムアウトエラー
- `timeout` 値を増やす（デフォルトは30秒）
- フックスクリプトを最適化

### JSON解析エラー
- フックスクリプトが有効なJSONをstdoutに出力しているか確認
- `jq` またはJSONライブラリを使用して出力を構築
- デバッグ情報は標準エラー出力（stderr）に出力

### ディスク容量不足
- 出力ディレクトリのディスク容量を確認
- 古いタイムスタンプディレクトリを定期的に削除

### ログの確認
```bash
# VS Code Output パネル
# GitHub Copilot Chat Hooks チャンネルを選択

# ローカルファイルの確認
ls -lht /tmp/copilot-hooks/

# 最新のディレクトリを確認
ls -lh $(ls -1dt /tmp/copilot-hooks/*/ | head -n 1)

### 最新のフックデータを表示
cat $(find .copilot-hooks -name "*.json" -type f | sort -r | head -n 1) | jq .
```
```

## セキュリティ考慮事項

⚠️ **重要**: フックはVS Codeと同じ権限でシェルコマンドを実行します。特に信頼できないソースからのフックは慎重に確認してください。

- **フックスクリプトを確認**: 有効化する前に、特に共有リポジトリ内のすべてのフックスクリプトを検査
- **権限を制限**: 最小権限の原則を使用。フックは必要なアクセスのみを持つべき
- **入力を検証**: フックスクリプトはエージェントから入力を受け取る。インジェクション攻撃を防ぐために、すべての入力を検証・サニタイズ
- **認証情報を保護**: フックスクリプトにシークレットをハードコードしない。環境変数または安全な認証情報ストレージを使用
- **エージェントによるフックスクリプトの編集**: エージェントがフックによって実行されるスクリプトを編集できる場合、実行中に自身が書いたコードを変更・実行できる。`chat.tools.edits.autoApprove` を使用して手動承認なしでフックスクリプトを編集できないようにすることを推奨

## 参考資料

- [VS Code Agent Hooks 公式ドキュメント](https://code.visualstudio.com/docs/copilot/customization/hooks)
- [VS Code エージェントでツールを使用](https://code.visualstudio.com/docs/copilot/agents/agent-tools)
- [カスタムエージェント](https://code.visualstudio.com/docs/copilot/customization/custom-agents)
- [サブエージェント](https://code.visualstudio.com/docs/copilot/agents/subagents)
