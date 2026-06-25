using System.Windows;
using Microsoft.VisualBasic;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Domain.Auth;

namespace OrderClientApp.Wpf;

public partial class ApprovalWindow : Window
{
    private readonly IOrderService _orderService;
    private readonly AuthenticatedUser _actor;

    public ApprovalWindow(IOrderService orderService, AuthenticatedUser actor)
    {
        _orderService = orderService;
        _actor = actor;
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _orderService.ListPendingApprovalsAsync();
        PendingDataGrid.ItemsSource = items.Select(x => new Row(x.OrderId, x.OrderNumber, x.SupplierName, x.OrderedAtUtc.ToLocalTime().ToString("yyyy/MM/dd"), x.AmountIncludingTax.ToString("N2"))).ToArray();
    }

    private async void ApproveButton_Click(object sender, RoutedEventArgs e)
    {
        if (PendingDataGrid.SelectedItem is not Row row)
        {
            return;
        }

        await _orderService.ApproveAsync(row.Id, _actor);
        await LoadAsync();
    }

    private async void RejectButton_Click(object sender, RoutedEventArgs e)
    {
        if (PendingDataGrid.SelectedItem is not Row row)
        {
            return;
        }

        var reason = Interaction.InputBox("却下理由を入力してください。", "却下理由");
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        await _orderService.RejectAsync(row.Id, reason, _actor);
        await LoadAsync();
    }

    private sealed record Row(Guid Id, string OrderNumber, string SupplierName, string OrderedAt, string Amount);
}
