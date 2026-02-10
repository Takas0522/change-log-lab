namespace SreAgentLab.DTOs;

public record RegisterRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? DisplayName { get; init; }
}

public record LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

public record AuthResponse
{
    public required string Token { get; init; }
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public string? DisplayName { get; init; }
}

public record UserInfoResponse
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public string? DisplayName { get; init; }
}
