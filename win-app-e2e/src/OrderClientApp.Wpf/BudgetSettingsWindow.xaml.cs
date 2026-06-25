using System.Globalization;
using System.Windows;
using OrderClientApp.Application.Abstractions.Orders;
using MessageBox = System.Windows.MessageBox;

namespace OrderClientApp.Wpf;

public partial class BudgetSettingsWindow : Window
{
    private readonly IOrderService _orderService;

    public BudgetSettingsWindow(IOrderService orderService)
    {
        _orderService = orderService;
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var settings = await _orderService.GetBudgetSettingsAsync();
        ApprovalThresholdTextBox.Text = settings.ApprovalThreshold.ToString(CultureInfo.InvariantCulture);
        MonthlyLimitTextBox.Text = settings.MonthlyLimit?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        YearlyLimitTextBox.Text = settings.YearlyLimit?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        InfoTextBlock.Text = $"最終更新: {settings.UpdatedAtUtc.LocalDateTime:yyyy/MM/dd HH:mm}";
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var threshold = decimal.Parse(ApprovalThresholdTextBox.Text, CultureInfo.InvariantCulture);
            var monthly = string.IsNullOrWhiteSpace(MonthlyLimitTextBox.Text)
                ? (decimal?)null
                : decimal.Parse(MonthlyLimitTextBox.Text, CultureInfo.InvariantCulture);
            var yearly = string.IsNullOrWhiteSpace(YearlyLimitTextBox.Text)
                ? (decimal?)null
                : decimal.Parse(YearlyLimitTextBox.Text, CultureInfo.InvariantCulture);

            await _orderService.SaveBudgetSettingsAsync(threshold, monthly, yearly);
            await LoadAsync();
            MessageBox.Show("予算設定を保存しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存に失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
