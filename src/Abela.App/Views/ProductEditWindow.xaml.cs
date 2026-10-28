using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using Abela.Domain.Entities;

namespace Abela.App.Views;

public partial class ProductEditWindow : Window
{
    private readonly Product _product;

    public ProductEditWindow(Product product, ObservableCollection<ProductCategory> categories, ObservableCollection<Unit> units, bool isNew)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        _product = product;

        CategoryCombo.ItemsSource = categories;
        UnitCombo.ItemsSource = units;

        CodeBox.Text = product.Code;
        BarcodeBox.Text = product.Barcode;
        NameBox.Text = product.Name;
        CategoryCombo.SelectedItem = categories.FirstOrDefault(c => c.Id == product.CategoryId);
        UnitCombo.SelectedItem = units.FirstOrDefault(u => u.Id == product.UnitId);
        DescriptionBox.Text = product.Description;
        PurchasePriceBox.Text = product.PurchasePrice.ToString(CultureInfo.InvariantCulture);
        SalePriceBox.Text = product.SalePrice.ToString(CultureInfo.InvariantCulture);
        QuantityBox.Text = product.CurrentQuantity.ToString(CultureInfo.InvariantCulture);
        MinStockBox.Text = product.MinStockLevel.ToString(CultureInfo.InvariantCulture);
        BrandBox.Text = product.Brand;
        ModelBox.Text = product.ModelNumber;
        WarrantyBox.Text = product.WarrantyMonths?.ToString();
        RequiresSerialCheck.IsChecked = product.RequiresSerialNumber;
        IsActiveCheck.IsChecked = product.IsActive;
        NotesBox.Text = product.Notes;

        if (!isNew)
        {
            QuantityBox.IsEnabled = false;
            QtyLabel.Text = "الكمية الحالية (تُعدَّل عبر المخزون/الفواتير)";
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(NameBox.Text)) { ShowError("اسم المنتج مطلوب"); return; }
        if (CategoryCombo.SelectedItem is not ProductCategory category) { ShowError("الرجاء اختيار التصنيف"); return; }
        if (UnitCombo.SelectedItem is not Unit unit) { ShowError("الرجاء اختيار الوحدة"); return; }
        if (!decimal.TryParse(PurchasePriceBox.Text, out var purchasePrice) || purchasePrice < 0) { ShowError("سعر الشراء غير صحيح"); return; }
        if (!decimal.TryParse(SalePriceBox.Text, out var salePrice) || salePrice < 0) { ShowError("سعر البيع غير صحيح"); return; }
        if (!decimal.TryParse(QuantityBox.Text, out var quantity) || quantity < 0) { ShowError("الكمية غير صحيحة"); return; }
        if (!decimal.TryParse(MinStockBox.Text, out var minStock) || minStock < 0) minStock = 0;

        _product.Code = string.IsNullOrWhiteSpace(CodeBox.Text) ? _product.Code : CodeBox.Text.Trim();
        _product.Barcode = string.IsNullOrWhiteSpace(BarcodeBox.Text) ? null : BarcodeBox.Text.Trim();
        _product.Name = NameBox.Text.Trim();
        _product.CategoryId = category.Id;
        _product.UnitId = unit.Id;
        _product.Description = DescriptionBox.Text;
        _product.PurchasePrice = purchasePrice;
        _product.SalePrice = salePrice;
        if (QuantityBox.IsEnabled) _product.CurrentQuantity = quantity;
        _product.MinStockLevel = minStock;
        _product.Brand = string.IsNullOrWhiteSpace(BrandBox.Text) ? null : BrandBox.Text.Trim();
        _product.ModelNumber = string.IsNullOrWhiteSpace(ModelBox.Text) ? null : ModelBox.Text.Trim();
        _product.WarrantyMonths = int.TryParse(WarrantyBox.Text, out var w) ? w : null;
        _product.RequiresSerialNumber = RequiresSerialCheck.IsChecked == true;
        _product.IsActive = IsActiveCheck.IsChecked == true;
        _product.Notes = NotesBox.Text;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
