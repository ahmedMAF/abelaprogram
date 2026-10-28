using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Abela.App.ViewModels;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using Abela.Domain.Enums;

namespace Abela.App.Views;

public partial class PurchaseInvoiceWindow : Window
{
    private readonly PurchaseService _purchaseService;
    private readonly SupplierService _supplierService;
    private readonly ProductService _productService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;
    private readonly ObservableCollection<PurchaseLineVM> _lines = new();

    public PurchaseInvoiceWindow(PurchaseService purchaseService, SupplierService supplierService, ProductService productService,
        ICurrentUserService currentUser, IDialogService dialog)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        _purchaseService = purchaseService;
        _supplierService = supplierService;
        _productService = productService;
        _currentUser = currentUser;
        _dialog = dialog;

        LinesGrid.ItemsSource = _lines;
        _lines.CollectionChanged += (_, __) => RecalculateTotals();

        PaymentMethodCombo.ItemsSource = Enum.GetValues<PaymentMethod>();
        PaymentMethodCombo.SelectedItem = PaymentMethod.Cash;

        _ = LoadSuppliersAsync();
    }

    private async Task LoadSuppliersAsync()
    {
        SupplierCombo.ItemsSource = await _supplierService.SearchAsync();
    }

    private async void AddLine_Click(object sender, RoutedEventArgs e)
    {
        var picker = new ProductPickerWindow(_productService) { Owner = this };
        if (picker.ShowDialog() != true || picker.SelectedProduct is null) return;

        var product = picker.SelectedProduct;
        var line = new PurchaseLineVM { Product = product, Quantity = 1, UnitCost = product.PurchasePrice };
        line.PropertyChanged += Line_PropertyChanged;

        if (product.RequiresSerialNumber)
        {
            var qtyText = InputDialog.Show("عدد القطع", $"كم عدد القطع المشتراة من {product.Name}؟ (سيتم طلب رقم تسلسلي لكل قطعة)");
            if (!int.TryParse(qtyText, out var qty) || qty <= 0) { _dialog.ShowError("عدد غير صحيح"); return; }

            var serialWindow = new SerialEntryWindow(product.Name, qty) { Owner = this };
            if (serialWindow.ShowDialog() != true) return;
            line.Quantity = qty;
            line.SerialNumbers = serialWindow.SelectedSerials;
        }

        _lines.Add(line);
        RecalculateTotals();
    }

    private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e) => RecalculateTotals();

    private void RemoveLine_Click(object sender, RoutedEventArgs e)
    {
        if (LinesGrid.SelectedItem is PurchaseLineVM line)
        {
            _lines.Remove(line);
            RecalculateTotals();
        }
    }

    private void Totals_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e) => RecalculateTotals();

    private void RecalculateTotals()
    {
        if (SubTotalText is null || TotalText is null || RemainingText is null) return; // fires from TextChanged during InitializeComponent, before later-declared controls exist
        var subTotal = _lines.Sum(l => l.Quantity * l.UnitCost);
        decimal.TryParse(OverallDiscountBox.Text, out var discount);
        decimal.TryParse(ExtraCostsBox.Text, out var extra);
        var total = subTotal - discount + extra;
        if (total < 0) total = 0;
        decimal.TryParse(PaidAmountBox.Text, out var paid);
        var remaining = total - paid;

        SubTotalText.Text = subTotal.ToString("N2");
        TotalText.Text = total.ToString("N2");
        RemainingText.Text = remaining.ToString("N2");
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;

        if (SupplierCombo.SelectedItem is not Supplier supplier) { ErrorText.Text = "الرجاء اختيار المورد"; return; }
        if (_lines.Count == 0) { ErrorText.Text = "الرجاء إضافة صنف واحد على الأقل"; return; }
        if (PaymentMethodCombo.SelectedItem is not PaymentMethod method) return;

        decimal.TryParse(OverallDiscountBox.Text, out var discount);
        decimal.TryParse(ExtraCostsBox.Text, out var extra);
        decimal.TryParse(PaidAmountBox.Text, out var paid);

        var lineInputs = _lines.Select(l => new PurchaseLineInput
        {
            ProductId = l.Product.Id,
            Quantity = l.Quantity,
            UnitCost = l.UnitCost,
            DiscountAmount = l.DiscountAmount,
            TaxAmount = 0,
            SerialNumbers = l.SerialNumbers
        }).ToList();

        var result = await _purchaseService.CreateAsync(supplier.Id, SupplierInvoiceNumberBox.Text, method, discount, extra, paid, NotesBox.Text, lineInputs, _currentUser.UserId);
        if (!result.Success) { ErrorText.Text = result.Message; return; }

        _dialog.ShowSuccess(result.Message);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
