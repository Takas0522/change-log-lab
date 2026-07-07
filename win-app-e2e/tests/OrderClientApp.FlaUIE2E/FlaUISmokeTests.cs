using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using FluentAssertions;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using Xunit.Abstractions;

namespace OrderClientApp.FlaUIE2E;

public sealed class FlaUISmokeTests : IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    private readonly ITestOutputHelper _output;
    private Application? _application;
    private UIA3Automation? _automation;

    public FlaUISmokeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void AdminLogin_ReachesDashboard()
    {
        StartApplication();

        try
        {
            var window = LoginAsAdmin();

            var header = WaitUntil(
                () => FindByAutomationId(window, "HeaderTextBlock"),
                element => element.IsAvailable && !string.IsNullOrWhiteSpace(element.Name),
                DefaultTimeout,
                "dashboard header text");

            header.Name.Should().Contain("admin.user");
            header.Name.Should().Contain("管理者");

            var adminButton = WaitUntil(
                () => FindByAutomationId(window, "AdminButton"),
                element => element.IsAvailable && !element.Properties.IsOffscreen.Value,
                DefaultTimeout,
                "admin button to be visible");

            adminButton.Name.Should().Be("設定");

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
        StartApplication();

        try
        {
            var mainWindow = LoginAsAdmin();
            FindByAutomationId(mainWindow, "OrderManagementButton").AsButton().Click();

            WaitUntil(
                () => FindByAutomationId(mainWindow, "PagerInfoTextBlock"),
                element => element.IsAvailable && element.Name.Contains("全", StringComparison.Ordinal),
                DefaultTimeout,
                "order list pager");

            FindByAutomationId(mainWindow, "CreateOrderButton").AsButton().Click();
            WaitUntil(
                () => FindByAutomationId(mainWindow, "LineItemsDataGrid"),
                element => element.IsAvailable,
                DefaultTimeout,
                "new order detail line item grid");

            var supplierName = $"E2E Supplier {DateTimeOffset.Now:HHmmss}";
            var productCode = $"E2E-{DateTimeOffset.Now:HHmmss}";
            CreateOrder(mainWindow, supplierName, productCode);

            var pager = WaitUntil(
                () => FindByAutomationId(mainWindow, "PagerInfoTextBlock"),
                element => element.IsAvailable && element.Name.Contains("全", StringComparison.Ordinal),
                DefaultTimeout,
                "order list pager after save");

            pager.Name.Should().Contain("全");
            CaptureScreenshot("order-management-created");
        }
        catch
        {
            CaptureScreenshot("order-management-failure");
            throw;
        }
    }

    private void StartApplication()
    {
        var appPath = FlaUITestSettings.ResolveAppPath();
        File.Exists(appPath).Should().BeTrue($"the WPF app must be built before running E2E tests. Expected: {appPath}");

        _application = Application.Launch(appPath);
        _automation = new UIA3Automation();
    }

    private Window LoginAsAdmin()
    {
        _application.Should().NotBeNull();
        _automation.Should().NotBeNull();

        var window = _application!.GetMainWindow(_automation!, DefaultTimeout);
        window.Should().NotBeNull();
        window.Title.Should().Be("発注管理クライアント");
        ArrangeApplicationWindows();

        SetText(window, "UsernameTextBox", "admin.user");
        var passwordBox = FindByAutomationId(window, "PasswordBox");
        SetText(passwordBox, "Admin#2026");
        passwordBox.Focus();
        Keyboard.Press(VirtualKeyShort.RETURN);

        WaitUntil(
            () => FindByAutomationId(window, "HeaderTextBlock"),
            element => element.IsAvailable && !string.IsNullOrWhiteSpace(element.Name),
            DefaultTimeout,
            "dashboard header text");

        return window;
    }

    private void CreateOrder(Window orderDetailWindow, string supplierName, string productCode)
    {
        SetText(orderDetailWindow, "SupplierTextBox", supplierName);
        SetText(orderDetailWindow, "NoteTextBox", "Created by FlaUI E2E");

        var lineItemsGrid = FindByAutomationId(orderDetailWindow, "LineItemsDataGrid");
        lineItemsGrid.Focus();
        var bounds = lineItemsGrid.BoundingRectangle;
        Mouse.Click(new Point(bounds.Left + 24, bounds.Top + 44));

        Keyboard.Type(productCode);
        Keyboard.Press(VirtualKeyShort.TAB);
        Keyboard.Type("E2E Item");
        Keyboard.Press(VirtualKeyShort.TAB);
        Keyboard.Type("2");
        Keyboard.Press(VirtualKeyShort.TAB);
        Keyboard.Type("1200");

        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_S);
    }

    public void Dispose()
    {
        _automation?.Dispose();

        if (_application is null || _application.HasExited)
        {
            return;
        }

        try
        {
            _application.Close();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Failed to close WPF app gracefully: {ex.Message}");
            _application.Kill();
        }
    }

    private static AutomationElement FindByAutomationId(Window window, string automationId)
        => window.FindFirstDescendant(cf => cf.ByAutomationId(automationId))
           ?? throw new InvalidOperationException($"AutomationId '{automationId}' was not found.");

    private static void SetText(Window window, string automationId, string value)
        => SetText(FindByAutomationId(window, automationId), value);

    private static void SetText(AutomationElement element, string value)
    {
        var valuePattern = element.Patterns.Value.PatternOrDefault
            ?? throw new InvalidOperationException($"AutomationId '{element.AutomationId}' does not support ValuePattern.");

        valuePattern.SetValue(value);
    }

    private static AutomationElement WaitUntil(
        Func<AutomationElement> find,
        Func<AutomationElement, bool> predicate,
        TimeSpan timeout,
        string description)
    {
        var result = Retry.WhileException(
            () =>
            {
                var element = find();
                if (!predicate(element))
                {
                    throw new InvalidOperationException($"Still waiting for {description}.");
                }

                return element;
            },
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!result.Success)
        {
            throw new TimeoutException($"Timed out waiting for {description}.", result.LastException);
        }

        return result.Result
               ?? throw new TimeoutException($"Timed out waiting for {description}.", result.LastException);
    }

    private void CaptureScreenshot(string scenarioName)
    {
        if (_automation is null || _application is null || _application.HasExited)
        {
            return;
        }

        try
        {
            var artifactDirectory = Path.Combine(FlaUITestSettings.RepositoryRoot, "artifacts", "e2e", "flaui");
            Directory.CreateDirectory(artifactDirectory);

            var fileName = $"{scenarioName}-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.png";
            var path = Path.Combine(artifactDirectory, fileName);
            var bounds = ArrangeApplicationWindows();
            if (bounds is null)
            {
                return;
            }

            CaptureScreenRectangle(bounds.Value, path);
            _output.WriteLine($"Screenshot saved: {path}");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Failed to capture screenshot: {ex.Message}");
        }
    }

    private Rectangle? ArrangeApplicationWindows()
    {
        if (_application is null || _application.HasExited)
        {
            return null;
        }

        return ArrangeProcessWindows(_application.ProcessId);
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
        EnumWindows((handle, _) =>
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

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

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
