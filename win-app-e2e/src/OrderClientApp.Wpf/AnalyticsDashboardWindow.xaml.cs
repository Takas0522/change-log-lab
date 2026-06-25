using System.Windows;
using OrderClientApp.Application.Abstractions.Analytics;

namespace OrderClientApp.Wpf;

public partial class AnalyticsDashboardWindow : Window
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsDashboardWindow(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            FromDatePicker.SelectedDate = DateTime.Today.AddMonths(-6);
            ToDatePicker.SelectedDate = DateTime.Today;
            await LoadAsync();
        };
    }

    private async Task LoadAsync()
    {
        var from = new DateTimeOffset(FromDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-6), TimeSpan.Zero);
        var to = new DateTimeOffset((ToDatePicker.SelectedDate ?? DateTime.Today).AddDays(1).AddTicks(-1), TimeSpan.Zero);
        var dashboard = await _analyticsService.GetDashboardAsync(from, to);
        MonthlyDataGrid.ItemsSource = dashboard.Monthly;
        ProductDataGrid.ItemsSource = dashboard.ProductWise;
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        => await LoadAsync();
}
