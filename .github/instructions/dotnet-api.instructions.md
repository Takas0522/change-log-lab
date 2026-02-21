---
applyTo: "src/**/api/**/*.cs"
---

# .NET API 開発ガイドライン（ASP.NET Core / .NET 10）

このプロジェクトはマイクロサービスアーキテクチャを採用しています。以下の規約に従って開発を行ってください。

## アーキテクチャ概要

```
Angular SPA (port 4200)
    │
    ▼
BFF Service (port 5000) ── プロキシ/集約レイヤー
    ├──► Auth Service (port 5001) ── auth-db
    ├──► User Service (port 5002) ── user-db
    ├──► Todo Service (port 5003) ── todo-db
    └──► Realtime Service (port 5004, SignalR)
```

## プロジェクト構成

各サービスは以下のディレクトリ構造に従います：

```
src/{service-name}/
├── api/
│   ├── Program.cs              # エントリポイント + DI設定
│   ├── api.csproj              # NuGetパッケージ参照
│   ├── Controllers/            # APIコントローラー
│   ├── Models/                 # EF Core エンティティモデル
│   ├── DTOs/                   # リクエスト/レスポンス用レコード型
│   ├── Services/               # ビジネスロジック
│   ├── Data/                   # DbContext
│   └── Middleware/             # カスタムミドルウェア
└── db/
    ├── schema.sql              # DDL
    └── seed.sql                # 開発用シードデータ
```

## C# コーディング規約

### 基本方針
- **Minimal Hosting モデル**: `Program.cs` にトップレベルステートメントで記述（`Startup.cs` は使用しない）
- **Nullable参照型**: `.csproj` で `#nullable enable` を有効化
- **レコード型**: DTO はイミュータブルなレコード型で定義する
- **`required` キーワード**: 非Nullable プロパティには `required` を付与

### DI とライフタイム
```csharp
// ✅ 正しい DI 登録
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ❌ Service Locator パターンは禁止
var service = serviceProvider.GetService<IAuthService>(); // 使用禁止
```

### DTOの定義
```csharp
// ✅ レコード型で定義
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, DateTime ExpiresAt);

// ❌ クラスで定義しない
public class LoginRequest { ... } // 非推奨
```

### Entity Framework Core
- 読み取り系クエリでは **`AsNoTracking()`** を使用
- DB カラム名は **snake_case** で `HasColumnName` を使用してマッピング
- 主キーは **UUID (`Guid`)** を使用
- 日時カラムは **`TIMESTAMP WITH TIME ZONE`** / `DateTimeOffset` を使用
- N+1 問題を防ぐため `Include()` / `ThenInclude()` を適切に使用

### 認証・認可
- **JWT Bearer 認証**: 自己発行JWT（有効期限10分）
- ユーザー識別は `ClaimTypes.NameIdentifier` を使用
- デバイス単位のセッション管理（`session_version` による即時無効化）

### エラーハンドリング
- グローバルエラーハンドリングを使用（各所での Try/Catch は原則禁止）
- 空の `catch` ブロックは禁止
- 構造化ログには `ILogger` のプレースホルダー形式を使用

```csharp
// ✅ 構造化ログ
_logger.LogInformation("User {UserId} logged in from device {DeviceId}", userId, deviceId);

// ❌ 文字列連結
_logger.LogInformation("User " + userId + " logged in"); // 禁止
```

### 非同期処理
- `async/await` を一貫して使用
- `.Result` / `.Wait()` の使用は禁止
- `CancellationToken` を可能な限り伝播

### Outbox パターン（todo-service）
イベント駆動アーキテクチャとして Outbox パターンを採用：
1. `outbox_events` テーブルにドメインイベントを書き込み
2. PostgreSQL トリガーが `pg_notify` を発火
3. Functions ワーカーがリッスンして SignalR へプッシュ

## データベース規約（PostgreSQL）

- 常にパラメータ化クエリを使用（EF Core 経由）
- 外部キーにはインデックスを作成
- 複合出一性制約を適切に使用
- 親子関係には `ON DELETE CASCADE` を使用
- `JSONB` 型を使用してイベントペイロードを格納
