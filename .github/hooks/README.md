# VS Code Agent Hooks

このディレクトリには、VS Code Agent Hooks の設定ファイルが配置されています。

## ファイル

- `hooks.json` - フック定義（VS Codeが自動的に読み込む）
- `send-to-blob.sh` - フックスクリプト

**重要**: 
- `hooks.json`のコマンドパスは、ワークスペースルートからの相対パス（`.github/hooks/send-to-blob.sh`）で指定されています
- VS Codeがフックを実行する際のカレントディレクトリはワークスペースルートです
- 開発・テスト用のファイルは `.github/copilot/hooks/` ディレクトリを参照

## 動作確認

### 1. フックが認識されているか確認

VS Codeで：
1. チャットビューで右クリック → **Diagnostics**
2. "hooks" セクションを確認
3. 8つのフック（SessionStart, UserPromptSubmit, PreToolUse, PostToolUse, PreCompact, SubagentStart, SubagentStop, Stop）が表示されていることを確認

### 2. フックの出力確認

VS Codeで：
1. **Output** パネルを開く
2. チャンネルリストから **"GitHub Copilot Chat Hooks"** を選択
3. フックの実行ログが表示される

### 3. データ確認

フックが実行されると、ワークスペースルートの `.copilot-hooks/` ディレクトリにJSONファイルが保存されます：

```bash
# ディレクトリ一覧
ls -ldt .copilot-hooks/*/

# 最新のフックデータを表示
cat $(find .copilot-hooks -name "*.json" -type f | sort -r | head -n 1) | jq .
```

## トラブルシューティング

### フックが認識されない

1. **VS Codeを再起動**
2. `/hooks` コマンドを実行してフック一覧を確認
3. Diagnostics でエラーメッセージを確認

### フックが実行されない

1. Output パネルで "GitHub Copilot Chat Hooks" チャンネルを確認
2. スクリプトに実行権限があるか確認: `ls -l .github/hooks/send-to-blob.sh`
3. スクリプトを手動実行してテスト:
   ```bash
   echo '{"timestamp":"2026-02-18T10:00:00.000Z","cwd":"/workspace","sessionId":"test","hookEventName":"SessionStart","transcript_path":"/tmp/test.json","source":"new"}' | .github/hooks/send-to-blob.sh
   ```

### 組織ポリシーで無効化されている

一部の組織では、セキュリティポリシーによりフックが無効化されている場合があります。管理者に確認してください。

参照: [VS Code Enterprise Policies](https://code.visualstudio.com/docs/enterprise/policies)

## 開発・テスト

開発用のファイル（テストスクリプト、テストデータ、詳細なドキュメントなど）は `.github/copilot/hooks/` ディレクトリを参照してください。

## 参考資料

- [VS Code Agent Hooks 公式ドキュメント](https://code.visualstudio.com/docs/copilot/customization/hooks)
- [詳細ドキュメント](../copilot/hooks/README.md)
