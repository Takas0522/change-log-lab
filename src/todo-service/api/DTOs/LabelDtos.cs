namespace TodoApi.DTOs;

// Label DTOs
public record LabelDto(
    Guid Id,
    Guid ListId,
    string Name,
    string Color,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateLabelRequest(
    string Name,
    string Color
);

public record UpdateLabelRequest(
    string? Name,
    string? Color
);

// TodoLabel DTOs
public record TodoLabelDto(
    Guid Id,
    Guid TodoId,
    Guid LabelId,
    LabelDto Label
);

public record AssignLabelRequest(
    Guid LabelId
);
