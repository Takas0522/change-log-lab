using System.Windows;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Application.Abstractions.Auth;
using OrderClientApp.Domain.Auth;

namespace OrderClientApp.Wpf;

public partial class MainWindow : Window
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IOrderService _orderService;
    private AuthenticatedUser? _authenticatedUser;

    public MainWindow(
        IAuthenticationService authenticationService,
        IAuthorizationService authorizationService,
        IOrderService orderService)
    {
        _authenticationService = authenticationService;
        _authorizationService = authorizationService;
        _orderService = orderService;
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
        ShowDashboard();
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

    private void ShowDashboard()
    {
        if (_authenticatedUser is null)
        {
            return;
        }

        LoginView.Visibility = Visibility.Collapsed;
        DashboardView.Visibility = Visibility.Visible;

        HeaderTextBlock.Text = $"ダッシュボード - {_authenticatedUser.Username} ({GetRoleLabel(_authenticatedUser.Role)})";
        DashboardInfoTextBlock.Text = "ログインに成功しました。発注管理ボタンから一覧・詳細画面へ遷移できます。";

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
}