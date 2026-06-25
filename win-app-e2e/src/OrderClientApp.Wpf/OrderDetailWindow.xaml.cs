using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Specialized;
using OrderClientApp.Application.Abstractions.Orders;
using OrderClientApp.Domain.Auth;
using OrderClientApp.Domain.Orders;
using MessageBox = System.Windows.MessageBox;

namespace OrderClientApp.Wpf;

public partial class OrderDetailWindow : Window
{
    private readonly IOrderService _orderService;
    private readonly AuthenticatedUser _authenticatedUser;
    private readonly Guid? _orderId;
    private readonly ObservableCollection<LineItemInputViewModel> _lineItems = [];
    private readonly ObservableCollection<OrderTemplateDto> _templates = [];

    public OrderDetailWindow(
        IOrderService orderService,
        AuthenticatedUser authenticatedUser,
        Guid? orderId)
    {
        _orderService = orderService;
        _authenticatedUser = authenticatedUser;
        _orderId = orderId;
        InitializeComponent();

        StatusComboBox.ItemsSource = new[]
        {
            new StatusSelectionItem("未処理", OrderStatus.Unprocessed),
            new StatusSelectionItem("申請中", OrderStatus.PendingApproval),
            new StatusSelectionItem("承認済み", OrderStatus.Approved),
            new StatusSelectionItem("却下", OrderStatus.Rejected),
            new StatusSelectionItem("処理中", OrderStatus.Processing),
            new StatusSelectionItem("入荷待ち", OrderStatus.WaitingForArrival),
            new StatusSelectionItem("部分入荷", OrderStatus.PartiallyReceived),
            new StatusSelectionItem("完了", OrderStatus.Completed),
            new StatusSelectionItem("キャンセル", OrderStatus.Canceled)
        };
        StatusComboBox.SelectedIndex = 0;

        LineItemsDataGrid.ItemsSource = _lineItems;
        TemplateComboBox.ItemsSource = _templates;
        _lineItems.CollectionChanged += LineItems_CollectionChanged;

        CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, async (_, _) => await SaveAsync()));
        InputBindings.Add(new KeyBinding(ApplicationCommands.Save, new KeyGesture(Key.S, ModifierKeys.Control)));

        Loaded += async (_, _) =>
        {
            await LoadTemplatesAsync();
            if (_orderId.HasValue)
            {
                await LoadOrderAsync(_orderId.Value);
            }
            else
            {
                OrderedAtDatePicker.SelectedDate = DateTime.Today;
                _lineItems.Add(new LineItemInputViewModel());
                UpdateAmounts();
            }
        };
    }

    private async Task LoadOrderAsync(Guid orderId)
    {
        var order = await _orderService.GetByIdAsync(orderId, includeDeleted: true);
        if (order is null)
        {
            MessageBox.Show("発注が見つかりません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        OrderNumberTextBox.Text = order.OrderNumber;
        SupplierTextBox.Text = order.SupplierName;
        OrderedAtDatePicker.SelectedDate = order.OrderedAtUtc.LocalDateTime.Date;
        ExpectedReceivingDatePicker.SelectedDate = order.ExpectedReceivingDateUtc?.LocalDateTime.Date;
        StatusComboBox.SelectedValue = order.Status;
        TaxRateTextBox.Text = order.TaxRate.ToString(CultureInfo.InvariantCulture);
        NoteTextBox.Text = order.Note ?? string.Empty;
        DeliveryNoteNumberTextBox.Text = order.DeliveryNoteNumber ?? string.Empty;
        DeliveryNoteDatePicker.SelectedDate = order.DeliveryNoteDateUtc?.LocalDateTime.Date;
        InvoiceNumberTextBox.Text = order.InvoiceNumber ?? string.Empty;
        InvoiceDatePicker.SelectedDate = order.InvoiceDateUtc?.LocalDateTime.Date;
        BudgetAlertTextBlock.Text = order.BudgetExceeded
            ? "予算超過アラート"
            : order.RequiresApproval ? "承認が必要です" : string.Empty;

        _lineItems.Clear();
        foreach (var lineItem in order.LineItems)
        {
            _lineItems.Add(new LineItemInputViewModel
            {
                ProductCode = lineItem.ProductCode,
                ProductName = lineItem.ProductName,
                Quantity = lineItem.Quantity,
                UnitPriceExcludingTax = lineItem.UnitPriceExcludingTax
            });
        }

        UpdateAmounts();
    }

    private async Task LoadTemplatesAsync()
    {
        _templates.Clear();
        foreach (var template in await _orderService.ListTemplatesAsync())
        {
            _templates.Add(template);
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            var orderedAt = OrderedAtDatePicker.SelectedDate ?? DateTime.Today;
            var deliveryNoteDate = DeliveryNoteDatePicker.SelectedDate;
            var invoiceDate = InvoiceDatePicker.SelectedDate;
            var taxRate = decimal.Parse(TaxRateTextBox.Text, CultureInfo.InvariantCulture);
            var lineItems = _lineItems
                .Where(x => !string.IsNullOrWhiteSpace(x.ProductCode) && !string.IsNullOrWhiteSpace(x.ProductName))
                .Select(x => new CreateOrderLineItemInput(
                    x.ProductCode,
                    x.ProductName,
                    x.Quantity,
                    x.UnitPriceExcludingTax))
                .ToArray();

            if (_orderId.HasValue)
            {
                await _orderService.UpdateAsync(new UpdateOrderRequest(
                    _orderId.Value,
                    SupplierTextBox.Text,
                    new DateTimeOffset(orderedAt, TimeSpan.Zero),
                    ExpectedReceivingDatePicker.SelectedDate.HasValue
                        ? new DateTimeOffset(ExpectedReceivingDatePicker.SelectedDate.Value, TimeSpan.Zero)
                        : null,
                    (OrderStatus)(StatusComboBox.SelectedValue ?? OrderStatus.Unprocessed),
                    NoteTextBox.Text,
                    DeliveryNoteNumberTextBox.Text,
                    deliveryNoteDate.HasValue ? new DateTimeOffset(deliveryNoteDate.Value, TimeSpan.Zero) : null,
                    InvoiceNumberTextBox.Text,
                    invoiceDate.HasValue ? new DateTimeOffset(invoiceDate.Value, TimeSpan.Zero) : null,
                    taxRate,
                    lineItems));
            }
            else if (lineItems.Length > 1)
            {
                var created = await _orderService.CreateBulkAsync(new CreateBulkOrderRequest(
                    _authenticatedUser.UserId,
                    SupplierTextBox.Text,
                    new DateTimeOffset(orderedAt, TimeSpan.Zero),
                    ExpectedReceivingDatePicker.SelectedDate.HasValue
                        ? new DateTimeOffset(ExpectedReceivingDatePicker.SelectedDate.Value, TimeSpan.Zero)
                        : null,
                    NoteTextBox.Text,
                    DeliveryNoteNumberTextBox.Text,
                    deliveryNoteDate.HasValue ? new DateTimeOffset(deliveryNoteDate.Value, TimeSpan.Zero) : null,
                    InvoiceNumberTextBox.Text,
                    invoiceDate.HasValue ? new DateTimeOffset(invoiceDate.Value, TimeSpan.Zero) : null,
                    taxRate,
                    lineItems));
                OrderNumberTextBox.Text = created.OrderNumber;
            }
            else
            {
                var created = await _orderService.CreateAsync(new CreateOrderRequest(
                    _authenticatedUser.UserId,
                    SupplierTextBox.Text,
                    new DateTimeOffset(orderedAt, TimeSpan.Zero),
                    ExpectedReceivingDatePicker.SelectedDate.HasValue
                        ? new DateTimeOffset(ExpectedReceivingDatePicker.SelectedDate.Value, TimeSpan.Zero)
                        : null,
                    NoteTextBox.Text,
                    DeliveryNoteNumberTextBox.Text,
                    deliveryNoteDate.HasValue ? new DateTimeOffset(deliveryNoteDate.Value, TimeSpan.Zero) : null,
                    InvoiceNumberTextBox.Text,
                    invoiceDate.HasValue ? new DateTimeOffset(invoiceDate.Value, TimeSpan.Zero) : null,
                    taxRate,
                    lineItems));
                OrderNumberTextBox.Text = created.OrderNumber;
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存に失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddLineButton_Click(object sender, RoutedEventArgs e)
    {
        _lineItems.Add(new LineItemInputViewModel());
        UpdateAmounts();
    }

    private void RemoveLineButton_Click(object sender, RoutedEventArgs e)
    {
        if (LineItemsDataGrid.SelectedItem is not LineItemInputViewModel item)
        {
            return;
        }

        _lineItems.Remove(item);
        UpdateAmounts();
    }

    private async void SaveTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var taxRate = decimal.Parse(TaxRateTextBox.Text, CultureInfo.InvariantCulture);
            var lineItems = _lineItems
                .Where(x => !string.IsNullOrWhiteSpace(x.ProductCode) && !string.IsNullOrWhiteSpace(x.ProductName))
                .Select(x => new CreateOrderLineItemInput(
                    x.ProductCode,
                    x.ProductName,
                    x.Quantity,
                    x.UnitPriceExcludingTax))
                .ToArray();

            await _orderService.SaveTemplateAsync(new SaveOrderTemplateRequest(
                _authenticatedUser.UserId,
                TemplateNameTextBox.Text,
                NoteTextBox.Text,
                taxRate,
                lineItems));

            await LoadTemplatesAsync();
            MessageBox.Show("テンプレートを保存しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"テンプレート保存に失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        if (TemplateComboBox.SelectedValue is not Guid templateId)
        {
            return;
        }

        var template = await _orderService.GetTemplateByIdAsync(templateId);
        if (template is null)
        {
            return;
        }

        TaxRateTextBox.Text = template.TaxRate.ToString(CultureInfo.InvariantCulture);
        NoteTextBox.Text = template.Note ?? string.Empty;
        _lineItems.Clear();
        foreach (var lineItem in template.LineItems)
        {
            _lineItems.Add(new LineItemInputViewModel
            {
                ProductCode = lineItem.ProductCode,
                ProductName = lineItem.ProductName,
                Quantity = lineItem.Quantity,
                UnitPriceExcludingTax = lineItem.UnitPriceExcludingTax
            });
        }

        UpdateAmounts();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
        => await SaveAsync();

    private async void ReceiveAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_orderId.HasValue)
        {
            return;
        }

        var order = await _orderService.GetByIdAsync(_orderId.Value, includeDeleted: true);
        if (order is null)
        {
            return;
        }

        var lines = order.LineItems
            .Where(x => x.RemainingQuantity > 0)
            .Select(x => new ConfirmReceivingLineInput(x.Id, x.RemainingQuantity))
            .ToArray();
        if (lines.Length == 0)
        {
            MessageBox.Show("入荷対象がありません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await _orderService.ConfirmReceivingAsync(new ConfirmReceivingRequest(order.Id, lines));
        MessageBox.Show("入荷確認を登録しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
        await LoadOrderAsync(order.Id);
    }

    private void UpdateAmounts()
    {
        var taxRate = decimal.TryParse(TaxRateTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedTaxRate)
            ? parsedTaxRate
            : 0.1m;
        var amountExcludingTax = _lineItems.Sum(x => x.AmountExcludingTax);
        var amountIncludingTax = decimal.Round(amountExcludingTax * (1 + taxRate), 2, MidpointRounding.AwayFromZero);
        AmountTextBlock.Text = $"税抜合計: {amountExcludingTax:N2} / 税込合計: {amountIncludingTax:N2}";
    }

    private void OnLineItemChanged(object? sender, PropertyChangedEventArgs e)
        => UpdateAmounts();

    private void LineItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (var newItem in e.NewItems.OfType<LineItemInputViewModel>())
            {
                newItem.PropertyChanged += OnLineItemChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (var oldItem in e.OldItems.OfType<LineItemInputViewModel>())
            {
                oldItem.PropertyChanged -= OnLineItemChanged;
            }
        }

        UpdateAmounts();
    }

    private sealed record StatusSelectionItem(string Label, OrderStatus Value);

    private sealed class LineItemInputViewModel : INotifyPropertyChanged
    {
        private string _productCode = string.Empty;
        private string _productName = string.Empty;
        private int _quantity = 1;
        private decimal _unitPriceExcludingTax;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string ProductCode
        {
            get => _productCode;
            set
            {
                _productCode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AmountExcludingTax));
            }
        }

        public string ProductName
        {
            get => _productName;
            set
            {
                _productName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AmountExcludingTax));
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value <= 0 ? 1 : value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AmountExcludingTax));
            }
        }

        public decimal UnitPriceExcludingTax
        {
            get => _unitPriceExcludingTax;
            set
            {
                _unitPriceExcludingTax = value < 0 ? 0 : value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AmountExcludingTax));
            }
        }

        public decimal AmountExcludingTax => Quantity * UnitPriceExcludingTax;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
