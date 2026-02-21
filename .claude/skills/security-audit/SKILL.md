---
name: security-audit
description: OWASP Top 10 / CWE 準拠のセキュリティ監査スキル。ASP.NET Core API・Angular SPA・PostgreSQL・Docker 構成のマイクロサービスを対象に、脆弱性の自動検出・重大度評価・修正コード提案・監査レポート生成を行う。コードレビュー、新規実装の安全性確認、セキュリティインシデント対応時に使用する。
license: MIT
---

# Security Audit Skill（OWASP / CWE 準拠）

マイクロサービスアーキテクチャ（ASP.NET Core + Angular + PostgreSQL）を対象とした包括的セキュリティ監査スキル。OWASP Top 10 (2021) と CWE（Common Weakness Enumeration）に基づき、脆弱性の検出・分類・修正提案を体系的に行う。

## When to Use This Skill

- **コードレビュー時**: PR / MR のセキュリティ観点レビュー
- **新規機能実装時**: 認証・認可・データ処理の安全性確認
- **脆弱性調査**: 既存コードのセキュリティスキャン
- **インシデント対応**: 報告された脆弱性の根本原因分析と修正
- **セキュリティレポート生成**: 監査結果の構造化ドキュメント作成
- **インフラ設定確認**: Docker / DB / CORS 等のセキュリティ設定検証

---

## 監査レポートフォーマット

### 発見事項テンプレート

各脆弱性は以下のフォーマットで報告する：

```markdown
### [重大度] 脆弱性タイトル

| 項目 | 内容 |
|------|------|
| **重大度** | 🔴 Critical / 🟠 High / 🟡 Medium / 🔵 Low / ⚪ Info |
| **CWE** | CWE-XXX: カテゴリ名 |
| **OWASP** | A0X:2021 カテゴリ名 |
| **対象ファイル** | `path/to/file.cs` L42-58 |
| **CVSS v3.1** | X.X (計算根拠) |
| **検出方法** | 静的解析 / パターンマッチ / 設定確認 |

**脆弱なコード:**
（問題のあるコードスニペット）

**攻撃シナリオ:**
（具体的な攻撃手法の説明）

**修正コード:**
（安全な実装例）

**検証方法:**
（修正後の確認手順）
```

### サマリーテンプレート

```markdown
## セキュリティ監査サマリー

| 重大度 | 件数 | 対応期限 |
|--------|------|----------|
| 🔴 Critical | X 件 | 即時対応 |
| 🟠 High | X 件 | 1週間以内 |
| 🟡 Medium | X 件 | 次スプリント |
| 🔵 Low | X 件 | バックログ |
| ⚪ Info | X 件 | 任意 |

**監査スコア: XX / 100**
```

---

## OWASP Top 10 (2021) 検出ルール

### A01:2021 – Broken Access Control（アクセス制御の不備）

#### 検出パターン — ASP.NET Core

```csharp
// 🔴 [Critical] 認可属性の欠落したコントローラー/アクション
// CWE-862: Missing Authorization
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase  // ← [Authorize] が無い
{
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id) { ... }
}

// ✅ 修正: 適切な認可属性を付与
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id) { ... }
}
```

```csharp
// 🟠 [High] IDOR（安全でない直接オブジェクト参照）
// CWE-639: Authorization Bypass Through User-Controlled Key
[HttpGet("{userId}/profile")]
public async Task<IActionResult> GetProfile(Guid userId)
{
    // ← リクエスト元ユーザーと userId の一致を検証していない
    var profile = await _repo.GetByIdAsync(userId);
    return Ok(profile);
}

// ✅ 修正: 認証済みユーザーIDとの照合
[HttpGet("{userId}/profile")]
[Authorize]
public async Task<IActionResult> GetProfile(Guid userId)
{
    var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (currentUserId != userId.ToString())
        return Forbid();

    var profile = await _repo.GetByIdAsync(userId);
    return Ok(profile);
}
```

#### 検出パターン — Angular

```typescript
// 🟡 [Medium] クライアントサイドのみのルートガード（サーバー側検証必須）
// CWE-602: Client-Side Enforcement of Server-Side Security
const routes: Routes = [
  {
    path: 'admin',
    component: AdminComponent,
    canActivate: [authGuard]  // ← サーバー側でも必ず認可検証すること
  }
];
```

---

### A02:2021 – Cryptographic Failures（暗号化の不備）

#### 検出パターン

```csharp
// 🔴 [Critical] パスワードの平文保存
// CWE-256: Plaintext Storage of a Password
user.Password = request.Password;  // ← ハッシュ化されていない
await _context.SaveChangesAsync();

// ✅ 修正: BCrypt によるハッシュ化
user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
await _context.SaveChangesAsync();
```

```csharp
// 🟠 [High] 弱いハッシュアルゴリズム
// CWE-328: Use of Weak Hash
var hash = MD5.Create().ComputeHash(data);     // ❌ MD5
var hash = SHA1.Create().ComputeHash(data);    // ❌ SHA-1

// ✅ 修正: SHA-256 以上、またはパスワードには BCrypt/Argon2
var hash = SHA256.Create().ComputeHash(data);
```

```csharp
// 🟠 [High] ハードコードされた暗号鍵・シークレット
// CWE-798: Use of Hard-coded Credentials
var key = "MySecretKey123!";  // ❌ ソースコードに直書き
var connectionString = "Host=localhost;Password=admin123";  // ❌

// ✅ 修正: 環境変数 / Secret Manager / Azure Key Vault
var key = builder.Configuration["Jwt:Secret"];
```

---

### A03:2021 – Injection（インジェクション）

#### 検出パターン — SQL インジェクション

```csharp
// 🔴 [Critical] 文字列連結による SQL 構築
// CWE-89: SQL Injection
var sql = $"SELECT * FROM users WHERE name = '{name}'";  // ❌
await _context.Database.ExecuteSqlRawAsync(sql);

// ✅ 修正: パラメータ化クエリ
var sql = "SELECT * FROM users WHERE name = @p0";
await _context.Database.ExecuteSqlRawAsync(sql, name);

// ✅ または EF Core の LINQ を使用
var users = await _context.Users
    .Where(u => u.Name == name)
    .AsNoTracking()
    .ToListAsync();
```

#### 検出パターン — XSS

```typescript
// 🔴 [Critical] 未サニタイズの HTML バインディング
// CWE-79: Cross-site Scripting
// Angular テンプレート
`<div [innerHTML]="userInput"></div>`  // ❌ userInput が未検証

// ✅ 修正: DomSanitizer で制御
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

safeContent: SafeHtml;
constructor(private sanitizer: DomSanitizer) {
  this.safeContent = this.sanitizer.sanitize(SecurityContext.HTML, userInput);
}
```

#### 検出パターン — コマンドインジェクション

```csharp
// 🔴 [Critical] ユーザー入力を含むプロセス実行
// CWE-78: OS Command Injection
Process.Start("cmd", $"/c echo {userInput}");  // ❌

// ✅ 修正: ホワイトリストバリデーション + エスケープ
if (!AllowedCommands.Contains(command))
    throw new InvalidOperationException("Command not allowed");
```

---

### A04:2021 – Insecure Design（安全でない設計）

#### 検出パターン

```csharp
// 🟠 [High] Mass Assignment（一括代入）
// CWE-915: Improperly Controlled Modification of Dynamically-Determined Object Attributes
[HttpPut("{id}")]
public async Task<IActionResult> Update(Guid id, [FromBody] User user)
{
    // ← リクエストボディを直接エンティティにバインド
    _context.Users.Update(user);  // ❌ IsAdmin 等も上書き可能
    await _context.SaveChangesAsync();
}

// ✅ 修正: DTO を使用して許可フィールドのみマッピング
[HttpPut("{id}")]
public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
{
    var user = await _context.Users.FindAsync(id);
    user.Name = request.Name;
    user.Email = request.Email;
    // IsAdmin は意図的にマッピングしない
    await _context.SaveChangesAsync();
}
```

```csharp
// 🟡 [Medium] レート制限の欠如
// CWE-770: Allocation of Resources Without Limits or Throttling
[HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request)
{
    // ← ブルートフォース攻撃に対する制限なし
}

// ✅ 修正: レート制限ミドルウェアを追加
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

[HttpPost("login")]
[EnableRateLimiting("login")]
public async Task<IActionResult> Login(LoginRequest request) { ... }
```

---

### A05:2021 – Security Misconfiguration（セキュリティ設定の不備）

#### 検出パターン

```csharp
// 🟠 [High] CORS の過剰許可
// CWE-346: Origin Validation Error
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()     // ❌ 全オリジン許可
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ✅ 修正: 許可オリジンを明示指定
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://app.example.com")
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .WithHeaders("Authorization", "Content-Type");
    });
});
```

```csharp
// 🟠 [High] 詳細エラー情報のレスポンス公開
// CWE-209: Generation of Error Message Containing Sensitive Information
app.UseDeveloperExceptionPage();  // ❌ 本番環境でスタックトレース公開

// ✅ 修正: 環境別ハンドリング
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler("/error");
```

```yaml
# 🟠 [High] Docker Compose での秘匿情報ハードコード
# CWE-798: Use of Hard-coded Credentials
services:
  db:
    environment:
      POSTGRES_PASSWORD: "admin123"  # ❌

# ✅ 修正: .env ファイル + シークレット管理
services:
  db:
    environment:
      POSTGRES_PASSWORD: ${DB_PASSWORD}  # .env から読み込み
```

---

### A06:2021 – Vulnerable and Outdated Components

#### 検査項目

```bash
# NuGet パッケージの脆弱性チェック
dotnet list package --vulnerable --include-transitive

# npm パッケージの脆弱性チェック
npm audit --production
```

---

### A07:2021 – Identification and Authentication Failures

#### 検出パターン

```csharp
// 🔴 [Critical] JWT の署名検証無効化
// CWE-347: Improper Verification of Cryptographic Signature
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = false,  // ❌ 署名検証無効
    ValidateLifetime = false,          // ❌ 有効期限チェック無効
    ValidateIssuer = false,            // ❌ 発行者チェック無効
};

// ✅ 修正: すべての検証を有効化
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
    ValidateLifetime = true,
    ClockSkew = TimeSpan.FromMinutes(1),
    ValidateIssuer = true,
    ValidIssuer = "https://auth.example.com",
    ValidateAudience = true,
    ValidAudience = "https://api.example.com",
};
```

```csharp
// 🟡 [Medium] セッション固定化
// CWE-384: Session Fixation
// ログイン成功時にセッションIDを再生成していない

// ✅ 修正: ログイン成功時に session_version をインクリメント
device.SessionVersion++;
await _context.SaveChangesAsync();
// → SessionVersionMiddleware が旧セッションを自動無効化
```

---

### A08:2021 – Software and Data Integrity Failures

#### 検出パターン

```csharp
// 🔴 [Critical] 安全でないデシリアライズ
// CWE-502: Deserialization of Untrusted Data
var settings = new JsonSerializerSettings
{
    TypeNameHandling = TypeNameHandling.All  // ❌ 任意の型をインスタンス化可能
};
var obj = JsonConvert.DeserializeObject(input, settings);

// ✅ 修正: TypeNameHandling.None（デフォルト）を使用
var obj = JsonConvert.DeserializeObject<ExpectedType>(input);
// または System.Text.Json を使用（デフォルトで安全）
var obj = JsonSerializer.Deserialize<ExpectedType>(input);
```

---

### A09:2021 – Security Logging and Monitoring Failures

#### 検出パターン

```csharp
// 🟡 [Medium] セキュリティイベントのログ欠如
// CWE-778: Insufficient Logging
[HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request)
{
    var user = await _authService.AuthenticateAsync(request);
    if (user == null)
        return Unauthorized();  // ← ログイン失敗をログに記録していない

    return Ok(new { Token = GenerateToken(user) });
}

// ✅ 修正: セキュリティイベントの構造化ログ
[HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request)
{
    var user = await _authService.AuthenticateAsync(request);
    if (user == null)
    {
        _logger.LogWarning(
            "Login failed for email {Email} from IP {IP}",
            request.Email,
            HttpContext.Connection.RemoteIpAddress);
        return Unauthorized();
    }

    _logger.LogInformation(
        "Login succeeded for user {UserId} from IP {IP}",
        user.Id,
        HttpContext.Connection.RemoteIpAddress);
    return Ok(new { Token = GenerateToken(user) });
}
```

---

### A10:2021 – Server-Side Request Forgery (SSRF)

#### 検出パターン

```csharp
// 🟠 [High] ユーザー入力による URL フェッチ
// CWE-918: Server-Side Request Forgery
[HttpGet("fetch")]
public async Task<IActionResult> Fetch([FromQuery] string url)
{
    var response = await _httpClient.GetAsync(url);  // ❌ 任意の URL にアクセス可能
    return Ok(await response.Content.ReadAsStringAsync());
}

// ✅ 修正: URL ホワイトリスト検証
private static readonly HashSet<string> AllowedHosts = new() { "api.example.com" };

[HttpGet("fetch")]
public async Task<IActionResult> Fetch([FromQuery] string url)
{
    var uri = new Uri(url);
    if (!AllowedHosts.Contains(uri.Host))
        return BadRequest("Disallowed host");

    var response = await _httpClient.GetAsync(uri);
    return Ok(await response.Content.ReadAsStringAsync());
}
```

---

## データベースセキュリティ検査

### PostgreSQL 固有チェック

| # | チェック項目 | CWE | 重大度 | 検出方法 |
|---|-------------|-----|--------|---------|
| D-01 | デフォルト/弱いパスワード | CWE-521 | 🔴 Critical | 設定ファイル確認 |
| D-02 | SUPERUSER 権限のアプリユーザー | CWE-250 | 🟠 High | `schema.sql` 確認 |
| D-03 | SSL/TLS 未使用 | CWE-319 | 🟠 High | 接続文字列確認 |
| D-04 | パスワード平文カラム | CWE-312 | 🔴 Critical | スキーマ確認 |
| D-05 | `pg_hba.conf` の trust 認証 | CWE-287 | 🔴 Critical | 設定確認 |
| D-06 | 外部キー制約の欠如 | CWE-20 | 🟡 Medium | DDL 分析 |

### 接続文字列の安全なパターン

```csharp
// ❌ 危険: ハードコード + SSL 無効
"Host=localhost;Database=authdb;Username=postgres;Password=admin123;SSL Mode=Disable"

// ✅ 安全: 環境変数 + SSL 有効 + 接続プーリング
builder.Configuration.GetConnectionString("DefaultConnection")
// appsettings.json → 環境変数でオーバーライド
// "Host=db;Database=authdb;Username=app_user;Password=${DB_PASSWORD};SSL Mode=Require;Trust Server Certificate=false;Pooling=true;Maximum Pool Size=20"
```

---

## Docker / インフラセキュリティ検査

| # | チェック項目 | CWE | 重大度 | 検出方法 |
|---|-------------|-----|--------|---------|
| I-01 | root ユーザーでのコンテナ実行 | CWE-250 | 🟠 High | Dockerfile 確認 |
| I-02 | 秘匿情報の docker-compose.yml 直書き | CWE-798 | 🟠 High | Compose ファイル確認 |
| I-03 | 不要ポートの外部公開 | CWE-284 | 🟡 Medium | Compose ファイル確認 |
| I-04 | ベースイメージの脆弱性 | CWE-1395 | 🟡 Medium | `FROM` ディレクティブ確認 |
| I-05 | .dockerignore の欠如 | CWE-200 | 🔵 Low | ファイル存在確認 |

### Dockerfile セキュリティチェック

```dockerfile
# ❌ 危険: root 実行 + 不要ファイル含む
FROM mcr.microsoft.com/dotnet/aspnet:10.0
COPY . /app
ENTRYPOINT ["dotnet", "api.dll"]

# ✅ 安全: マルチステージビルド + 非 root ユーザー
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN adduser --disabled-password --gecos "" appuser
USER appuser
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "api.dll"]
```

---

## セキュリティヘッダー検査

### 必須レスポンスヘッダー

```csharp
// ✅ Program.cs にセキュリティヘッダーミドルウェアを追加
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "0");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'");
    context.Response.Headers.Append("Permissions-Policy",
        "camera=(), microphone=(), geolocation=()");
    context.Response.Headers.Remove("Server");
    context.Response.Headers.Remove("X-Powered-By");
    await next();
});
```

| ヘッダー | 推奨値 | 目的 |
|---------|--------|------|
| `X-Content-Type-Options` | `nosniff` | MIME スニッフィング防止 |
| `X-Frame-Options` | `DENY` | クリックジャッキング防止 |
| `Content-Security-Policy` | 厳格なポリシー | XSS / データ注入防止 |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | HTTPS 強制 |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | リファラ情報制御 |
| `Permissions-Policy` | `camera=(), microphone=()` | ブラウザ機能制限 |
| `Server` | 削除 | サーバー情報隠蔽 |

---

## Angular SPA 固有の検査

| # | チェック項目 | CWE | 重大度 |
|---|-------------|-----|--------|
| F-01 | `[innerHTML]` 未サニタイズ使用 | CWE-79 | 🔴 Critical |
| F-02 | API キー / シークレットのハードコード | CWE-200 | 🟠 High |
| F-03 | `bypassSecurityTrust*` の不適切な使用 | CWE-79 | 🟠 High |
| F-04 | HTTP Interceptor での認証トークン管理欠如 | CWE-522 | 🟡 Medium |
| F-05 | `environment.ts` への機密情報格納 | CWE-200 | 🟡 Medium |
| F-06 | `console.log` での機密情報出力 | CWE-532 | 🟡 Medium |
| F-07 | CSP 非対応の `eval()` / `Function()` 使用 | CWE-95 | 🟠 High |

---

## 監査実行手順

### Phase 1: 自動スキャン

以下のパターンをコードベース全体で検索する：

#### C# / .NET 危険パターン

```
検索キーワード（正規表現）:
- ExecuteSqlRaw.*\$"           → SQL インジェクション
- \.Result\b|\.Wait\(\)       → 非同期デッドロック
- AllowAnyOrigin              → CORS 不備
- TypeNameHandling\.All       → 安全でないデシリアライズ
- Password\s*=\s*"[^"]+"     → ハードコード資格情報
- \[HttpPost\](?!.*\[Authorize) → 未認可エンドポイント
- UseDeveloperExceptionPage   → 開発者エラーページ
- MD5\.Create|SHA1\.Create    → 弱いハッシュ
- Process\.Start              → コマンドインジェクション
- ValidateIssuerSigningKey\s*=\s*false → JWT 検証無効
- catch\s*\{\s*\}|catch\s*\(Exception\)\s*\{\s*\} → 空キャッチ
```

#### TypeScript / Angular 危険パターン

```
検索キーワード（正規表現）:
- \[innerHTML\]               → XSS リスク
- bypassSecurityTrust         → セキュリティバイパス
- localStorage\.setItem.*token → トークンの安全でない保存
- console\.(log|debug|info)   → 機密情報のコンソール出力
- eval\(|new Function\(       → コードインジェクション
- http://                     → 非暗号化通信
- apiKey|secret|password      → ハードコード資格情報
```

#### SQL 危険パターン

```
検索キーワード（正規表現）:
- SUPERUSER|CREATEDB          → 過剰権限
- password.*VARCHAR.*plain    → 平文パスワード格納
- sslmode.*=.*disable         → SSL 無効
- GRANT ALL                   → 過剰な権限付与
- trust                       → 信頼認証
```

#### Docker / インフラ危険パターン

```
検索キーワード（正規表現）:
- POSTGRES_PASSWORD.*=.*"     → ハードコードパスワード
- ports:.*5432:5432           → DB ポート外部公開
- FROM.*:latest               → 固定バージョン未使用
- USER root|(?<!USER )ENTRYPOINT → root 実行
```

### Phase 2: 手動検証

1. **認証フロー**: ログイン → トークン発行 → トークン検証 → セッション管理の一連を追跡
2. **認可境界**: 全エンドポイントの `[Authorize]` 属性とロールチェックを検証
3. **データフロー**: ユーザー入力 → バリデーション → DB 保存 → レスポンスの各段階を追跡
4. **エラーハンドリング**: 例外発生時のレスポンス内容を確認（スタックトレース漏洩の有無）
5. **シークレット管理**: 設定ファイル・環境変数・Key Vault の使い分けを確認

### Phase 3: レポート生成

1. 発見事項を重大度順にソート
2. 各項目にサマリーテンプレートのフォーマットを適用
3. 修正コードと検証手順を付加
4. 監査スコアを算出（100点満点）

---

## 監査スコア算出方法

```
基本スコア = 100

減点:
  🔴 Critical × (-15点)
  🟠 High     × (-8点)
  🟡 Medium   × (-3点)
  🔵 Low      × (-1点)

最低スコア = 0
```

| スコア | 評価 | アクション |
|--------|------|-----------|
| 90-100 | ✅ Excellent | リリース可 |
| 70-89  | 🟡 Good | High 以上を修正してリリース |
| 50-69  | 🟠 Needs Work | Critical/High を修正必須 |
| 0-49   | 🔴 Critical | リリース不可・即時対応 |

---

## CVSS v3.1 簡易算出ガイド

重大度判定の根拠として CVSS v3.1 ベーススコアを使用：

| 要素 | 説明 | 値の例 |
|------|------|--------|
| Attack Vector (AV) | ネットワーク(N) / 隣接(A) / ローカル(L) / 物理(P) | SQL Injection → N |
| Attack Complexity (AC) | 低(L) / 高(H) | 認証バイパス → L |
| Privileges Required (PR) | 無(N) / 低(L) / 高(H) | 未認証攻撃 → N |
| User Interaction (UI) | 無(N) / 有(R) | XSS → R, SQLi → N |
| Scope (S) | 変更無(U) / 変更有(C) | DB アクセス → C |
| CIA Impact | 無(N) / 低(L) / 高(H) | データ漏洩 → C:H |

---

## Keywords

security audit, OWASP, CWE, vulnerability, SQL injection, XSS, CSRF, authentication, authorization, ASP.NET Core, Angular, PostgreSQL, Docker, CVSS, penetration testing, セキュリティ監査, 脆弱性診断
