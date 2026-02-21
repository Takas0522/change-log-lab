#!/bin/bash

# Copilot Hook: Save Hook Data to Local Storage
# VS Code Agent Hooks公式仕様に準拠
# このスクリプトはCopilot Hooksから呼び出され、入力データをタイムスタンプ付きディレクトリに保存します。

set -euo pipefail

# ワークスペースルートを取得
# Git リポジトリのルートディレクトリを優先、それ以外は相対パスから計算
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if command -v git &> /dev/null && git rev-parse --is-inside-work-tree &> /dev/null 2>&1; then
  # Gitリポジトリの場合はルートディレクトリを使用
  WORKSPACE_ROOT="$(git rev-parse --show-toplevel)"
else
  # 相対パスから計算（.github/copilot/hooks/ から3階層上）
  WORKSPACE_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
fi

# 設定
OUTPUT_BASE_DIR="${COPILOT_OUTPUT_DIR:-$WORKSPACE_ROOT/.copilot-hooks}"
TIMESTAMP=$(date -u +"%Y%m%d_%H%M%S")
TIMESTAMP_MILLIS=$(date -u +"%Y-%m-%dT%H:%M:%S.%3NZ")
UNIQUE_ID=$(uuidgen 2>/dev/null || cat /proc/sys/kernel/random/uuid 2>/dev/null || echo "$(date +%s)-$$")

# 標準入力からJSONデータを読み込み（VS Codeから自動的に渡される）
INPUT_JSON=$(cat)

# フックイベント名を抽出（存在する場合）
HOOK_EVENT_NAME=$(echo "$INPUT_JSON" | jq -r '.hookEventName // "Unknown"' 2>/dev/null || echo "Unknown")

# タイムスタンプディレクトリを作成
TIMESTAMP_DIR="$OUTPUT_BASE_DIR/$TIMESTAMP"
mkdir -p "$TIMESTAMP_DIR"

# ログ用にデバッグ情報を追加（標準エラー出力に）
echo "[$(date)] Hook triggered: $HOOK_EVENT_NAME" >&2
echo "[$(date)] Input size: $(echo "$INPUT_JSON" | wc -c) bytes" >&2
echo "[$(date)] Output directory: $TIMESTAMP_DIR" >&2

# Hookメタデータを含む完全なJSONペイロードを構築
PAYLOAD=$(jq -n \
  --arg captured_timestamp "$TIMESTAMP_MILLIS" \
  --arg hook_id "$UNIQUE_ID" \
  --arg hostname "$(hostname)" \
  --arg event_name "$HOOK_EVENT_NAME" \
  --argjson input "$INPUT_JSON" \
  '{
    metadata: {
      hook_id: $hook_id,
      captured_timestamp: $captured_timestamp,
      hostname: $hostname,
      hook_event_name: $event_name,
      hook_version: "1.0.0"
    },
    original_input: $input
  }')

# JSONファイルに保存（タイムスタンプディレクトリ内）
OUTPUT_FILE="$TIMESTAMP_DIR/${HOOK_EVENT_NAME}-${UNIQUE_ID}.json"
echo "$PAYLOAD" > "$OUTPUT_FILE"
echo "[$(date)] Saved to: $OUTPUT_FILE" >&2

# VS Code Hooks仕様に準拠した出力を返す
# 成功時：continueをtrueに設定
# 標準出力にJSONを返す（VS Codeが解析）
jq -n \
  --arg message "Hook data saved successfully" \
  --arg output_file "$OUTPUT_FILE" \
  --arg timestamp_dir "$TIMESTAMP_DIR" \
  '{
    continue: true,
    systemMessage: $message,
    hookSpecificOutput: {
      output_file: $output_file,
      timestamp_directory: $timestamp_dir,
      status: "success"
    }
  }'

# 成功終了
exit 0

