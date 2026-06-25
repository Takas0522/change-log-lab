namespace OrderClientApp.Application.Abstractions.Auth;

public sealed record PasswordHashResult(string Hash, string Salt);
