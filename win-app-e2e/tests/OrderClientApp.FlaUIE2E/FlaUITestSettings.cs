namespace OrderClientApp.FlaUIE2E;

internal static class FlaUITestSettings
{
    public static string RepositoryRoot { get; } = ResolveRepositoryRoot();

    public static string ResolveAppPath()
    {
        var configuredPath = Environment.GetEnvironmentVariable("ORDER_CLIENT_APP_EXE");
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        var configuration = Environment.GetEnvironmentVariable("ORDER_CLIENT_APP_CONFIGURATION");
        var configurations = string.IsNullOrWhiteSpace(configuration)
            ? ["Release", "Debug"]
            : new[] { configuration };

        foreach (var candidateConfiguration in configurations)
        {
            var candidate = Path.Combine(
                RepositoryRoot,
                "src",
                "OrderClientApp.Wpf",
                "bin",
                candidateConfiguration,
                "net10.0-windows",
                "OrderClientApp.Wpf.exe");

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(
            RepositoryRoot,
            "src",
            "OrderClientApp.Wpf",
            "bin",
            configurations[0],
            "net10.0-windows",
            "OrderClientApp.Wpf.exe");
    }

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OrderClientApp.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate OrderClientApp.slnx from the test output directory.");
    }
}

