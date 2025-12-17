namespace TodoApi.DTOs;

// Label DTOs
public record CreateLabelRequest(
    string Name,
    string Color  // HEX format (#RRGGBB)
);

public record UpdateLabelRequest(
    string? Name,
    string? Color  // HEX format (#RRGGBB)
);

public record LabelResponse(
    Guid Id,
    Guid UserId,
    string Name,
    string Color,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
