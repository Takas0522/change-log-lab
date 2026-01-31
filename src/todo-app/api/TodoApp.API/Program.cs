using Microsoft.EntityFrameworkCore;
using TodoApp.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
// REQ-SEC-001対応: CORS設定
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

// TODO: Service層とRepository層の登録（後で実装）
// builder.Services.AddScoped<ITodoService, TodoService>();
// builder.Services.AddScoped<ILabelService, LabelService>();

var app = builder.Build();

// Configure the HTTP request pipeline
// REQ-SEC-006対応: セキュリティヘッダー
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
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

app.UseAuthorization();

app.MapControllers();

app.Run();
