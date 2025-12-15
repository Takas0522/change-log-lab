using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;

public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options)
    {
    }

    public DbSet<List> Lists { get; set; } = null!;
    public DbSet<Todo> Todos { get; set; } = null!;
    public DbSet<ListMember> ListMembers { get; set; } = null!;
    public DbSet<OutboxEvent> OutboxEvents { get; set; } = null!;
    public DbSet<Label> Labels { get; set; } = null!;
    public DbSet<TodoLabel> TodoLabels { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure List entity
        modelBuilder.Entity<List>(entity =>
        {
            entity.ToTable("lists");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Title).HasColumnName("title").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.OwnerId).HasDatabaseName("idx_lists_owner");

            // Configure relationships
            entity.HasMany(e => e.Todos)
                .WithOne(e => e.List)
                .HasForeignKey(e => e.ListId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Members)
                .WithOne(e => e.List)
                .HasForeignKey(e => e.ListId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Todo entity
        modelBuilder.Entity<Todo>(entity =>
        {
            entity.ToTable("todos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ListId).HasColumnName("list_id").IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsCompleted).HasColumnName("is_completed");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.Position).HasColumnName("position");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.ListId).HasDatabaseName("idx_todos_list");
            entity.HasIndex(e => new { e.ListId, e.Position }).HasDatabaseName("idx_todos_list_position");

            // Configure relationships
            entity.HasMany(e => e.TodoLabels)
                .WithOne(e => e.Todo)
                .HasForeignKey(e => e.TodoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ListMember entity
        modelBuilder.Entity<ListMember>(entity =>
        {
            entity.ToTable("list_members");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ListId).HasColumnName("list_id").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.Role).HasColumnName("role").IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.ListId).HasDatabaseName("idx_list_members_list");
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_list_members_user");
            entity.HasIndex(e => new { e.ListId, e.UserId })
                .HasDatabaseName("idx_list_members_list_user")
                .IsUnique();
        });

        // Configure OutboxEvent entity
        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.ToTable("outbox_events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EventId).HasColumnName("event_id").IsRequired();
            entity.Property(e => e.EventType).HasColumnName("event_type").IsRequired().HasMaxLength(100);
            entity.Property(e => e.AggregateId).HasColumnName("aggregate_id").IsRequired();
            entity.Property(e => e.AggregateType).HasColumnName("aggregate_type").IsRequired().HasMaxLength(50);
            entity.Property(e => e.Payload).HasColumnName("payload").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");

            entity.HasIndex(e => e.EventId).HasDatabaseName("idx_outbox_events_event_id").IsUnique();
        });

        // Configure Label entity
        modelBuilder.Entity<Label>(entity =>
        {
            entity.ToTable("labels");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Color).HasColumnName("color").IsRequired().HasMaxLength(7);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_labels_user");

            // Configure relationships
            entity.HasMany(e => e.TodoLabels)
                .WithOne(e => e.Label)
                .HasForeignKey(e => e.LabelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TodoLabel entity
        modelBuilder.Entity<TodoLabel>(entity =>
        {
            entity.ToTable("todo_labels");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TodoId).HasColumnName("todo_id").IsRequired();
            entity.Property(e => e.LabelId).HasColumnName("label_id").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.TodoId).HasDatabaseName("idx_todo_labels_todo");
            entity.HasIndex(e => e.LabelId).HasDatabaseName("idx_todo_labels_label");
            entity.HasIndex(e => new { e.TodoId, e.LabelId })
                .IsUnique();
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
            .Where(e => e.Entity is List || e.Entity is Todo || e.Entity is ListMember || e.Entity is Label || e.Entity is TodoLabel);

        foreach (var entry in entries)
        {
            var now = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is List list)
                {
                    list.CreatedAt = now;
                    list.UpdatedAt = now;
                }
                else if (entry.Entity is Todo todo)
                {
                    todo.CreatedAt = now;
                    todo.UpdatedAt = now;
                }
                else if (entry.Entity is ListMember member)
                {
                    member.CreatedAt = now;
                    member.UpdatedAt = now;
                }
                else if (entry.Entity is Label label)
                {
                    label.CreatedAt = now;
                    label.UpdatedAt = now;
                }
                else if (entry.Entity is TodoLabel todoLabel)
                {
                    todoLabel.CreatedAt = now;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is List list)
                {
                    list.UpdatedAt = now;
                }
                else if (entry.Entity is Todo todo)
                {
                    todo.UpdatedAt = now;
                }
                else if (entry.Entity is ListMember member)
                {
                    member.UpdatedAt = now;
                }
                else if (entry.Entity is Label label)
                {
                    label.UpdatedAt = now;
                }
            }
        }
    }
}
