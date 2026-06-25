namespace OrderClientApp.Application.Abstractions.Settings;

public sealed record AppSettingsDto(
    string CompanyName,
    string CompanyAddress,
    decimal ApprovalThreshold,
    string Theme,
    DateTimeOffset UpdatedAtUtc);

public sealed record SaveAppSettingsRequest(
    string CompanyName,
    string CompanyAddress,
    decimal ApprovalThreshold,
    string Theme,
    string? UpdatedBy);
