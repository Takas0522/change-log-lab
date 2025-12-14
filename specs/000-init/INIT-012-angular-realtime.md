# INIT-012 Angular Web（SignalR購読 + リアルタイム反映）

## 目的
別デバイス/別ブラウザで行われたList/Todo更新をリアルタイムに反映する。

## 依存関係
- Depends on: INIT-008, INIT-010, INIT-011
- Blocks: INIT-013

## 作業スコープ
- SignalR接続
  - JWTで接続
  - 再接続時の挙動（単純でOK）
- 購読
  - 現在表示中list（またはアクセス可能list）に対する更新イベントを受信
- 反映
  - 受信イベントで対象listのデータを再取得し、画面を更新（最小実装）
  - event_idでの重複排除は可能なら実装（最小は「再取得で整合」でも可）

## 受け入れ条件（Acceptance Criteria）
- ブラウザAでTodo更新→ブラウザBのUIが自動更新される
- NOTIFY欠落を想定し、最終的に整合する（再取得で追従できる）
