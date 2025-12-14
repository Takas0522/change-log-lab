# INIT-002 DevContainer + docker-compose（postgres/azurite）基盤

## 目的
開発者がWindows環境でも、DevContainer上で全サービスをローカル動作確認できる基盤を用意する。

## 依存関係
- Depends on: INIT-001
- Blocks: INIT-003, INIT-005〜INIT-013

## 方針
- Docker Desktop（ホスト）を使う DoOD（Docker-outside-of-Docker）
- docker-composeで最低限以下を起動
  - postgres
  - azurite（Functionsのストレージ要件）
  - （将来）各サービス（api/realtime/functions/web）もcomposeに統合可能な形にする

## 作業スコープ
- `.devcontainer/devcontainer.json` 作成
- `.devcontainer/docker-compose.yml` 作成（postgres + azurite）
- `.env.example`（接続文字列、JWT署名鍵など）

## 受け入れ条件（Acceptance Criteria）
- DevContainerを起動すると postgres と azurite が立ち上がる
- DevContainer内から `psql` で postgres に接続できる
- DevContainer内から Functions（Core Tools）を起動できる前提ツールが揃っている

## 注意点
- Windowsパス問題を避けるため、composeのデータ永続化は named volume を基本とする
