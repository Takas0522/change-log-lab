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
    public DbSet<ListInvite> ListInvites { get; set; } = null!;
    public DbSet<OutboxEvent> OutboxEvents { get; set; } = null!;

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

        // Configure ListInvite entity
        modelBuilder.Entity<ListInvite>(entity =>
        {
            entity.ToTable("list_invites");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ListId).HasColumnName("list_id").IsRequired();
            entity.Property(e => e.InviterUserId).HasColumnName("inviter_user_id").IsRequired();
            entity.Property(e => e.InviteeUserId).HasColumnName("invitee_user_id").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.ListId).HasDatabaseName("idx_list_invites_list");
            entity.HasIndex(e => e.InviteeUserId).HasDatabaseName("idx_list_invites_invitee");
            entity.HasIndex(e => e.Status).HasDatabaseName("idx_list_invites_status");

            // Configure relationship
            entity.HasOne(e => e.List)
                .WithMany()
                .HasForeignKey(e => e.ListId)
                .OnDelete(DeleteBehavior.Cascade);
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
            .Where(e => e.Entity is List || e.Entity is Todo || e.Entity is ListMember || e.Entity is ListInvite);

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
                else if (entry.Entity is ListInvite invite)
                {
                    invite.CreatedAt = now;
                    invite.UpdatedAt = now;
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
                else if (entry.Entity is ListInvite invite)
                {
                    invite.UpdatedAt = now;
                }
            }
        }
    }
}
