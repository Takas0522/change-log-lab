using System.Windows;
using OrderClientApp.Application.Abstractions.Analytics;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Application.Abstractions.Products;
using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Application.Abstractions.Suppliers;
using OrderClientApp.Domain.Auth;

namespace OrderClientApp.Wpf;

public partial class MainWindow : Window
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;
    private readonly ISupplierService _supplierService;
    private readonly IAnalyticsService _analyticsService;
    private AuthenticatedUser? _authenticatedUser;

    public MainWindow(
        IAuthenticationService authenticationService,
        IAuthorizationService authorizationService,
        IOrderService orderService,
        IProductService productService,
        ISupplierService supplierService,
        IAnalyticsService analyticsService)
    {
        _authenticationService = authenticationService;
        _authorizationService = authorizationService;
        _orderService = orderService;
        _productService = productService;
        _supplierService = supplierService;
        _analyticsService = analyticsService;
        InitializeComponent();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorTextBlock.Visibility = Visibility.Collapsed;
        ErrorTextBlock.Text = string.Empty;

        var result = await _authenticationService.LoginAsync(UsernameTextBox.Text, PasswordBox.Password);
        if (!result.IsSuccess || result.User is null)
        {
            ErrorTextBlock.Text = result.ErrorMessage ?? "ログインに失敗しました。";
            ErrorTextBlock.Visibility = Visibility.Visible;
            return;
        }

        _authenticatedUser = result.User;
        await ShowDashboardAsync();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        _authenticatedUser = null;
        UsernameTextBox.Text = string.Empty;
        PasswordBox.Password = string.Empty;
        ErrorTextBlock.Text = string.Empty;
        ErrorTextBlock.Visibility = Visibility.Collapsed;

        DashboardView.Visibility = Visibility.Collapsed;
        LoginView.Visibility = Visibility.Visible;
    }

    private async Task ShowDashboardAsync()
    {
        if (_authenticatedUser is null)
        {
            return;
        }

        LoginView.Visibility = Visibility.Collapsed;
        DashboardView.Visibility = Visibility.Visible;

        HeaderTextBlock.Text = $"ダッシュボード - {_authenticatedUser.Username} ({GetRoleLabel(_authenticatedUser.Role)})";
        var alerts = await _orderService.GetInventoryAlertsAsync();
        DashboardInfoTextBlock.Text = $"ログインに成功しました。発注管理ボタンから一覧・詳細画面へ遷移できます。 在庫アラート: {alerts.Count} 件";

        ApproverButton.Visibility = _authorizationService.CanAccess(_authenticatedUser, UserRole.Approver)
            ? Visibility.Visible
            : Visibility.Collapsed;
        AdminButton.Visibility = _authorizationService.CanAccess(_authenticatedUser, UserRole.Admin)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static string GetRoleLabel(UserRole role)
        => role switch
        {
            UserRole.General => "一般ユーザー",
            UserRole.Approver => "承認者",
            UserRole.Admin => "管理者",
            _ => "不明"
        };

    private void OrderManagementButton_Click(object sender, RoutedEventArgs e)
    {
        if (_authenticatedUser is null)
        {
            return;
        }

        var window = new OrderListWindow(_orderService, _authenticatedUser);
        window.Owner = this;
        window.ShowDialog();
    }

    private void ApproverButton_Click(object sender, RoutedEventArgs e)
    {
        if (_authenticatedUser is null)
        {
            return;
        }

        var window = new ApprovalWindow(_orderService, _authenticatedUser)
        {
            Owner = this
        };
        window.ShowDialog();
    }

    private void AdminButton_Click(object sender, RoutedEventArgs e)
    {
        if (_authenticatedUser is null || !_authorizationService.CanAccess(_authenticatedUser, UserRole.Admin))
        {
            return;
        }

        var window = new BudgetSettingsWindow(_orderService)
        {
            Owner = this
        };
        window.ShowDialog();
    }

    private void ProductMasterButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new ProductMasterWindow(_productService);
        window.Owner = this;
        window.ShowDialog();
    }

    private void SupplierMasterButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new SupplierMasterWindow(_supplierService, _productService);
        window.Owner = this;
        window.ShowDialog();
    }

    private void AnalyticsButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new AnalyticsDashboardWindow(_analyticsService);
        window.Owner = this;
        window.ShowDialog();
    }
}