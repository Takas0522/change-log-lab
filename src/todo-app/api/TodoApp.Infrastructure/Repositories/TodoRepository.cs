using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Repositories.Interfaces;

namespace TodoApp.Infrastructure.Repositories;

/// <summary>
/// ToDoリポジトリ実装
/// REQ-COMP-002対応: EF Core データアクセス層
/// </summary>
public class TodoRepository : ITodoRepository
{
    private readonly TodoDbContext _context;

    public TodoRepository(TodoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<(List<Todo> Items, int TotalCount)> GetPagedAsync(
        string? keyword,
        List<string>? statuses,
        List<int>? labelIds,
        DateTime? startDate,
        DateTime? endDate,
        string sortBy,
        string sortOrder,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Todos
            .Include(t => t.TodoLabels)
                .ThenInclude(tl => tl.Label)
            .Where(t => !t.IsDeleted);

        // キーワード検索
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(t =>
                EF.Functions.Like(t.Title, $"%{keyword}%") ||
                (t.Content != null && EF.Functions.Like(t.Content, $"%{keyword}%")));
        }

        // ステータスフィルタ
        if (statuses != null && statuses.Any())
        {
            query = query.Where(t => statuses.Contains(t.Status));
        }

        // ラベルフィルタ
        if (labelIds != null && labelIds.Any())
        {
            query = query.Where(t => t.TodoLabels.Any(tl => labelIds.Contains(tl.LabelId)));
        }

        // 作成日フィルタ
        if (startDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            // 終了日は23:59:59まで含める
            var endDateTime = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(t => t.CreatedAt <= endDateTime);
        }

        // 総件数取得
        var totalCount = await query.CountAsync(cancellationToken);

        // ソート
        query = sortBy.ToLower() switch
        {
            "title" => sortOrder.ToUpper() == "ASC" 
                ? query.OrderBy(t => t.Title) 
                : query.OrderByDescending(t => t.Title),
            "status" => sortOrder.ToUpper() == "ASC" 
                ? query.OrderBy(t => t.Status) 
                : query.OrderByDescending(t => t.Status),
            "updatedat" => sortOrder.ToUpper() == "ASC" 
                ? query.OrderBy(t => t.UpdatedAt) 
                : query.OrderByDescending(t => t.UpdatedAt),
            _ => sortOrder.ToUpper() == "ASC" 
                ? query.OrderBy(t => t.CreatedAt) 
                : query.OrderByDescending(t => t.CreatedAt),
        };

        // ページング
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<Todo?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Todos
            .Include(t => t.TodoLabels)
                .ThenInclude(tl => tl.Label)
            .Where(t => t.TodoId == id && !t.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Todo> AddAsync(Todo todo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(todo);

        todo.CreatedAt = DateTime.UtcNow;
        todo.UpdatedAt = DateTime.UtcNow;
        todo.IsDeleted = false;

        await _context.Todos.AddAsync(todo, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // ラベルを含めて再取得
        return await GetByIdAsync(todo.TodoId, cancellationToken) 
            ?? throw new InvalidOperationException("Failed to retrieve added Todo");
    }

    /// <inheritdoc />
    public async Task<Todo> UpdateAsync(Todo todo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(todo);

        var existing = await _context.Todos
            .Include(t => t.TodoLabels)
            .FirstOrDefaultAsync(t => t.TodoId == todo.TodoId && !t.IsDeleted, cancellationToken);

        if (existing == null)
        {
            throw new InvalidOperationException($"Todo with ID {todo.TodoId} not found");
        }

        // プロパティの更新
        existing.Title = todo.Title;
        existing.Content = todo.Content;
        existing.Status = todo.Status;
        existing.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("This Todo has been modified by another user. Please refresh and try again.", ex);
        }

        // ラベルを含めて再取得
        return await GetByIdAsync(todo.TodoId, cancellationToken) 
            ?? throw new InvalidOperationException("Failed to retrieve updated Todo");
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var todo = await _context.Todos
            .FirstOrDefaultAsync(t => t.TodoId == id && !t.IsDeleted, cancellationToken);

        if (todo == null)
        {
            throw new InvalidOperationException($"Todo with ID {id} not found");
        }

        // 論理削除
        todo.IsDeleted = true;
        todo.DeletedAt = DateTime.UtcNow;
        todo.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
