using Microsoft.EntityFrameworkCore;
using SreAgentLab.Models;

namespace SreAgentLab.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<TodoItem> Todos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(255);
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.Email).HasDatabaseName("idx_users_email").IsUnique();

            entity.HasMany(e => e.Todos)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.ToTable("todos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Body).HasColumnName("body");
            entity.Property(e => e.Status).HasColumnName("status").IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_todos_user");
            entity.HasIndex(e => new { e.UserId, e.Status }).HasDatabaseName("idx_todos_user_status");
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is User || e.Entity is TodoItem);

        foreach (var entry in entries)
        {
            var now = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is User user)
                {
                    user.CreatedAt = now;
                    user.UpdatedAt = now;
                }
                else if (entry.Entity is TodoItem todo)
                {
                    todo.CreatedAt = now;
                    todo.UpdatedAt = now;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is User user)
                {
                    user.UpdatedAt = now;
                }
                else if (entry.Entity is TodoItem todo)
                {
                    todo.UpdatedAt = now;

                    if (todo.Status == TodoStatus.Completed && todo.CompletedAt == null)
                    {
                        todo.CompletedAt = now;
                    }
                    else if (todo.Status != TodoStatus.Completed)
                    {
                        todo.CompletedAt = null;
                    }
                }
            }
        }
    }
}
