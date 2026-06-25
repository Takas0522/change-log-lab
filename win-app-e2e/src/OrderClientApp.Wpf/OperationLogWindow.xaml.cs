using System.Windows;
using System.Windows.Controls;
using OrderClientApp.Application.Abstractions.Operations;

namespace OrderClientApp.Wpf;

public partial class OperationLogWindow : Window
{
    private readonly IOperationLogService _operationLogService;

    public OperationLogWindow(IOperationLogService operationLogService)
    {
        _operationLogService = operationLogService;
        InitializeComponent();
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var category = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        var logs = await _operationLogService.QueryAsync(new OperationLogQuery(
            KeywordTextBox.Text,
            string.IsNullOrWhiteSpace(category) ? null : category,
            null,
            null,
            300));
        LogsDataGrid.ItemsSource = logs;
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }
}
