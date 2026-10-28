using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Abela.App.Common;
using Abela.App.ViewModels;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using Abela.Domain.Enums;

namespace Abela.App.Views;

public partial class SalesInvoiceWindow : Window
{
    private readonly SalesService _salesService;
    private readonly CustomerService _customerService;
    private readonly ProductService _productService;
    private readonly SettingsService _settingsService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;
    private readonly ObservableCollection<SalesLineVM> _lines = new();
    private Customer? _defaultCashCustomer;

    public SalesInvoiceWindow(SalesService salesService, CustomerService customerService, ProductService productService,
        SettingsService settingsService, ICurrentUserService currentUser, IDialogService dialog)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        _salesService = salesService;
        _customerService = customerService;
        _productService = productService;
        _settingsService = settingsService;
        _currentUser = currentUser;
        _dialog = dialog;

        LinesGrid.ItemsSource = _lines;
        _lines.CollectionChanged += (_, __) => RecalculateTotals();

        SaleTypeCombo.ItemsSource = Enum.GetValues<SaleType>();
        SaleTypeCombo.SelectedItem = SaleType.Cash;
        PaymentMethodCombo.ItemsSource = Enum.GetValues<PaymentMethod>();
        PaymentMethodCombo.SelectedItem = PaymentMethod.Cash;

        _ = LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        var customers = await _customerService.SearchAsync();
        _defaultCashCustomer = customers.FirstOrDefault(c => c.IsDefault);
        CustomerCombo.ItemsSource = customers.Where(c => !c.IsDefault).ToList();
    }

    private void CustomerTypeRadio_Changed(object sender, RoutedEventArgs e)
    {
        if (WalkInNamePanel is null || RegisteredCustomerPanel is null) return; // fires during InitializeComponent before both panels exist
        var isWalkIn = WalkInCustomerRadio.IsChecked == true;
        WalkInNamePanel.Visibility = isWalkIn ? Visibility.Visible : Visibility.Collapsed;
        RegisteredCustomerPanel.Visibility = isWalkIn ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void AddLine_Click(object sender, RoutedEventArgs e)
    {
        var picker = new ProductPickerWindow(_productService) { Owner = this };
        if (picker.ShowDialog() != true || picker.SelectedProduct is null) return;

        var product = picker.SelectedProduct;
        var line = new SalesLineVM { Product = product, Quantity = 1, UnitPrice = product.SalePrice };
        line.PropertyChanged += Line_PropertyChanged;

        if (product.RequiresSerialNumber)
        {
            var full = await _productService.GetByIdAsync(product.Id);
            var available = full?.SerialNumbers.Where(s => s.Status == SerialStatus.InStock).Select(s => s.SerialNumber).ToList() ?? new List<string>();
            if (available.Count == 0) { _dialog.ShowError("لا توجد أرقام تسلسلية متوفرة لهذا المنتج"); return; }

            var serialWindow = new SerialEntryWindow(product.Name, 1, available) { Owner = this };
            if (serialWindow.ShowDialog() != true) return;
            line.SerialNumbers = serialWindow.SelectedSerials;
        }

        _lines.Add(line);
        RecalculateTotals();
    }

    private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e) => RecalculateTotals();

    private void RemoveLine_Click(object sender, RoutedEventArgs e)
    {
        if (LinesGrid.SelectedItem is SalesLineVM line)
        {
            _lines.Remove(line);
            RecalculateTotals();
        }
    }

    private void Totals_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e) => RecalculateTotals();

    private void RecalculateTotals()
    {
        if (SubTotalText is null || TotalText is null || RemainingText is null) return; // fires from TextChanged during InitializeComponent, before later-declared controls exist
        var subTotal = _lines.Sum(l => l.Quantity * l.UnitPrice);
        decimal.TryParse(OverallDiscountBox.Text, out var discount);
        var total = subTotal - discount;
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

        if (_lines.Count == 0) { ErrorText.Text = "الرجاء إضافة صنف واحد على الأقل"; return; }
        if (SaleTypeCombo.SelectedItem is not SaleType saleType) return;
        if (PaymentMethodCombo.SelectedItem is not PaymentMethod method) return;

        decimal.TryParse(OverallDiscountBox.Text, out var discount);
        decimal.TryParse(PaidAmountBox.Text, out var paid);

        var subTotal = _lines.Sum(l => l.Quantity * l.UnitPrice);
        var total = Math.Max(0, subTotal - discount);
        var remaining = total - paid;

        Customer customer;
        string? notes = NotesBox.Text;
        if (WalkInCustomerRadio.IsChecked == true)
        {
            if (remaining > 0)
            {
                ErrorText.Text = "يوجد مبلغ متبقٍ على الفاتورة، يجب اختيار عميل مسجل لتسجيل الدين عليه";
                return;
            }
            if (_defaultCashCustomer is null) { ErrorText.Text = "تعذر العثور على العميل النقدي الافتراضي"; return; }
            customer = _defaultCashCustomer;
            if (!string.IsNullOrWhiteSpace(WalkInNameBox.Text))
                notes = $"الزبون: {WalkInNameBox.Text.Trim()}" + (string.IsNullOrWhiteSpace(notes) ? "" : $"\n{notes}");
        }
        else
        {
            if (CustomerCombo.SelectedItem is not Customer selected) { ErrorText.Text = "الرجاء اختيار العميل المسجل"; return; }
            customer = selected;
        }

        var lineInputs = _lines.Select(l => new SalesLineInput
        {
            ProductId = l.Product.Id,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            DiscountAmount = l.DiscountAmount,
            TaxAmount = 0,
            SerialNumbers = l.SerialNumbers
        }).ToList();

        var result = await _salesService.CreateAsync(customer.Id, saleType, method, discount, paid, notes, lineInputs, _currentUser.UserId);
        if (!result.Success) { ErrorText.Text = result.Message; return; }

        _dialog.ShowSuccess(result.Message);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
