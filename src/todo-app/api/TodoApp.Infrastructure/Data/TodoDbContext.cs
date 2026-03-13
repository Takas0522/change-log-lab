using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Data;

/// <summary>
/// TodoアプリケーションのDbContext
/// REQ-SEC-001対応: パラメータ化クエリ、SQLインジェクション対策
/// </summary>
public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options)
        : base(options)
    {
    }
    
    /// <summary>
    /// Todosテーブル
    /// </summary>
    public DbSet<Todo> Todos => Set<Todo>();
    
    /// <summary>
    /// Labelsテーブル
    /// </summary>
    public DbSet<Label> Labels => Set<Label>();
    
    /// <summary>
    /// TodoLabels中間テーブル
    /// </summary>
    public DbSet<TodoLabel> TodoLabels => Set<TodoLabel>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Todoエンティティ設定
        modelBuilder.Entity<Todo>(entity =>
        {
            entity.HasKey(e => e.TodoId);
            
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Content)
                .HasMaxLength(5000);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("NotStarted");
            
            entity.Property(e => e.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(e => e.RowVersion)
                .IsRowVersion();
            
            // インデックス設定（REQ-PERF-006対応）
            entity.HasIndex(e => new { e.Status, e.CreatedAt })
                .HasDatabaseName("IX_Todos_Status_CreatedAt");
            
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_Todos_CreatedAt_Desc")
                .IsDescending();
            
            // グローバルクエリフィルター（論理削除されたデータを除外）
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
        
        // Labelエンティティ設定
        modelBuilder.Entity<Label>(entity =>
        {
            entity.HasKey(e => e.LabelId);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Color)
                .IsRequired()
                .HasMaxLength(7);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            entity.Property(e => e.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);
            
            // ユニーク制約（REQ-FUNC-008対応）
            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("UX_Labels_Name");
            
            // グローバルクエリフィルター
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
        
        // TodoLabel中間テーブル設定
        modelBuilder.Entity<TodoLabel>(entity =>
        {
            entity.HasKey(e => new { e.TodoId, e.LabelId });
            
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            // リレーション設定（CASCADE DELETE）
            entity.HasOne(e => e.Todo)
                .WithMany(t => t.TodoLabels)
                .HasForeignKey(e => e.TodoId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Label)
                .WithMany(l => l.TodoLabels)
                .HasForeignKey(e => e.LabelId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // インデックス
            entity.HasIndex(e => e.LabelId)
                .HasDatabaseName("IX_TodoLabels_LabelId");
        });
    }
    
    /// <summary>
    /// SaveChanges時にタイムスタンプを自動更新
    /// REQ-DATA-001対応
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }
    
    /// <summary>
    /// SaveChangesAsync時にタイムスタンプを自動更新
    /// REQ-DATA-001対応
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }
    
    /// <summary>
    /// エンティティのタイムスタンプを更新
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Todo && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));
        
        foreach (var entry in entries)
        {
            var entity = (Todo)entry.Entity;
            
            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
                entry.Property(nameof(Todo.CreatedAt)).IsModified = false;
            }
        }
    }
}
