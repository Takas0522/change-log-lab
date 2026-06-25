using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Domain.Auth;
using OrderClientApp.Domain.Orders;

namespace OrderClientApp.Wpf;

public partial class OrderListWindow : Window
{
    private readonly IOrderService _orderService;
    private readonly AuthenticatedUser _authenticatedUser;
    private int _pageNumber = 1;
    private int _totalCount;

    public OrderListWindow(IOrderService orderService, AuthenticatedUser authenticatedUser)
    {
        _orderService = orderService;
        _authenticatedUser = authenticatedUser;
        InitializeComponent();

        StatusFilterComboBox.ItemsSource = new[]
        {
            new StatusFilterItem("すべて", null),
            new StatusFilterItem("未処理", OrderStatus.Unprocessed),
            new StatusFilterItem("処理中", OrderStatus.Processing),
            new StatusFilterItem("入荷待ち", OrderStatus.WaitingForArrival),
            new StatusFilterItem("部分入荷", OrderStatus.PartiallyReceived),
            new StatusFilterItem("完了", OrderStatus.Completed),
            new StatusFilterItem("キャンセル", OrderStatus.Canceled)
        };
        StatusFilterComboBox.SelectedIndex = 0;
        PageSizeComboBox.SelectedIndex = 0;

        CommandBindings.Add(new CommandBinding(ApplicationCommands.New, (_, _) => OpenOrderDetail(null)));
        InputBindings.Add(new KeyBinding(ApplicationCommands.New, new KeyGesture(Key.N, ModifierKeys.Control)));

        Loaded += async (_, _) => await LoadOrdersAsync();
    }

    private async Task LoadOrdersAsync()
    {
        var pageSize = GetPageSize();
        var status = (OrderStatus?)StatusFilterComboBox.SelectedValue;
        DateTimeOffset? from = FromDatePicker.SelectedDate.HasValue
            ? new DateTimeOffset(FromDatePicker.SelectedDate.Value, TimeSpan.Zero)
            : null;
        DateTimeOffset? to = ToDatePicker.SelectedDate.HasValue
            ? new DateTimeOffset(ToDatePicker.SelectedDate.Value.AddDays(1).AddTicks(-1), TimeSpan.Zero)
            : null;

        var result = await _orderService.ListAsync(
            new OrderListQuery(status, from, to, _pageNumber, pageSize));

        _totalCount = result.TotalCount;
        var items = result.Items.Select(x => new OrderRowViewModel(
            x.Id,
            x.OrderNumber,
            x.SupplierName,
            x.OrderedAtUtc.ToLocalTime().ToString("yyyy/MM/dd"),
            x.Status.ToJapaneseLabel(),
            x.AmountExcludingTax.ToString("N2"),
            x.AmountIncludingTax.ToString("N2"))).ToArray();

        OrdersDataGrid.ItemsSource = items;
        var maxPage = Math.Max(1, (int)Math.Ceiling((double)_totalCount / pageSize));
        PagerInfoTextBlock.Text = $"ページ {_pageNumber}/{maxPage} - 全 {_totalCount} 件";
        PreviousPageButton.IsEnabled = _pageNumber > 1;
        NextPageButton.IsEnabled = _pageNumber < maxPage;
    }

    private int GetPageSize()
    {
        return (PageSizeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "50" => 50,
            "100" => 100,
            _ => 10
        };
    }

    private void OpenOrderDetail(Guid? orderId)
    {
        var window = new OrderDetailWindow(_orderService, _authenticatedUser, orderId);
        window.Owner = this;
        if (window.ShowDialog() == true)
        {
            _ = LoadOrdersAsync();
        }
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        _pageNumber = 1;
        _ = LoadOrdersAsync();
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
        => OpenOrderDetail(null);

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (OrdersDataGrid.SelectedItem is OrderRowViewModel row)
        {
            OpenOrderDetail(row.Id);
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (OrdersDataGrid.SelectedItem is not OrderRowViewModel row)
        {
            return;
        }

        if (MessageBox.Show(
                $"{row.OrderNumber} を論理削除しますか？",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        await _orderService.SoftDeleteAsync(row.Id);
        await LoadOrdersAsync();
    }

    private async void SaveTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        if (OrdersDataGrid.SelectedItem is not OrderRowViewModel row)
        {
            return;
        }

        var order = await _orderService.GetByIdAsync(row.Id);
        if (order is null)
        {
            return;
        }

        var request = new SaveOrderTemplateRequest(
            _authenticatedUser.UserId,
            $"TPL-{order.OrderNumber}",
            order.Note,
            order.TaxRate,
            order.LineItems.Select(x => new CreateOrderLineItemInput(
                x.ProductCode,
                x.ProductName,
                x.Quantity,
                x.UnitPriceExcludingTax)).ToArray());

        await _orderService.SaveTemplateAsync(request);
        MessageBox.Show("テンプレートを保存しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void FiltersChanged(object sender, RoutedEventArgs e)
    {
        _pageNumber = 1;
    }

    private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pageNumber <= 1)
        {
            return;
        }

        _pageNumber--;
        _ = LoadOrdersAsync();
    }

    private void NextPageButton_Click(object sender, RoutedEventArgs e)
    {
        var maxPage = Math.Max(1, (int)Math.Ceiling((double)_totalCount / GetPageSize()));
        if (_pageNumber >= maxPage)
        {
            return;
        }

        _pageNumber++;
        _ = LoadOrdersAsync();
    }

    private sealed record StatusFilterItem(string Label, OrderStatus? Value);

    private sealed record OrderRowViewModel(
        Guid Id,
        string OrderNumber,
        string SupplierName,
        string OrderedAt,
        string StatusLabel,
        string AmountExcludingTax,
        string AmountIncludingTax);
}
