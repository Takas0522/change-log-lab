using System.Globalization;
using System.Windows;
using OrderClientApp.Application.Abstractions.Products;
using OrderClientApp.Application.Abstractions.Suppliers;

namespace OrderClientApp.Wpf;

public partial class SupplierMasterWindow : Window
{
    private readonly ISupplierService _supplierService;
    private readonly IProductService _productService;
    private SupplierDto? _selected;

    public SupplierMasterWindow(ISupplierService supplierService, IProductService productService)
    {
        _supplierService = supplierService;
        _productService = productService;
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            await LoadSuppliersAsync();
            await LoadProductsAsync();
        };
    }

    private async Task LoadSuppliersAsync()
    {
        var suppliers = await _supplierService.ListAsync(KeywordTextBox.Text);
        SuppliersDataGrid.ItemsSource = suppliers;
        SupplierComboBox.ItemsSource = suppliers;
    }

    private async Task LoadProductsAsync()
    {
        var products = await _productService.ListAsync(new ProductFilter(null, null, null));
        ProductComboBox.ItemsSource = products.Select(x => new ProductSelectionItem(x.Id, $"{x.ProductCode} - {x.Name}")).ToArray();
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
        => await LoadSuppliersAsync();

    private async void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _supplierService.CreateAsync(new CreateSupplierRequest(
                CompanyNameTextBox.Text,
                ContactNameTextBox.Text,
                ContactEmailTextBox.Text,
                ContactPhoneTextBox.Text,
                NotesTextBox.Text));
            await LoadSuppliersAsync();
            ClearEditor();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "作成エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null)
        {
            return;
        }

        try
        {
            await _supplierService.UpdateAsync(new UpdateSupplierRequest(
                _selected.Id,
                CompanyNameTextBox.Text,
                ContactNameTextBox.Text,
                ContactEmailTextBox.Text,
                ContactPhoneTextBox.Text,
                NotesTextBox.Text));
            await LoadSuppliersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "更新エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null)
        {
            return;
        }

        await _supplierService.DeleteAsync(_selected.Id);
        await LoadSuppliersAsync();
        ClearEditor();
    }

    private void SuppliersDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _selected = SuppliersDataGrid.SelectedItem as SupplierDto;
        if (_selected is null)
        {
            return;
        }

        CompanyNameTextBox.Text = _selected.CompanyName;
        ContactNameTextBox.Text = _selected.ContactName ?? string.Empty;
        ContactEmailTextBox.Text = _selected.ContactEmail ?? string.Empty;
        ContactPhoneTextBox.Text = _selected.ContactPhone ?? string.Empty;
        NotesTextBox.Text = _selected.Notes ?? string.Empty;
    }

    private async void SetPriceButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProductComboBox.SelectedValue is not Guid productId || SupplierComboBox.SelectedValue is not Guid supplierId)
        {
            return;
        }

        await _supplierService.SetProductSupplierPriceAsync(
            productId,
            supplierId,
            decimal.Parse(SupplierUnitPriceTextBox.Text, CultureInfo.InvariantCulture));
        MessageBox.Show("商品別単価を設定しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void SetPreferredSupplierButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProductComboBox.SelectedValue is not Guid productId || SupplierComboBox.SelectedValue is not Guid supplierId)
        {
            return;
        }

        await _productService.SetPreferredSupplierAsync(productId, supplierId);
        MessageBox.Show("優先仕入先を設定しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ClearEditor()
    {
        _selected = null;
        CompanyNameTextBox.Text = string.Empty;
        ContactNameTextBox.Text = string.Empty;
        ContactEmailTextBox.Text = string.Empty;
        ContactPhoneTextBox.Text = string.Empty;
        NotesTextBox.Text = string.Empty;
    }

    private sealed record ProductSelectionItem(Guid Id, string DisplayName);
}
