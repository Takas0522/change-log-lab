using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using Xunit.Abstractions;

namespace OrderClientApp.NovaWindowsE2E;

public sealed class NovaWindowsSmokeTests : IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    private readonly ITestOutputHelper _output;
    private WindowsDriver? _driver;

    public NovaWindowsSmokeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void AdminLogin_ReachesDashboard()
    {
        StartSession();

        try
        {
            LoginAsAdmin();

            var header = WaitUntilDisplayedByAutomationId("HeaderTextBlock", DefaultTimeout);
            header.Text.Should().Contain("admin.user");
            header.Text.Should().Contain("管理者");

            WaitUntilDisplayedByAutomationId("AdminButton", DefaultTimeout).Displayed.Should().BeTrue();

            CaptureScreenshot("admin-login-dashboard");
        }
        catch
        {
            CaptureScreenshot("admin-login-failure");
            throw;
        }
    }

    [Fact]
    public void AdminCanCreateOrder_FromOrderManagement()
    {
        StartSession();

        try
        {
            LoginAsAdmin();
            ClickByAutomationId("OrderManagementButton");

            WaitUntilDisplayedByAutomationId("CreateOrderButton", DefaultTimeout);

            ClickByAutomationId("CreateOrderButton");
            WaitUntilDisplayedByAutomationId("SupplierTextBox", DefaultTimeout);
            WaitUntilDisplayedByAutomationId("LineItemsDataGrid", DefaultTimeout);

            var supplierName = $"E2E Supplier {DateTimeOffset.Now:HHmmss}";
            var productCode = $"E2E-{DateTimeOffset.Now:HHmmss}";
            CreateOrder(supplierName, productCode);

            WaitUntilDisplayedByAutomationId("CreateOrderButton", DefaultTimeout);

            CaptureScreenshot("order-management-created");
        }
        catch
        {
            CaptureScreenshot("order-management-failure");
            throw;
        }
    }

    private void StartSession()
    {
        var appPath = NovaWindowsTestSettings.ResolveAppPath();
        File.Exists(appPath).Should().BeTrue($"the WPF app must be built before running E2E tests. Expected: {appPath}");

        var options = new AppiumOptions
        {
            AutomationName = "NovaWindows",
            PlatformName = "Windows"
        };

        var topLevelWindow = NovaWindowsTestSettings.AppTopLevelWindow;
        if (string.IsNullOrWhiteSpace(topLevelWindow))
        {
            options.App = appPath;
            options.AddAdditionalAppiumOption("ms:waitForAppLaunch", 30000);
        }
        else
        {
            options.AddAdditionalAppiumOption("appTopLevelWindow", topLevelWindow);
        }

        _driver = new WindowsDriver(
            new Uri(NovaWindowsTestSettings.AppiumServerUrl),
            options,
            TimeSpan.FromSeconds(180));
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(500);
        ArrangeApplicationWindows();
    }

    private void LoginAsAdmin()
    {
        SetValue(FindByAutomationId("UsernameTextBox"), "admin.user");
        var passwordBox = FindByAutomationId("PasswordBox");
        SetValue(passwordBox, "Admin#2026");
        passwordBox.SendKeys(Keys.Enter);

        WaitUntilDisplayedByAutomationId("HeaderTextBlock", DefaultTimeout);
    }

    private void CreateOrder(string supplierName, string productCode)
    {
        SetValue(FindByAutomationId("SupplierTextBox"), supplierName);
        SetValue(FindByAutomationId("NoteTextBox"), "Created by NovaWindows E2E");

        var lineItemsGrid = FindByAutomationId("LineItemsDataGrid");
        _driver.Should().NotBeNull("the NovaWindows session must be created first");
        new Actions(_driver!)
            .MoveToElement(lineItemsGrid, 24, 44)
            .Click()
            .Perform();
        lineItemsGrid.SendKeys(productCode);
        lineItemsGrid.SendKeys(Keys.Tab);
        lineItemsGrid.SendKeys("E2E Item");
        lineItemsGrid.SendKeys(Keys.Tab);
        lineItemsGrid.SendKeys("2");
        lineItemsGrid.SendKeys(Keys.Tab);
        lineItemsGrid.SendKeys("1200");

        new Actions(_driver!)
            .KeyDown(Keys.Control)
            .SendKeys("s")
            .KeyUp(Keys.Control)
            .Perform();
    }

    private void ClickByAutomationId(string automationId)
    {
        var element = WaitUntilDisplayedByAutomationId(automationId, DefaultTimeout);
        element.SendKeys(Keys.Enter);
    }

    public void Dispose()
    {
        try
        {
            _driver?.Quit();
        }
        catch (WebDriverException ex)
        {
            _output.WriteLine($"Failed to quit NovaWindows session: {ex.Message}");
        }

        _driver?.Dispose();
    }

    private IWebElement FindByAutomationId(string automationId)
    {
        _driver.Should().NotBeNull("the NovaWindows session must be created first");
        return _driver!.FindElement(MobileBy.AccessibilityId(automationId));
    }

    private void SetValue(IWebElement element, string value)
    {
        _driver.Should().NotBeNull("the NovaWindows session must be created first");
        ((IJavaScriptExecutor)_driver!).ExecuteScript("windows: setValue", element, value);
    }

    private IWebElement WaitUntilDisplayedByAutomationId(string automationId, TimeSpan timeout)
        => WaitUntil(
            () =>
            {
                var element = FindByAutomationId(automationId);
                return element.Displayed ? element : null;
            },
            timeout,
            $"AutomationId '{automationId}' to be displayed");

    private IWebElement WaitUntilTextByAutomationId(string automationId, string expectedText, TimeSpan timeout)
        => WaitUntil(
            () =>
            {
                var element = FindByAutomationId(automationId);
                return element.Text.Contains(expectedText, StringComparison.Ordinal) ? element : null;
            },
            timeout,
            $"AutomationId '{automationId}' to contain '{expectedText}'");

    private void AttachToTopLevelWindow(string title, TimeSpan timeout)
    {
        var windowHandle = WaitForTopLevelWindowHandle(title, timeout);

        _driver?.Quit();
        _driver?.Dispose();

        var options = new AppiumOptions
        {
            AutomationName = "NovaWindows",
            PlatformName = "Windows"
        };
        options.AddAdditionalAppiumOption("appTopLevelWindow", $"0x{windowHandle.ToInt64():x}");

        _driver = new WindowsDriver(
            new Uri(NovaWindowsTestSettings.AppiumServerUrl),
            options,
            TimeSpan.FromSeconds(180));
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(500);
    }

    private static IntPtr WaitForTopLevelWindowHandle(string title, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var handle = FindTopLevelWindowHandle(title);
            if (handle.HasValue)
            {
                return handle.Value;
            }

            Thread.Sleep(250);
        }

        throw new WebDriverTimeoutException($"Timed out waiting for top-level window '{title}'.");
    }

    private static IntPtr? FindTopLevelWindowHandle(string title)
    {
        var processIdText = Environment.GetEnvironmentVariable("ORDER_CLIENT_APP_PROCESS_ID");
        if (!int.TryParse(processIdText, out var expectedProcessId))
        {
            throw new InvalidOperationException("ORDER_CLIENT_APP_PROCESS_ID is required when switching NovaWindows sessions between WPF windows.");
        }

        IntPtr? foundHandle = null;
        EnumWindows((handle, lParam) =>
        {
            GetWindowThreadProcessId(handle, out var processId);
            if (processId != expectedProcessId || !IsWindowVisible(handle))
            {
                return true;
            }

            var text = new StringBuilder(256);
            _ = GetWindowText(handle, text, text.Capacity);
            if (string.Equals(text.ToString(), title, StringComparison.Ordinal))
            {
                foundHandle = handle;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        return foundHandle;
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    private static T WaitUntil<T>(Func<T?> action, TimeSpan timeout, string description)
        where T : class
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                var result = action();
                if (result is not null)
                {
                    return result;
                }
            }
            catch (WebDriverException ex)
            {
                lastException = ex;
            }

            Thread.Sleep(250);
        }

        throw new WebDriverTimeoutException($"Timed out waiting for {description}.", lastException);
    }

    private void CaptureScreenshot(string scenarioName)
    {
        var artifactDirectory = Path.Combine(NovaWindowsTestSettings.RepositoryRoot, "artifacts", "e2e", "novawindows");
        Directory.CreateDirectory(artifactDirectory);

        var fileName = $"{scenarioName}-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.png";
        var path = Path.Combine(artifactDirectory, fileName);

        var bounds = ArrangeApplicationWindows();
        if (bounds is not null)
        {
            CaptureScreenRectangle(bounds.Value, path);
        }
        else if (_driver is ITakesScreenshot screenshotDriver)
        {
            screenshotDriver.GetScreenshot().SaveAsFile(path);
        }
        else
        {
            return;
        }

        _output.WriteLine($"Screenshot saved: {path}");
    }

    private static Rectangle? ArrangeApplicationWindows()
    {
        var processIdText = Environment.GetEnvironmentVariable("ORDER_CLIENT_APP_PROCESS_ID");
        if (!int.TryParse(processIdText, out var processId))
        {
            return null;
        }

        return ArrangeProcessWindows(processId);
    }

    private static Rectangle? ArrangeProcessWindows(int processId)
    {
        using var dpiScope = DpiAwarenessScope.Enter();
        var windows = EnumerateProcessWindows(processId).ToList();
        if (windows.Count == 0)
        {
            return null;
        }

        var virtualScreen = GetVirtualScreen();
        for (var index = 0; index < windows.Count; index++)
        {
            var (_, bounds) = windows[index];
            var width = Math.Min(Math.Max(bounds.Width, 1), Math.Max(virtualScreen.Width - 80, 1));
            var height = Math.Min(Math.Max(bounds.Height, 1), Math.Max(virtualScreen.Height - 80, 1));
            var x = virtualScreen.Left + 40 + (index * 32);
            var y = virtualScreen.Top + 40 + (index * 32);
            _ = MoveWindow(windows[index].Handle, x, y, width, height, true);
        }

        Thread.Sleep(250);
        var arranged = EnumerateProcessWindows(processId).Select(window => window.Bounds).ToList();
        return arranged.Count == 0 ? null : Union(arranged);
    }

    private static IReadOnlyList<(IntPtr Handle, Rectangle Bounds)> EnumerateProcessWindows(int processId)
    {
        var windows = new List<(IntPtr Handle, Rectangle Bounds)>();
        EnumWindows((handle, lParam) =>
        {
            GetWindowThreadProcessId(handle, out var windowProcessId);
            if (windowProcessId != processId || !IsWindowVisible(handle))
            {
                return true;
            }

            var title = new StringBuilder(256);
            _ = GetWindowText(handle, title, title.Capacity);
            if (string.IsNullOrWhiteSpace(title.ToString()) || !GetWindowRect(handle, out var rect))
            {
                return true;
            }

            var bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
            if (bounds.Width > 0 && bounds.Height > 0)
            {
                windows.Add((handle, bounds));
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    private static Rectangle Union(IReadOnlyCollection<Rectangle> rectangles)
    {
        var union = rectangles.First();
        foreach (var rectangle in rectangles.Skip(1))
        {
            union = Rectangle.Union(union, rectangle);
        }

        var virtualScreen = GetVirtualScreen();
        return Rectangle.Intersect(union, virtualScreen);
    }

    private static Rectangle GetVirtualScreen()
        => new(
            GetSystemMetrics(SystemMetricVirtualScreenX),
            GetSystemMetrics(SystemMetricVirtualScreenY),
            GetSystemMetrics(SystemMetricVirtualScreenWidth),
            GetSystemMetrics(SystemMetricVirtualScreenHeight));

    private static void CaptureScreenRectangle(Rectangle bounds, string path)
    {
        using var dpiScope = DpiAwarenessScope.Enter();
        using var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        bitmap.Save(path, ImageFormat.Png);
    }

    private const int SystemMetricVirtualScreenX = 76;
    private const int SystemMetricVirtualScreenY = 77;
    private const int SystemMetricVirtualScreenWidth = 78;
    private const int SystemMetricVirtualScreenHeight = 79;

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect lpRect);

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

    private sealed class DpiAwarenessScope : IDisposable
    {
        private static readonly IntPtr PerMonitorAwareV2 = new(-4);
        private readonly IntPtr _previousContext;

        private DpiAwarenessScope(IntPtr previousContext)
        {
            _previousContext = previousContext;
        }

        public static DpiAwarenessScope Enter()
            => new(SetThreadDpiAwarenessContext(PerMonitorAwareV2));

        public void Dispose()
        {
            if (_previousContext != IntPtr.Zero)
            {
                SetThreadDpiAwarenessContext(_previousContext);
            }
        }
    }

    private readonly struct NativeRect
    {
        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;
    }
}
