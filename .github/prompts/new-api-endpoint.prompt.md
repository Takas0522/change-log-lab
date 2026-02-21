---
description: 新しいマイクロサービスのAPIエンドポイントを設計・実装するためのプロンプト
mode: agent
tools:
  - read
  - edit
  - search
  - execute
  - todo
---

# 新規APIエンドポイント実装

以下の情報をもとに、マイクロサービスに新しいAPIエンドポイントを実装してください。

## 実装依頼

対象サービス: ${{service_name}}
エンドポイント概要: ${{endpoint_description}}

## 実装チェックリスト

以下の順序で実装を進めてください：

1. **既存コードの調査**
   - `src/${service_name}/api/Controllers/` の既存パターンを確認
   - `src/${service_name}/api/Models/` と `DTOs/` の既存データ構造を確認
   - `src/${service_name}/api/Data/` の DbContext を確認

2. **実装**（以下の順序で）
   - [ ] Model（エンティティ）の追加/更新
   - [ ] DbContext の更新（`DbSet<T>` 追加、`OnModelCreating` マッピング）
   - [ ] DTO（リクエスト/レスポンス用のレコード型）の作成
   - [ ] Service（ビジネスロジック）のインターフェースと実装
   - [ ] Controller のエンドポイント実装
   - [ ] `Program.cs` への DI 登録
   - [ ] DB スキーマの更新（`db/schema.sql`）

3. **ビルド確認**
   ```bash
   cd src/${service_name}/api && dotnet build
   ```

## コーディング規約

- DTO はレコード型（`public record XxxRequest(...)`）で定義
- 主キーは `Guid` 型、日時は `DateTimeOffset` 型
- 読み取り系クエリは `AsNoTracking()` を使用
- 認証が必要なエンドポイントには `[Authorize]` を付与
- ユーザーIDは `ClaimTypes.NameIdentifier` から取得
- 構造化ログは `ILogger` のプレースホルダー形式を使用
