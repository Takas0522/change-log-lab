# INIT-011 Angular Web（ログイン + List/Todo管理 + 招待UI）

## 目的
ユーザーがブラウザからログインし、List/Todoの管理とviewer招待ができるUIを用意する。

## 依存関係
- Depends on: INIT-005, INIT-006, INIT-007
- Blocks: INIT-012, INIT-013

## 仕様（確定事項）
- device_idはlocalStorageに保存
- JWTは10分、refresh無し（期限切れ時は再ログイン）

## 作業スコープ
- 認証
  - 登録/ログイン/ログアウト（この端末のみ）
  - localStorageに `device_id` を生成・保存（初回のみ）
  - JWTの保持（保存先は実装方針に従う）
- 画面
  - List一覧/作成/更新/削除
  - Todo一覧/作成/更新/削除
  - 招待：inviteeユーザー選択（既存ユーザーのみ）→招待作成
  - 招待受諾

## 受け入れ条件（Acceptance Criteria）
- ログインしてList/Todoが操作できる
- viewerとして招待されたユーザーが招待を受諾し、参照のみできる
- viewerで書き込み操作を試みるとUI上で失敗（403）を適切に扱う
