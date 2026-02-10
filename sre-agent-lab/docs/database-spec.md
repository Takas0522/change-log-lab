# データベース仕様書

## 概要

PostgreSQLを使用し、単一データベースで認証情報とToDoデータを管理する。

- **データベース名:** `sre_agent_lab_db`
- **文字エンコーディング:** UTF-8
- **タイムゾーン:** UTC

---

## テーブル定義

### users テーブル

ユーザー認証情報を管理する。

| カラム名 | 型 | 制約 | 説明 |
|---|---|---|---|
| id | UUID | PRIMARY KEY, DEFAULT gen_random_uuid() | ユーザーID |
| email | VARCHAR(255) | NOT NULL, UNIQUE | メールアドレス |
| password_hash | VARCHAR(255) | NOT NULL | BCryptハッシュ化パスワード |
| display_name | VARCHAR(100) | | 表示名 |
| created_at | TIMESTAMP WITH TIME ZONE | NOT NULL, DEFAULT NOW() | 作成日時 |
| updated_at | TIMESTAMP WITH TIME ZONE | NOT NULL, DEFAULT NOW() | 更新日時 |

**インデックス:**
- `idx_users_email` ON `users(email)` - ログイン時のメールアドレス検索用

### todos テーブル

ToDoデータを管理する。

| カラム名 | 型 | 制約 | 説明 |
|---|---|---|---|
| id | UUID | PRIMARY KEY, DEFAULT gen_random_uuid() | ToDo ID |
| user_id | UUID | NOT NULL, REFERENCES users(id) ON DELETE CASCADE | 所有者ユーザーID |
| title | VARCHAR(255) | NOT NULL | タイトル |
| body | TEXT | | 本文 |
| status | VARCHAR(20) | NOT NULL, DEFAULT '未着手', CHECK | ステータス |
| created_at | TIMESTAMP WITH TIME ZONE | NOT NULL, DEFAULT NOW() | 作成日時 |
| due_date | TIMESTAMP WITH TIME ZONE | | 完了予定日時 |
| completed_at | TIMESTAMP WITH TIME ZONE | | 完了日時 |
| updated_at | TIMESTAMP WITH TIME ZONE | NOT NULL, DEFAULT NOW() | 更新日時 |

**CHECK制約:**
- `status IN ('未着手', '着手中', '完了')`

**インデックス:**
- `idx_todos_user` ON `todos(user_id)` - ユーザー別ToDo取得用
- `idx_todos_user_status` ON `todos(user_id, status)` - ステータスフィルタリング用

---

## ステータス遷移ルール

| ステータス | 値 | completedAt の挙動 |
|---|---|---|
| 未着手 | `未着手` | `NULL` に設定 |
| 着手中 | `着手中` | `NULL` に設定 |
| 完了 | `完了` | 現在日時を自動設定 |

- ステータスが `完了` に遷移した時点で `completed_at` に現在日時が設定される
- `完了` から他のステータスに戻った場合、`completed_at` は `NULL` にリセットされる

---

## シードデータ

開発環境およびデモ用のテストデータ。

### ユーザー

| email | password | display_name |
|---|---|---|
| admin@example.com | password123 | Admin User |
| demo@example.com | password123 | Demo User |

### ToDo (admin@example.com)

| title | status | due_date |
|---|---|---|
| サーバー監視設定 | 未着手 | 7日後 |
| ドキュメント更新 | 着手中 | 3日後 |
| CI/CDパイプライン構築 | 完了 | 1日前 |

### ToDo (demo@example.com)

| title | status | due_date |
|---|---|---|
| デモ環境準備 | 未着手 | 5日後 |
| プレゼン資料作成 | 着手中 | 2日後 |

---

## セキュリティ考慮事項

- パスワードはBCrypt (cost factor 11) でハッシュ化して保存
- `user_id` による行レベルアクセス制御（APIレイヤーで実装）
- `ON DELETE CASCADE` によりユーザー削除時にToDoも自動削除
- 本番環境ではDBユーザーの権限を最小限に設定すること
