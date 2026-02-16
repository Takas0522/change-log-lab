using TodoApp.Application.Common;
using TodoApp.Application.DTOs;
using TodoApp.Application.Services.Interfaces;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure.Repositories.Interfaces;

namespace TodoApp.Application.Services;

/// <summary>
/// ラベルサービス実装
/// REQ-FUNC-008～012対応
/// </summary>
public class LabelService : ILabelService
{
    private readonly ILabelRepository _labelRepository;

    public LabelService(ILabelRepository labelRepository)
    {
        _labelRepository = labelRepository ?? throw new ArgumentNullException(nameof(labelRepository));
    }

    /// <inheritdoc />
    public async Task<List<LabelDto>> GetAllLabelsAsync(CancellationToken cancellationToken = default)
    {
        var labels = await _labelRepository.GetAllAsync(cancellationToken);
        return labels.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<LabelDto> GetLabelByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var label = await _labelRepository.GetByIdAsync(id, cancellationToken);
        
        if (label == null)
        {
            throw new NotFoundException("Label", id);
        }

        return MapToDto(label);
    }

    /// <inheritdoc />
    public async Task<LabelDto> CreateLabelAsync(
        CreateLabelRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // バリデーション
        ValidateCreateRequest(request);

        // 名前の重複チェック
        var existing = await _labelRepository.GetByNameAsync(request.Name, cancellationToken);
        if (existing != null)
        {
            throw new ValidationException($"Label with name '{request.Name}' already exists");
        }

        var label = new Label
        {
            Name = request.Name,
            Color = request.Color,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _labelRepository.AddAsync(label, cancellationToken);
        return MapToDto(created);
    }

    /// <inheritdoc />
    public async Task<LabelDto> UpdateLabelAsync(
        int id,
        UpdateLabelRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var label = await _labelRepository.GetByIdAsync(id, cancellationToken);
        if (label == null)
        {
            throw new NotFoundException("Label", id);
        }

        // バリデーション
        ValidateUpdateRequest(request);

        // 名前の重複チェック（自分以外）
        var existing = await _labelRepository.GetByNameAsync(request.Name, cancellationToken);
        if (existing != null && existing.LabelId != id)
        {
            throw new ValidationException($"Label with name '{request.Name}' already exists");
        }

        label.Name = request.Name;
        label.Color = request.Color;

        var updated = await _labelRepository.UpdateAsync(label, cancellationToken);
        return MapToDto(updated);
    }

    /// <inheritdoc />
    public async Task DeleteLabelAsync(int id, CancellationToken cancellationToken = default)
    {
        var label = await _labelRepository.GetByIdAsync(id, cancellationToken);
        if (label == null)
        {
            throw new NotFoundException("Label", id);
        }

        await _labelRepository.DeleteAsync(id, cancellationToken);
    }

    /// <summary>
    /// LabelエンティティをDTOに変換
    /// </summary>
    private static LabelDto MapToDto(Label label)
    {
        return new LabelDto
        {
            LabelId = label.LabelId,
            Name = label.Name,
            Color = label.Color,
            CreatedAt = label.CreatedAt
        };
    }

    /// <summary>
    /// 作成リクエストのバリデーション
    /// </summary>
    private static void ValidateCreateRequest(CreateLabelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Label name is required");
        }

        if (request.Name.Length > 50)
        {
            throw new ValidationException("Label name must not exceed 50 characters");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Color, @"^#[0-9A-Fa-f]{6}$"))
        {
            throw new ValidationException("Color must be in HEX format (#RRGGBB)");
        }
    }

    /// <summary>
    /// 更新リクエストのバリデーション
    /// </summary>
    private static void ValidateUpdateRequest(UpdateLabelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Label name is required");
        }

        if (request.Name.Length > 50)
        {
            throw new ValidationException("Label name must not exceed 50 characters");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Color, @"^#[0-9A-Fa-f]{6}$"))
        {
            throw new ValidationException("Color must be in HEX format (#RRGGBB)");
        }
    }
}
