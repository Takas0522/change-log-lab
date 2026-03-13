using TodoApp.Application.Common;
using TodoApp.Application.DTOs;
using TodoApp.Application.Services.Interfaces;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure.Repositories.Interfaces;

namespace TodoApp.Application.Services;

/// <summary>
/// ToDoサービス実装
/// REQ-FUNC-001～007, REQ-FUNC-013～017対応
/// </summary>
public class TodoService : ITodoService
{
    private readonly ITodoRepository _todoRepository;
    private readonly ILabelRepository _labelRepository;

    public TodoService(
        ITodoRepository todoRepository,
        ILabelRepository labelRepository)
    {
        _todoRepository = todoRepository ?? throw new ArgumentNullException(nameof(todoRepository));
        _labelRepository = labelRepository ?? throw new ArgumentNullException(nameof(labelRepository));
    }

    /// <inheritdoc />
    public async Task<PagedResult<TodoDto>> GetTodosAsync(
        TodoQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        // バリデーション
        if (parameters.Page < 1) parameters.Page = 1;
        if (parameters.PageSize < 1) parameters.PageSize = 20;
        if (parameters.PageSize > 100) parameters.PageSize = 100;

        var (items, totalCount) = await _todoRepository.GetPagedAsync(
            parameters.Keyword,
            parameters.Statuses,
            parameters.LabelIds,
            parameters.StartDate,
            parameters.EndDate,
            parameters.SortBy,
            parameters.SortOrder,
            parameters.Page,
            parameters.PageSize,
            cancellationToken);

        return new PagedResult<TodoDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<TodoDto> GetTodoByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var todo = await _todoRepository.GetByIdAsync(id, cancellationToken);
        
        if (todo == null)
        {
            throw new NotFoundException("Todo", id);
        }

        return MapToDto(todo);
    }

    /// <inheritdoc />
    public async Task<TodoDto> CreateTodoAsync(
        CreateTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // バリデーション
        ValidateCreateRequest(request);

        // ラベルの存在確認
        List<Label>? labels = null;
        if (request.LabelIds != null && request.LabelIds.Any())
        {
            labels = await _labelRepository.GetByIdsAsync(request.LabelIds, cancellationToken);
            if (labels.Count != request.LabelIds.Count)
            {
                throw new ValidationException("One or more label IDs are invalid");
            }
        }

        // Todoエンティティの作成
        var todo = new Todo
        {
            Title = request.Title,
            Content = request.Content,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // ラベルの関連付け
        if (labels != null && labels.Any())
        {
            todo.TodoLabels = labels.Select(l => new TodoLabel
            {
                LabelId = l.LabelId,
                Label = l
            }).ToList();
        }

        var created = await _todoRepository.AddAsync(todo, cancellationToken);
        return MapToDto(created);
    }

    /// <inheritdoc />
    public async Task<TodoDto> UpdateTodoAsync(
        long id,
        UpdateTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var todo = await _todoRepository.GetByIdAsync(id, cancellationToken);
        if (todo == null)
        {
            throw new NotFoundException("Todo", id);
        }

        // バリデーション
        ValidateUpdateRequest(request);

        // ラベルの存在確認
        List<Label>? labels = null;
        if (request.LabelIds != null && request.LabelIds.Any())
        {
            labels = await _labelRepository.GetByIdsAsync(request.LabelIds, cancellationToken);
            if (labels.Count != request.LabelIds.Count)
            {
                throw new ValidationException("One or more label IDs are invalid");
            }
        }

        // 更新
        todo.Title = request.Title;
        todo.Content = request.Content;
        todo.Status = request.Status;

        // ラベルの更新
        todo.TodoLabels.Clear();
        if (labels != null && labels.Any())
        {
            todo.TodoLabels = labels.Select(l => new TodoLabel
            {
                TodoId = todo.TodoId,
                LabelId = l.LabelId,
                Label = l
            }).ToList();
        }

        try
        {
            var updated = await _todoRepository.UpdateAsync(todo, cancellationToken);
            return MapToDto(updated);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("modified by another user"))
        {
            throw new ConcurrencyException("This Todo has been modified by another user. Please refresh and try again.");
        }
    }

    /// <inheritdoc />
    public async Task DeleteTodoAsync(long id, CancellationToken cancellationToken = default)
    {
        var todo = await _todoRepository.GetByIdAsync(id, cancellationToken);
        if (todo == null)
        {
            throw new NotFoundException("Todo", id);
        }

        await _todoRepository.DeleteAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TodoDto> UpdateStatusAsync(
        long id,
        UpdateTodoStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var todo = await _todoRepository.GetByIdAsync(id, cancellationToken);
        if (todo == null)
        {
            throw new NotFoundException("Todo", id);
        }

        // ステータス検証
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            throw new ValidationException("Status is required");
        }

        todo.Status = request.Status;

        try
        {
            var updated = await _todoRepository.UpdateAsync(todo, cancellationToken);
            return MapToDto(updated);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("modified by another user"))
        {
            throw new ConcurrencyException("This Todo has been modified by another user. Please refresh and try again.");
        }
    }

    /// <summary>
    /// TodoエンティティをDTOに変換
    /// </summary>
    private static TodoDto MapToDto(Todo todo)
    {
        return new TodoDto
        {
            TodoId = todo.TodoId,
            Title = todo.Title,
            Content = todo.Content,
            Status = todo.Status,
            CreatedAt = todo.CreatedAt,
            UpdatedAt = todo.UpdatedAt,
            Labels = todo.TodoLabels
                .Where(tl => tl.Label != null && !tl.Label.IsDeleted)
                .Select(tl => new LabelDto
                {
                    LabelId = tl.Label!.LabelId,
                    Name = tl.Label.Name,
                    Color = tl.Label.Color,
                    CreatedAt = tl.Label.CreatedAt
                })
                .ToList()
        };
    }

    /// <summary>
    /// 作成リクエストのバリデーション
    /// </summary>
    private static void ValidateCreateRequest(CreateTodoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ValidationException("Title is required");
        }

        if (request.Title.Length > 200)
        {
            throw new ValidationException("Title must not exceed 200 characters");
        }

        if (request.Content != null && request.Content.Length > 4000)
        {
            throw new ValidationException("Content must not exceed 4000 characters");
        }

        if (request.LabelIds != null && request.LabelIds.Count > 10)
        {
            throw new ValidationException("Maximum 10 labels can be assigned");
        }
    }

    /// <summary>
    /// 更新リクエストのバリデーション
    /// </summary>
    private static void ValidateUpdateRequest(UpdateTodoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ValidationException("Title is required");
        }

        if (request.Title.Length > 200)
        {
            throw new ValidationException("Title must not exceed 200 characters");
        }

        if (request.Content != null && request.Content.Length > 4000)
        {
            throw new ValidationException("Content must not exceed 4000 characters");
        }

        if (request.LabelIds != null && request.LabelIds.Count > 10)
        {
            throw new ValidationException("Maximum 10 labels can be assigned");
        }
    }
}
