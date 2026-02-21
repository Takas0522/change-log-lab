#!/bin/bash

# Copilot Hooks テストスクリプト
# VS Code Agent Hooks公式仕様に準拠したテスト

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HOOK_SCRIPT="$SCRIPT_DIR/send-to-blob.sh"

echo "=========================================="
echo "Copilot Hooks 全イベントテスト"
echo "VS Code Agent Hooks 公式仕様準拠"
echo "=========================================="
echo ""

# テストデータファイルの配列（イベント名に対応）
declare -a test_files=(
  "test-data-chat-request.json:SessionStart"
  "test-data-chat.json:UserPromptSubmit"
  "test-data-chat-response.json:PreToolUse"
  "test-data-inline-shown.json:PreToolUse (readFile)"
  "test-data-code.json:PostToolUse (editFiles)"
  "test-data-inline-accepted.json:PostToolUse (runCommand)"
  "test-data-editor-change.json:PreCompact"
  "test-data-copilot-enabled.json:SubagentStart"
  "test-data-editor-focus.json:SubagentStop"
  "test-data-workspace-open.json:Stop"
)

test_count=0
success_count=0
fail_count=0

# 各テストを実行
for test_item in "${test_files[@]}"; do
  IFS=':' read -r test_file test_name <<< "$test_item"
  test_count=$((test_count + 1))
  
  echo "テスト $test_count: $test_name"
  echo "--------------------------------------"
  
  if [ ! -f "$SCRIPT_DIR/$test_file" ]; then
    echo "⚠️  テストファイルが見つかりません: $test_file"
    fail_count=$((fail_count + 1))
    echo ""
    continue
  fi
  
  # テストデータの内容を表示
  if command -v jq &> /dev/null; then
    echo "入力: $(cat "$SCRIPT_DIR/$test_file" | jq -c '.')"
  fi
  
  # フックスクリプトを実行
  if output=$(cat "$SCRIPT_DIR/$test_file" | "$HOOK_SCRIPT" 2>&1); then
    echo "✅ テスト成功: $test_name"
    success_count=$((success_count + 1))
    
    # 出力を表示
    if command -v jq &> /dev/null && [ -n "$output" ]; then
      echo "出力: $(echo "$output" | jq -c '.' 2>/dev/null || echo "$output")"
    fi
  else
    exit_code=$?
    echo "❌ テスト失敗: $test_name (終了コード: $exit_code)"
    fail_count=$((fail_count + 1))
  fi
  
  echo ""
  sleep 0.5
done

# ローカルファイル確認
OUTPUT_DIR="${COPILOT_OUTPUT_DIR:-$(cd "$SCRIPT_DIR/../../.." && pwd)/.copilot-hooks}"
echo "ローカル出力ファイル一覧:"
echo "--------------------------------------"
if [ -d "$OUTPUT_DIR" ]; then
  file_count=$(ls -1 "$OUTPUT_DIR"/*.json 2>/dev/null | wc -l)
  if [ "$file_count" -gt 0 ]; then
    ls -lht "$OUTPUT_DIR"/*.json | head -n 20
    echo ""
    echo "合計: $file_count ファイル"
    
    # 最新のファイルを表示
    if command -v jq &> /dev/null; then
      latest_file=$(ls -1t "$OUTPUT_DIR"/*.json | head -n 1)
      echo ""
      echo "最新のファイル内容:"
      echo "--------------------------------------"
      cat "$latest_file" | jq '.'
    fi
  else
    echo "JSONファイルが見つかりませんでした"
  fi
else
  echo "出力ディレクトリが存在しません: $OUTPUT_DIR"
fi
echo ""

echo "=========================================="
echo "テスト結果サマリー"
echo "=========================================="
echo "総テスト数: $test_count"
echo "成功: $success_count"
echo "失敗: $fail_count"
echo ""

if [ $fail_count -eq 0 ]; then
  echo "🎉 すべてのテストが成功しました！"
else
  echo "⚠️  一部のテストが失敗しました"
fi

echo ""
echo "次のステップ:"
echo "1. $OUTPUT_DIR 内のタイムスタンプディレクトリを確認"
echo "   例: ls -lh \$(ls -1dt $OUTPUT_DIR/*/ | head -n 1)"
echo "2. 最新のフックデータを表示"
echo "   例: cat \$(find $OUTPUT_DIR -name '*.json' -type f | sort -r | head -n 1) | jq ."
echo "3. VS Code でCopilotエージェントを使用してフックが自動実行されるか確認"
echo "   - チャットで質問してみる (SessionStart, UserPromptSubmit)"
echo "   - ツールを使用させる (PreToolUse, PostToolUse)"
echo "4. フックの診断を確認"
echo "   - チャットビューで右クリック → Diagnostics"
echo "5. フックの出力を確認"
echo "   - Output パネル → GitHub Copilot Chat Hooks"
echo ""
