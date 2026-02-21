#!/bin/bash

# Copilot Hooks - 統計レポート生成
# 収集されたフックデータの統計情報を表示

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WORKSPACE_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
OUTPUT_DIR="${COPILOT_OUTPUT_DIR:-$WORKSPACE_ROOT/.copilot-hooks}"

echo "=========================================="
echo "Copilot Hooks 統計レポート"
echo "=========================================="
echo "データディレクトリ: $OUTPUT_DIR"
echo ""

if [ ! -d "$OUTPUT_DIR" ]; then
  echo "❌ 出力ディレクトリが存在しません: $OUTPUT_DIR"
  exit 1
fi

# 総ファイル数
total_files=$(ls -1 "$OUTPUT_DIR"/*.json 2>/dev/null | wc -l)

if [ "$total_files" -eq 0 ]; then
  echo "📭 収集されたデータがありません"
  exit 0
fi

echo "📊 基本統計"
echo "--------------------------------------"
echo "総イベント数: $total_files"
echo ""

# 最新・最古のイベント
oldest_file=$(ls -1t "$OUTPUT_DIR"/*.json 2>/dev/null | tail -n 1)
newest_file=$(ls -1t "$OUTPUT_DIR"/*.json 2>/dev/null | head -n 1)

if command -v jq &> /dev/null; then
  echo "最古のイベント: $(cat "$oldest_file" | jq -r '.metadata.timestamp // "N/A"')"
  echo "最新のイベント: $(cat "$newest_file" | jq -r '.metadata.timestamp // "N/A"')"
  echo ""
  
  # イベントタイプ別カウント（dataの最初のキーから推測）
  echo "📈 収集データの内訳"
  echo "--------------------------------------"
  
  # 各ファイルのデータ構造を解析
  declare -A event_types
  
  for file in "$OUTPUT_DIR"/*.json; do
    # dataオブジェクトのキーを取得して分類
    keys=$(cat "$file" | jq -r '.data | keys[]' 2>/dev/null | sort | tr '\n' ',' | sed 's/,$//')
    
    if [ -n "$keys" ]; then
      event_types["$keys"]=$((${event_types["$keys"]:-0} + 1))
    fi
  done
  
  # イベントタイプごとに表示
  for event_type in "${!event_types[@]}"; do
    count=${event_types[$event_type]}
    echo "  $count 件 - キー: $event_type"
  done
  
  echo ""
  
  # ファイルサイズの統計
  echo "💾 データサイズ"
  echo "--------------------------------------"
  total_size=$(du -sh "$OUTPUT_DIR" 2>/dev/null | cut -f1)
  avg_size=$(find "$OUTPUT_DIR" -name "*.json" -exec stat -f%z {} \; 2>/dev/null | awk '{sum+=$1; count++} END {if(count>0) print int(sum/count); else print 0}')
  
  if [ -z "$avg_size" ]; then
    # Linux用（statコマンドの形式が異なる）
    avg_size=$(find "$OUTPUT_DIR" -name "*.json" -exec stat -c%s {} \; 2>/dev/null | awk '{sum+=$1; count++} END {if(count>0) print int(sum/count); else print 0}')
  fi
  
  echo "合計サイズ: $total_size"
  echo "平均ファイルサイズ: $avg_size バイト"
  echo ""
  
  # 時系列分析（直近1時間、24時間、7日）
  echo "⏰ 時系列分析"
  echo "--------------------------------------"
  
  now=$(date +%s)
  hour_ago=$((now - 3600))
  day_ago=$((now - 86400))
  week_ago=$((now - 604800))
  
  count_1h=0
  count_24h=0
  count_7d=0
  
  for file in "$OUTPUT_DIR"/*.json; do
    timestamp=$(cat "$file" | jq -r '.metadata.timestamp // empty' 2>/dev/null)
    if [ -n "$timestamp" ]; then
      file_time=$(date -d "$timestamp" +%s 2>/dev/null || echo 0)
      
      if [ "$file_time" -ge "$hour_ago" ]; then
        count_1h=$((count_1h + 1))
      fi
      
      if [ "$file_time" -ge "$day_ago" ]; then
        count_24h=$((count_24h + 1))
      fi
      
      if [ "$file_time" -ge "$week_ago" ]; then
        count_7d=$((count_7d + 1))
      fi
    fi
  done
  
  echo "直近1時間: $count_1h 件"
  echo "直近24時間: $count_24h 件"
  echo "直近7日間: $count_7d 件"
  echo ""
  
  # 最近の5件を表示
  echo "📋 最近のイベント（最新5件）"
  echo "--------------------------------------"
  
  ls -1t "$OUTPUT_DIR"/*.json | head -n 5 | while read -r file; do
    timestamp=$(cat "$file" | jq -r '.metadata.timestamp // "N/A"')
    hook_id=$(cat "$file" | jq -r '.metadata.hook_id // "N/A"' | cut -c1-8)
    data_preview=$(cat "$file" | jq -c '.data' | cut -c1-60)
    
    echo "[$timestamp] ID:$hook_id..."
    echo "  $data_preview..."
    echo ""
  done
  
else
  echo "⚠️  jq がインストールされていません"
  echo "   詳細な統計情報を表示するには jq が必要です"
  echo "   インストール: sudo apt-get install jq"
  echo ""
  echo "ファイル一覧:"
  ls -lht "$OUTPUT_DIR"/*.json | head -n 10
fi

echo ""
echo "=========================================="
echo "📁 ファイル管理"
echo "=========================================="
echo ""
echo "コマンド例:"
echo "  全ファイルを表示: ls -lht $OUTPUT_DIR/*.json"
echo "  最新のファイル: cat \$(ls -1t $OUTPUT_DIR/*.json | head -n 1) | jq ."
echo "  古いファイルを削除: find $OUTPUT_DIR -name '*.json' -mtime +7 -delete"
echo ""
