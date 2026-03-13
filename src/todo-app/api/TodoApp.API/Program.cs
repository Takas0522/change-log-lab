using Microsoft.EntityFrameworkCore;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Repositories;
using TodoApp.Infrastructure.Repositories.Interfaces;
using TodoApp.Application.Services;
using TodoApp.Application.Services.Interfaces;
using TodoApp.API.Middleware;
using AspNetCoreRateLimit;
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
// REQ-SEC-001対応: CORS設定（環境別）
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}
else
{
    // 本番環境では具体的なドメインを指定
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            var allowedOrigins = builder.Configuration["AllowedOrigins"] ?? "https://yourdomain.com";
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}

// REQ-COMP-002対応: Entity Framework Core
builder.Services.AddDbContext<TodoDbContext>(options =>
{
    // 開発環境ではInMemoryDatabase、本番ではSQL Server
    if (builder.Environment.IsDevelopment())
    {
        options.UseInMemoryDatabase("TodoAppDb");
    }
    else
    {
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure());
    }
});

// REQ-SEC-004対応: レート制限設定
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Application層サービス登録
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddScoped<ILabelService, LabelService>();

// Infrastructure層リポジトリ登録
builder.Services.AddScoped<ITodoRepository, TodoRepository>();
builder.Services.AddScoped<ILabelRepository, LabelRepository>();

// FluentValidation登録
builder.Services.AddValidatorsFromAssemblyContaining<TodoApp.Application.Validators.CreateTodoRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // PascalCase維持
    });

// REQ-MAIN-003対応: Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Todo App API",
        Version = "v1",
        Description = "高機能ToDoアプリケーション RESTful API",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Todo App Team"
        }
    });
    
    // XML コメントの有効化
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline
// グローバル例外ハンドラー（最初に登録）
app.UseMiddleware<GlobalExceptionHandler>();

// REQ-SEC-006対応: セキュリティヘッダー
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo App API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// REQ-SEC-001対応: CORS有効化
app.UseCors("AllowFrontend");

// REQ-SEC-004対応: レート制限有効化
app.UseIpRateLimiting();

app.UseAuthorization();

app.MapControllers();

app.Run();
