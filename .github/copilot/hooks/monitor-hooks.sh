#!/bin/bash

# Copilot Hooks - リアルタイムモニター
# ローカルに保存されたJSONファイルをリアルタイムで監視・表示

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WORKSPACE_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
OUTPUT_DIR="${COPILOT_OUTPUT_DIR:-$WORKSPACE_ROOT/.copilot-hooks}"

echo "=========================================="
echo "Copilot Hooks リアルタイムモニター"
echo "=========================================="
echo "監視ディレクトリ: $OUTPUT_DIR"
echo "Ctrl+C で終了"
echo ""

# ディレクトリを作成（存在しない場合）
mkdir -p "$OUTPUT_DIR"

# inotify-toolsがインストールされているか確認
if command -v inotifywait &> /dev/null; then
  echo "📡 inotifywait を使用してファイル変更を監視中..."
  echo ""
  
  # inotifywaitでファイル作成を監視
  inotifywait -m -e create -e moved_to --format '%f' "$OUTPUT_DIR" | while read -r filename; do
    if [[ "$filename" == *.json ]]; then
      filepath="$OUTPUT_DIR/$filename"
      echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
      echo "🆕 新しいフック: $(date '+%Y-%m-%d %H:%M:%S')"
      echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
      
      # JSONを整形して表示
      if command -v jq &> /dev/null; then
        cat "$filepath" | jq '.'
      else
        cat "$filepath"
      fi
      
      echo ""
    fi
  done
else
  echo "⚠️  inotify-tools がインストールされていません"
  echo "   インストール: sudo apt-get install inotify-tools"
  echo ""
  echo "📊 ポーリングモードで監視中（5秒間隔）..."
  echo ""
  
  # ポーリングで監視（フォールバック）
  last_count=0
  
  while true; do
    current_count=$(ls -1 "$OUTPUT_DIR"/*.json 2>/dev/null | wc -l)
    
    if [ "$current_count" -gt "$last_count" ]; then
      # 新しいファイルを表示
      new_files=$(ls -1t "$OUTPUT_DIR"/*.json 2>/dev/null | head -n $((current_count - last_count)))
      
      for filepath in $new_files; do
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo "🆕 新しいフック: $(date '+%Y-%m-%d %H:%M:%S')"
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        
        if command -v jq &> /dev/null; then
          cat "$filepath" | jq '.'
        else
          cat "$filepath"
        fi
        
        echo ""
      done
      
      last_count=$current_count
    fi
    
    sleep 5
  done
fi
