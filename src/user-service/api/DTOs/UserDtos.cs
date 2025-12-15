namespace UserApi.DTOs;

public record UserProfileResponse
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record UpdateProfileRequest
{
    public string? DisplayName { get; init; }
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
}

public record CreateProfileRequest
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
}

public record UserSearchResult
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
}

public record UserSearchResponse
{
    public required List<UserSearchResult> Users { get; init; }
    public required int TotalCount { get; init; }
}
