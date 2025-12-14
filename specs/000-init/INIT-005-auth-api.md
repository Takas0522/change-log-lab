# INIT-005 Auth API（ユーザー登録/ログイン/JWT/端末別ログアウト）

## 目的
自前ユーザーDBで認証を提供し、JWT（10分）発行と端末別ログアウトを実現する。

## 依存関係
- Depends on: INIT-003, INIT-004
- Blocks: INIT-006, INIT-007, INIT-008, INIT-011, INIT-012

## 仕様（確定事項）
- JWT有効期限: 10分
- Refresh Token: なし
- device_id: WebのlocalStorageに保存
- ログアウト: 当該 device_id のみ失効

## 推奨方式（実装指針）
- 端末セッションテーブルに `session_version` を保持
- JWTに `device_id` と `sv`（session_version）を含める
- 認可ミドルウェアで `sv` をDB照合し一致しなければ401
- ログアウトで該当deviceの `session_version++`

## エンドポイント（最小）
- `POST /auth/register`
- `POST /auth/login`
- `POST /auth/logout`（現在のdeviceのみ）
- `GET /me`

## 受け入れ条件（Acceptance Criteria）
- 登録→ログインでJWTが取得できる
- `logout` 後、同じJWT（exp内）でもAPIが401になる（DB照合で拒否）
- パスワードは安全なハッシュ（例: PBKDF2/BCrypt/Argon2）で保存

## 非スコープ
- Refresh Token
- 外部IdP
