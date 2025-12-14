# INIT-004 共有契約（DTO/Event schema）パッケージ

## 目的
サービス間・フロント間で共有する契約（DTO、イベントペイロード、エラー形式）を一箇所に集約し、破壊的変更を防ぐ。

## 依存関係
- Depends on: INIT-001
- Blocks: INIT-005〜INIT-012

## 作業スコープ
- `event_id` を含むイベント基本形（重複排除キー）
- SignalR配信イベント（例：`ListChanged`）の最小ペイロード
  - 例：`{ eventId, listId, kind }`（kindは Created/Updated/Deleted でも単一でもOK。今回は単一チャネルなので kind は任意）
- APIエラーの標準レスポンス形式（問題詳細: ProblemDetails等）

## 受け入れ条件（Acceptance Criteria）
- 共有契約を参照するプロジェクト配置（`src/packages/contracts/` 等）が確定している
- DTOとイベントのバージョニング方針（後方互換優先、破壊的変更時の手順）が短く明文化されている
