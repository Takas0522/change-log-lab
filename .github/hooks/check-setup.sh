#!/bin/bash

# VS Code Agent Hooks 診断スクリプト
# フックが正しく設定されているか確認

echo "=========================================="
echo "VS Code Agent Hooks 診断"
echo "=========================================="
echo ""

# 1. フックファイルの存在確認
echo "✓ フックファイルの確認"
echo "--------------------------------------"

if [ -f ".github/hooks/hooks.json" ]; then
  echo "✓ .github/hooks/hooks.json が存在します"
else
  echo "❌ .github/hooks/hooks.json が見つかりません"
  echo "   実行: cp .github/copilot/hooks/hooks.json .github/hooks/"
  exit 1
fi

if [ -f ".github/hooks/send-to-blob.sh" ]; then
  echo "✓ .github/hooks/send-to-blob.sh が存在します"
else
  echo "❌ .github/hooks/send-to-blob.sh が見つかりません"
  echo "   実行: cp .github/copilot/hooks/send-to-blob.sh .github/hooks/"
  exit 1
fi

echo ""

# 2. 実行権限の確認
echo "✓ 実行権限の確認"
echo "--------------------------------------"

if [ -x ".github/hooks/send-to-blob.sh" ]; then
  echo "✓ send-to-blob.sh に実行権限があります"
else
  echo "⚠️  send-to-blob.sh に実行権限がありません"
  echo "   実行: chmod +x .github/hooks/send-to-blob.sh"
  chmod +x .github/hooks/send-to-blob.sh
  echo "   → 実行権限を付与しました"
fi

echo ""

# 3. JSON構文確認
echo "✓ hooks.json の構文確認"
echo "--------------------------------------"

if command -v jq &> /dev/null; then
  if jq empty .github/hooks/hooks.json 2>/dev/null; then
    echo "✓ JSON構文は正しいです"
    
    # フック数をカウント
    hook_count=$(jq '.hooks | length' .github/hooks/hooks.json)
    echo "✓ 定義されているフックイベント: $hook_count 個"
    
    # フックイベント名とコマンドパスを表示
    jq -r '.hooks | keys[]' .github/hooks/hooks.json | while read event; do
      command_path=$(jq -r ".hooks.\"$event\"[0].command" .github/hooks/hooks.json)
      echo "  - $event → $command_path"
    done
    
    echo ""
    
    # コマンドパスの確認
    echo "✓ フックスクリプトパスの確認"
    first_command=$(jq -r '.hooks | .[keys[0]][0].command' .github/hooks/hooks.json)
    if [[ "$first_command" == .github/hooks/* ]]; then
      echo "✓ コマンドパスはワークスペースルートからの相対パスです: $first_command"
    elif [[ "$first_command" == ./* ]]; then
      echo "⚠️  コマンドパスが相対パス './...' になっています"
      echo "   VS Codeから実行される際はワークスペースルートがカレントディレクトリなので"
      echo "   '.github/hooks/send-to-blob.sh' のように指定してください"
    else
      echo "ℹ️  コマンドパス: $first_command"
    fi
  else
    echo "❌ JSON構文エラーがあります"
    jq empty .github/hooks/hooks.json
    exit 1
  fi
else
  echo "⚠️  jq がインストールされていないため構文確認をスキップ"
fi

echo ""

# 4. 出力ディレクトリの確認
echo "✓ 出力ディレクトリの確認"
echo "--------------------------------------"

WORKSPACE_ROOT="$(pwd)"
OUTPUT_DIR="${COPILOT_OUTPUT_DIR:-$WORKSPACE_ROOT/.copilot-hooks}"

if [ -d "$OUTPUT_DIR" ]; then
  echo "✓ 出力ディレクトリが存在します: $OUTPUT_DIR"
  
  # ファイル数をカウント
  file_count=$(find "$OUTPUT_DIR" -name "*.json" -type f 2>/dev/null | wc -l)
  echo "  保存されているファイル: $file_count 件"
  
  if [ $file_count -gt 0 ]; then
    echo "  最新: $(ls -1t "$OUTPUT_DIR"/*/*.json 2>/dev/null | head -n 1 | xargs basename)"
  fi
else
  echo "ℹ️  出力ディレクトリはまだ作成されていません: $OUTPUT_DIR"
  echo "  フックが初めて実行されると自動的に作成されます"
fi

echo ""

# 5. テスト実行
echo "✓ フックスクリプトのテスト実行"
echo "--------------------------------------"

TEST_INPUT='{"timestamp":"2026-02-18T10:00:00.000Z","cwd":"'$WORKSPACE_ROOT'","sessionId":"diagnostic-test","hookEventName":"SessionStart","transcript_path":"/tmp/test.json","source":"new"}'

echo "テストデータを送信中..."
if echo "$TEST_INPUT" | ./.github/hooks/send-to-blob.sh > /tmp/hook-test-output.json 2>&1; then
  echo "✓ フックスクリプトが正常に実行されました"
  
  if [ -f /tmp/hook-test-output.json ]; then
    echo "  出力:"
    cat /tmp/hook-test-output.json | head -5
  fi
else
  echo "❌ フックスクリプトの実行に失敗しました"
  echo "  エラー詳細:"
  cat /tmp/hook-test-output.json 2>&1 | head -10
  exit 1
fi

echo ""

# まとめ
echo "=========================================="
echo "診断結果"
echo "=========================================="
echo ""
echo "✓ フックは正しく設定されています！"
echo ""
echo "次のステップ:"
echo "1. VS Code を再起動"
echo "2. チャットビューで右クリック → Diagnostics"
echo "3. 'hooks' セクションでフックが認識されていることを確認"
echo "4. Output パネル → 'GitHub Copilot Chat Hooks' でログを確認"
echo "5. Copilot とチャットしてフックが実行されるか確認"
echo ""
echo "詳細: .github/hooks/README.md"
echo ""
