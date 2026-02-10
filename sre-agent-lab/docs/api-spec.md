# API仕様書

## 概要

本アプリケーションはREST APIを提供し、JWT認証によりアクセス制御を行う。
ベースURL: `/api`

---

## 認証エンドポイント

ログインフローに関するエンドポイントはJWT不要。

### POST /api/auth/register

ユーザー登録。

**Request Body:**
```json
{
  "email": "string (required, unique)",
  "password": "string (required, 8文字以上)",
  "displayName": "string (optional, 100文字以内)"
}
```

**Response (200 OK):**
```json
{
  "userId": "uuid",
  "email": "string",
  "displayName": "string | null",
  "message": "登録が完了しました"
}
```

**エラー:**
- `400 Bad Request` - バリデーションエラー、メールアドレス重複

### POST /api/auth/login

ログイン。JWTトークンを発行する。

**Request Body:**
```json
{
  "email": "string (required)",
  "password": "string (required)"
}
```

**Response (200 OK):**
```json
{
  "token": "string (JWT)",
  "userId": "uuid",
  "email": "string",
  "displayName": "string | null"
}
```

**エラー:**
- `401 Unauthorized` - メールアドレスまたはパスワードが不正

### GET /api/auth/me

認証済みユーザー情報を取得する。`[Authorize]`

**Response (200 OK):**
```json
{
  "userId": "uuid",
  "email": "string",
  "displayName": "string | null"
}
```

**エラー:**
- `401 Unauthorized` - JWT無効または期限切れ

---

## ToDoエンドポイント

全エンドポイントに `[Authorize]` が必要。ユーザーは自分のToDoのみにアクセス可能。

### GET /api/todos

ToDo一覧を取得する。クエリパラメータによるフィルタリング対応。

**Query Parameters:**
| パラメータ | 型 | 説明 |
|---|---|---|
| status | string | ステータスで絞り込み (`未着手` / `着手中` / `完了`) |
| title | string | タイトルの部分一致検索 |
| dueDateFrom | datetime | 完了予定日の開始日 |
| dueDateTo | datetime | 完了予定日の終了日 |

**Response (200 OK):**
```json
[
  {
    "id": "uuid",
    "title": "string",
    "body": "string | null",
    "status": "未着手 | 着手中 | 完了",
    "createdAt": "datetime",
    "dueDate": "datetime | null",
    "completedAt": "datetime | null",
    "updatedAt": "datetime"
  }
]
```

### GET /api/todos/{id}

ToDo詳細を取得する。

**Response (200 OK):** TodoResponse (上記と同じ形式)

**エラー:**
- `404 Not Found` - 存在しない
- `403 Forbidden` - 他ユーザーのToDo

### POST /api/todos

ToDoを新規作成する。ステータスは `未着手` で作成される。

**Request Body:**
```json
{
  "title": "string (required, 255文字以内)",
  "body": "string (optional)",
  "dueDate": "datetime (optional)"
}
```

**Response (201 Created):** TodoResponse

**エラー:**
- `400 Bad Request` - バリデーションエラー

### PUT /api/todos/{id}

ToDoを更新する（詳細画面から利用）。

**Request Body:**
```json
{
  "title": "string (optional, 255文字以内)",
  "body": "string (optional)",
  "status": "string (optional, 未着手/着手中/完了)",
  "dueDate": "datetime (optional)"
}
```

**Response (200 OK):** TodoResponse

**エラー:**
- `400 Bad Request` - バリデーションエラー
- `404 Not Found` - 存在しない
- `403 Forbidden` - 他ユーザーのToDo

**備考:** ステータスが `完了` に変更された場合、`completedAt` に現在日時が自動設定される。`完了` 以外に変更された場合、`completedAt` は `null` にリセットされる。

### PATCH /api/todos/{id}/status

ステータスのみを変更する（一覧画面から利用）。

**Request Body:**
```json
{
  "status": "未着手 | 着手中 | 完了"
}
```

**Response (200 OK):** TodoResponse

**エラー:**
- `400 Bad Request` - 不正なステータス値
- `404 Not Found` - 存在しない
- `403 Forbidden` - 他ユーザーのToDo

### DELETE /api/todos/{id}

ToDoを削除する（詳細画面から利用）。

**Response:** `204 No Content`

**エラー:**
- `404 Not Found` - 存在しない
- `403 Forbidden` - 他ユーザーのToDo

---

## 共通仕様

### 認証ヘッダー

```
Authorization: Bearer <JWT Token>
```

### エラーレスポンス形式

```json
{
  "message": "エラーの説明"
}
```

### ステータスコード

| コード | 説明 |
|---|---|
| 200 | 成功 |
| 201 | 作成成功 |
| 204 | 削除成功 (レスポンスボディなし) |
| 400 | リクエスト不正 |
| 401 | 認証エラー |
| 403 | アクセス権限なし |
| 404 | リソースが存在しない |
| 500 | サーバー内部エラー |
