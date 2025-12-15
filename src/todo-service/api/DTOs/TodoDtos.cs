namespace TodoApi.DTOs;

// List DTOs
public record CreateListRequest(
    string Title,
    string? Description
);

public record UpdateListRequest(
    string? Title,
    string? Description
);

public record ListResponse(
    Guid Id,
    string Title,
    string? Description,
    Guid OwnerId,
    string UserRole,  // owner, editor, viewer
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ListDetailResponse(
    Guid Id,
    string Title,
    string? Description,
    Guid OwnerId,
    string UserRole,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<TodoResponse> Todos
);

// Todo DTOs
public record CreateTodoRequest(
    string Title,
    string? Description,
    DateTime? DueDate,
    int? Position
);

public record UpdateTodoRequest(
    string? Title,
    string? Description,
    bool? IsCompleted,
    DateTime? DueDate,
    int? Position
);

public record TodoResponse(
    Guid Id,
    Guid ListId,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime? DueDate,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// List Member DTOs
public record AddMemberRequest(
    Guid UserId,
    string Role  // owner, editor, viewer
);

public record UpdateMemberRoleRequest(
    string Role
);

public record ListMemberResponse(
    Guid Id,
    Guid ListId,
    Guid UserId,
    string Role,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
