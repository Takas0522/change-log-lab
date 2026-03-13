using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Repositories.Interfaces;

namespace TodoApp.Infrastructure.Repositories;

/// <summary>
/// ラベルリポジトリ実装
/// REQ-FUNC-008～012対応
/// </summary>
public class LabelRepository : ILabelRepository
{
    private readonly TodoDbContext _context;

    public LabelRepository(TodoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<List<Label>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Labels
            .Where(l => !l.IsDeleted)
            .OrderBy(l => l.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Label?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Labels
            .Where(l => l.LabelId == id && !l.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Label>> GetByIdsAsync(List<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids == null || !ids.Any())
        {
            return new List<Label>();
        }

        return await _context.Labels
            .Where(l => ids.Contains(l.LabelId) && !l.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Label?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return await _context.Labels
            .Where(l => l.Name == name && !l.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Label> AddAsync(Label label, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(label);

        label.CreatedAt = DateTime.UtcNow;
        label.IsDeleted = false;

        await _context.Labels.AddAsync(label, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return label;
    }

    /// <inheritdoc />
    public async Task<Label> UpdateAsync(Label label, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(label);

        var existing = await _context.Labels
            .FirstOrDefaultAsync(l => l.LabelId == label.LabelId && !l.IsDeleted, cancellationToken);

        if (existing == null)
        {
            throw new InvalidOperationException($"Label with ID {label.LabelId} not found");
        }

        existing.Name = label.Name;
        existing.Color = label.Color;

        await _context.SaveChangesAsync(cancellationToken);

        return existing;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var label = await _context.Labels
            .FirstOrDefaultAsync(l => l.LabelId == id && !l.IsDeleted, cancellationToken);

        if (label == null)
        {
            throw new InvalidOperationException($"Label with ID {id} not found");
        }

        // 論理削除
        label.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
