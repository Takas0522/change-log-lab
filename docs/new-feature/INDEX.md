# ユーザー間リスト共有機能 — 設計文書 INDEX

## 概要

本ディレクトリには、ToDo アプリケーションの**ユーザー間リスト共有機能**に関する設計文書が格納されている。

現行システムでは owner がメンバーを直接追加する方式であるが、本機能により招待ワークフロー（招待送信 → 受諾 / 辞退）を導入し、ロール付き（editor / viewer）の安全な共有体験を実現する。

## 文書一覧

| 文書 | 文書番号 | 説明 | ステータス |
|------|---------|------|-----------|
| [architecture.md](architecture.md) | FEAT-SHARE-ARCH-001 | アーキテクチャ設計 — サービス間連携、API 設計、権限モデル、イベント駆動統合、フロントエンド変更点 | Draft |
| [data-model.md](data-model.md) | FEAT-SHARE-DATA-001 | データモデル設計 — ER 図、テーブル定義、マイグレーション SQL、Model / DTO / DbContext 定義 | Draft |

## 関連する既存文書

| 文書 | 説明 |
|------|------|
| [specs/000-init/INIT-007-sharing-invites.md](../../specs/000-init/INIT-007-sharing-invites.md) | 初期実装仕様（viewer 招待 API の最小定義） |
| [specs/000-init/00-issue-map.md](../../specs/000-init/00-issue-map.md) | 全体アーキテクチャと Issue 一覧 |
| [src/todo-service/db/schema.sql](../../src/todo-service/db/schema.sql) | 現行の todo-db スキーマ |
