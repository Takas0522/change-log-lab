using System.Text.Json;

namespace OrderClientApp.Domain.Tests.E2E;

public sealed class WpfNativeE2EContractTests
{
    [Fact]
    public void WinAppCliSmokeSuite_DefinesAllCriticalFeatureAreas()
    {
        var suite = LoadSuite("smoke");
        var featureAreas = suite.Scenarios
            .Select(x => x.FeatureArea)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("auth", featureAreas);
        Assert.Contains("order-core", featureAreas);
        Assert.Contains("approval-inventory-budget", featureAreas);
        Assert.Contains("masters-analytics", featureAreas);
        Assert.Contains("ops-settings", featureAreas);
    }

    [Fact]
    public void WinAppCliRegressionSuite_ContainsOpsSettingsPlaceholderContract()
    {
        var suite = LoadSuite("regression");

        var opsScenario = suite.Scenarios.FirstOrDefault(x => x.FeatureArea.Equals("ops-settings", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(opsScenario);
        Assert.Equal("placeholder", opsScenario!.ExecutionMode);
        Assert.NotEmpty(opsScenario.CommandContract);
    }

    private static E2ESuite LoadSuite(string suiteName)
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, "tests", "wpf-native-e2e", "winappcli", "suites", $"{suiteName}.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<E2ESuite>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException($"Failed to parse suite: {path}");
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OrderClientApp.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    private sealed record E2ESuite(string Suite, IReadOnlyCollection<E2EScenario> Scenarios);

    private sealed record E2EScenario(
        string Id,
        string FeatureArea,
        string ExecutionMode,
        string[] Tags,
        string[] CommandContract);
}
