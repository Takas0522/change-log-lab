using System.Globalization;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using OrderClientApp.Application.Abstractions.Products;

namespace OrderClientApp.Wpf;

public partial class ProductMasterWindow : Window
{
    private readonly IProductService _productService;
    private ProductDto? _selected;

    public ProductMasterWindow(IProductService productService)
    {
        _productService = productService;
        InitializeComponent();
        Loaded += async (_, _) => await LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        var products = await _productService.ListAsync(new ProductFilter(
            CodeFilterTextBox.Text,
            NameFilterTextBox.Text,
            CategoryFilterTextBox.Text));
        ProductsDataGrid.ItemsSource = products;
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
        => await LoadProductsAsync();

    private async void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _productService.CreateAsync(ToCreateRequest());
            await LoadProductsAsync();
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
            await _productService.UpdateAsync(new UpdateProductRequest(
                _selected.Id,
                CodeTextBox.Text,
                NameTextBox.Text,
                decimal.Parse(UnitPriceTextBox.Text, CultureInfo.InvariantCulture),
                UnitTextBox.Text,
                NotesTextBox.Text,
                CategoryTextBox.Text,
                int.Parse(ReorderPointTextBox.Text, CultureInfo.InvariantCulture)));
            await LoadProductsAsync();
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

        if (MessageBox.Show("削除しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        await _productService.DeleteAsync(_selected.Id);
        await LoadProductsAsync();
        ClearEditor();
    }

    private async void ImportCsvButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV (*.csv)|*.csv|All files (*.*)|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var csv = await File.ReadAllTextAsync(dialog.FileName);
        var result = await _productService.ImportCsvAsync(csv);
        ImportResultTextBlock.Text = $"CSV取込: 全{result.TotalRows}行 / 成功{result.ImportedCount}行 / エラー{result.ErrorCount}件";
        if (result.Errors.Count > 0)
        {
            var details = string.Join(Environment.NewLine, result.Errors.Take(10).Select(x => $"{x.RowNumber}行目({x.Field}): {x.Message}"));
            MessageBox.Show(details, "取込エラー(先頭10件)", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        await LoadProductsAsync();
    }

    private void ProductsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _selected = ProductsDataGrid.SelectedItem as ProductDto;
        if (_selected is null)
        {
            return;
        }

        CodeTextBox.Text = _selected.ProductCode;
        NameTextBox.Text = _selected.Name;
        UnitPriceTextBox.Text = _selected.UnitPriceExcludingTax.ToString(CultureInfo.InvariantCulture);
        UnitTextBox.Text = _selected.Unit;
        CategoryTextBox.Text = _selected.Category;
        ReorderPointTextBox.Text = _selected.ReorderPoint.ToString(CultureInfo.InvariantCulture);
        NotesTextBox.Text = _selected.Notes ?? string.Empty;
    }

    private CreateProductRequest ToCreateRequest()
        => new(
            CodeTextBox.Text,
            NameTextBox.Text,
            decimal.Parse(UnitPriceTextBox.Text, CultureInfo.InvariantCulture),
            UnitTextBox.Text,
            NotesTextBox.Text,
            CategoryTextBox.Text,
            int.Parse(ReorderPointTextBox.Text, CultureInfo.InvariantCulture));

    private void ClearEditor()
    {
        _selected = null;
        CodeTextBox.Text = string.Empty;
        NameTextBox.Text = string.Empty;
        UnitPriceTextBox.Text = "0";
        UnitTextBox.Text = string.Empty;
        CategoryTextBox.Text = string.Empty;
        ReorderPointTextBox.Text = "0";
        NotesTextBox.Text = string.Empty;
    }
}
