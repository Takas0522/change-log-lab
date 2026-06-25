using OrderClientApp.Application.Abstractions.Operations;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Application.Abstractions.Settings;

namespace OrderClientApp.Application.Services.Settings;

public sealed class AppSettingsService : IAppSettingsService
{
    private readonly IAppSettingsRepository _appSettingsRepository;
    private readonly IBudgetSettingsRepository _budgetSettingsRepository;
    private readonly IOperationLogService _operationLogService;
    private readonly TimeProvider _timeProvider;

    public AppSettingsService(
        IAppSettingsRepository appSettingsRepository,
        IBudgetSettingsRepository budgetSettingsRepository,
        IOperationLogService operationLogService,
        TimeProvider? timeProvider = null)
    {
        _appSettingsRepository = appSettingsRepository;
        _budgetSettingsRepository = budgetSettingsRepository;
        _operationLogService = operationLogService;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<AppSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var app = await _appSettingsRepository.GetAsync(cancellationToken);
        var budget = await _budgetSettingsRepository.GetAsync(cancellationToken);
        return app with { ApprovalThreshold = budget.ApprovalThreshold };
    }

    public async Task<AppSettingsDto> SaveAsync(SaveAppSettingsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ApprovalThreshold < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.ApprovalThreshold));
        }

        var companyName = Normalize(request.CompanyName, 100, "Company name");
        var companyAddress = Normalize(request.CompanyAddress, 300, "Company address");
        var theme = NormalizeTheme(request.Theme);
        var nowUtc = _timeProvider.GetUtcNow();

        var budget = await _budgetSettingsRepository.GetAsync(cancellationToken);
        await _budgetSettingsRepository.UpsertAsync(
            request.ApprovalThreshold,
            budget.MonthlyLimit,
            budget.YearlyLimit,
            nowUtc,
            cancellationToken);

        var app = await _appSettingsRepository.UpsertAsync(
            companyName,
            companyAddress,
            theme,
            nowUtc,
            cancellationToken);

        await _operationLogService.LogAsync(
            "Settings",
            "Update",
            request.UpdatedBy,
            $"設定を更新しました: Company={companyName}, Theme={theme}, ApprovalThreshold={request.ApprovalThreshold}",
            cancellationToken);

        return app with { ApprovalThreshold = request.ApprovalThreshold };
    }

    private static string Normalize(string value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} is required.", fieldName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"{fieldName} is too long.", fieldName);
        }

        return normalized;
    }

    private static string NormalizeTheme(string theme)
    {
        if (string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase))
        {
            return "Dark";
        }

        return "Light";
    }
}
