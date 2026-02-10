namespace SreAgentLab.DTOs;

public record CreateTodoRequest(
    string Title,
    string? Body,
    DateTime? DueDate
);

public record UpdateTodoRequest(
    string? Title,
    string? Body,
    string? Status,
    DateTime? DueDate
);

public record UpdateStatusRequest(
    string Status
);

public record TodoResponse(
    Guid Id,
    string Title,
    string? Body,
    string Status,
    DateTime CreatedAt,
    DateTime? DueDate,
    DateTime? CompletedAt,
    DateTime UpdatedAt
);
