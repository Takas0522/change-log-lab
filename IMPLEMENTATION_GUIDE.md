# 高機能ToDoアプリの実装 - セットアップとテスト手順

## 実装内容

このPRでは、以下の機能を実装しました：

### 1. ステータス管理機能
ToDoアイテムに以下の4つのステータスを設定できるようになりました：
- **未着手** (not_started)
- **着手中** (in_progress)
- **完了** (completed)
- **放棄** (abandoned)

### 2. ラベル機能
- ラベルの作成・編集・削除が可能
- 各ラベルに名前とカラー（HEXコード）を設定
- ToDoに複数のラベルを付与可能
- ラベル専用の管理画面を実装

### 3. フィルタリング機能（API実装済み）
以下の条件でToDoをフィルタリング可能：
- ステータス
- ラベル
- 期日（From/To）
- 検索キーワード（タイトル・内容）

## セットアップ手順

### 1. データベースのセットアップ

```bash
# Todo Service Database
cd src/todo-service/db

# データベースを再作成（既存のデータは削除されます）
PGPASSWORD=postgres psql -h localhost -U postgres -c "DROP DATABASE IF EXISTS todo_db;"
PGPASSWORD=postgres psql -h localhost -U postgres -c "CREATE DATABASE todo_db;"

# スキーマとシードデータを投入
PGPASSWORD=postgres psql -h localhost -U postgres -d todo_db -f schema.sql
PGPASSWORD=postgres psql -h localhost -U postgres -d todo_db -f seed.sql
```

### 2. バックエンドの起動

```bash
# Todo Service (Port 5003)
cd src/todo-service/api
dotnet restore
dotnet run

# 他のサービスも必要に応じて起動
# Auth Service (Port 5001)
cd src/auth-service/api && dotnet restore && dotnet run

# User Service (Port 5002)
cd src/user-service/api && dotnet restore && dotnet run

# BFF Service (Port 5000)
cd src/bff-service/api && dotnet restore && dotnet run
```

### 3. フロントエンドの起動

```bash
cd src/web
npm ci
npm start
# http://localhost:4200 でアクセス
```

## テストデータ

シードデータで以下のラベルとステータスが設定されています：

### Admin Tasks リスト (11111111-1111-1111-1111-111111111111)
**ラベル:**
- Urgent (#FF5733) - 赤
- Security (#C70039) - 濃赤
- Maintenance (#FFC300) - 黄色

**ToDo:**
- "Review system logs" - 未着手、Urgentラベル
- "Update security policies" - 着手中、Security + Urgentラベル
- "Database backup check" - 完了、Maintenanceラベル

### Team Projects リスト (22222222-2222-2222-2222-222222222222)
**ラベル:**
- Frontend (#3498DB) - 青
- Backend (#2ECC71) - 緑
- DevOps (#9B59B6) - 紫
- Bug (#E74C3C) - 赤

**ToDo:**
- "Sprint planning meeting" - 未着手
- "Code review" - 着手中、Frontendラベル
- "Deploy to staging" - 完了、DevOpsラベル

### Personal Tasks リスト (44444444-4444-4444-4444-444444444444)
**ラベル:**
- Health (#1ABC9C) - ターコイズ
- Learning (#9B59B6) - 紫
- Hobby (#F39C12) - オレンジ

**ToDo:**
- "Call dentist" - 未着手
- "Read book" - 着手中
- "Morning exercise" - 完了、Healthラベル

## 機能テスト手順

### 1. ラベル管理のテスト

1. ログイン: `admin@example.com` / `password123`
2. "Admin Tasks" リストを開く
3. "Manage Labels" ボタンをクリック
4. 既存のラベル（Urgent、Security、Maintenance）が表示されることを確認
5. "New Label" ボタンをクリック
6. 新しいラベルを作成:
   - 名前: "Important"
   - 色: カラーパレットから選択または #FF6B6B を入力
   - "Create" ボタンをクリック
7. 作成されたラベルが一覧に表示されることを確認
8. ラベルの "Edit" ボタンをクリックして編集可能なことを確認
9. ラベルの "Delete" ボタンをクリックして削除可能なことを確認

### 2. ステータスとラベルの表示テスト

1. リスト詳細画面に戻る
2. 各ToDoにステータスとラベルが表示されることを確認:
   - ステータスはバッジで表示（未着手=グレー、着手中=青、完了=緑、放棄=赤）
   - ラベルはカラーバッジで表示

### 3. API直接テスト

#### ラベル一覧取得
```bash
# JWT トークンを取得（ログイン）
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"password123","deviceId":"test-device"}'

# レスポンスからtokenを取得し、以下で使用
export TOKEN="<取得したJWTトークン>"

# ラベル一覧を取得
curl -X GET http://localhost:5003/api/lists/11111111-1111-1111-1111-111111111111/labels \
  -H "Authorization: Bearer $TOKEN"
```

#### ラベル作成
```bash
curl -X POST http://localhost:5003/api/lists/11111111-1111-1111-1111-111111111111/labels \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Label","color":"#FF6B6B"}'
```

#### ToDoにラベルを割り当て
```bash
# まずToDoの一覧を取得してIDを確認
curl -X GET http://localhost:5003/api/lists/11111111-1111-1111-1111-111111111111/todos \
  -H "Authorization: Bearer $TOKEN"

# ラベルを割り当て
curl -X POST http://localhost:5003/api/lists/11111111-1111-1111-1111-111111111111/todos/<TODO_ID>/labels \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"labelId":"<LABEL_ID>"}'
```

#### フィルタリング
```bash
# ステータスでフィルタ
curl -X GET "http://localhost:5003/api/lists/11111111-1111-1111-1111-111111111111/todos?status=in_progress" \
  -H "Authorization: Bearer $TOKEN"

# ラベルでフィルタ
curl -X GET "http://localhost:5003/api/lists/11111111-1111-1111-1111-111111111111/todos?labelId=<LABEL_ID>" \
  -H "Authorization: Bearer $TOKEN"

# 検索キーワードでフィルタ
curl -X GET "http://localhost:5003/api/lists/11111111-1111-1111-1111-111111111111/todos?search=review" \
  -H "Authorization: Bearer $TOKEN"
```

## データベース確認

```bash
# ラベルの確認
PGPASSWORD=postgres psql -h localhost -U postgres -d todo_db -c "SELECT * FROM labels;"

# ToDo-ラベル関連の確認
PGPASSWORD=postgres psql -h localhost -U postgres -d todo_db -c "SELECT * FROM todo_labels;"

# ToDoのステータス確認
PGPASSWORD=postgres psql -h localhost -U postgres -d todo_db -c "SELECT id, title, status FROM todos;"
```

## 実装の詳細

### バックエンド

#### データベーススキーマ
- `todos.status`: VARCHAR(20) - ステータスフィールド追加
- `labels`: 新テーブル（id, list_id, name, color, created_at, updated_at）
- `todo_labels`: 新テーブル（id, todo_id, label_id, created_at）

#### APIエンドポイント

**ラベル管理:**
- `GET /api/lists/{listId}/labels` - ラベル一覧取得
- `POST /api/lists/{listId}/labels` - ラベル作成
- `GET /api/lists/{listId}/labels/{labelId}` - ラベル詳細取得
- `PUT /api/lists/{listId}/labels/{labelId}` - ラベル更新
- `DELETE /api/lists/{listId}/labels/{labelId}` - ラベル削除

**ラベル割り当て:**
- `POST /api/lists/{listId}/todos/{todoId}/labels` - ToDoにラベル割り当て
- `DELETE /api/lists/{listId}/todos/{todoId}/labels/{labelId}` - ToDoからラベル削除

**フィルタリング:**
- `GET /api/lists/{listId}/todos?status={status}` - ステータスフィルタ
- `GET /api/lists/{listId}/todos?labelId={labelId}` - ラベルフィルタ
- `GET /api/lists/{listId}/todos?search={keyword}` - キーワード検索
- `GET /api/lists/{listId}/todos?dueDateFrom={date}&dueDateTo={date}` - 期日フィルタ

### フロントエンド

#### 新規コンポーネント
- `LabelManagerComponent`: ラベル管理画面

#### 更新されたコンポーネント
- `ListDetailComponent`: ラベル・ステータス表示、ラベル管理画面へのナビゲーション

#### 新規サービス
- `LabelService`: ラベルCRUD操作

#### 更新されたサービス
- `TodoService`: フィルタリングパラメータ対応

## トラブルシューティング

### データベース接続エラー
```bash
# PostgreSQLの起動確認
sudo service postgresql status

# PostgreSQLの起動
sudo service postgresql start
```

### ビルドエラー
```bash
# バックエンド
cd src/todo-service/api
dotnet clean
dotnet restore
dotnet build

# フロントエンド
cd src/web
rm -rf node_modules
npm ci
```

## 今後の拡張案

以下の機能は基盤が実装されているため、容易に追加可能：

1. **フィルタリングUI**: フロントエンドにフィルタバーを追加
2. **ToDo編集時のステータス・ラベル変更**: 編集フォームにドロップダウンを追加
3. **一括操作**: 複数のToDoに一括でラベルを付与
4. **ラベル統計**: ラベル別のToDo完了率などの可視化
5. **ラベルのソート**: ドラッグ&ドロップでラベルの順序を変更
