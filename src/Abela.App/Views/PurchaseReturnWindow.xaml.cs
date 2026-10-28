using System.Windows;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Abela.App.Views;

public partial class PurchaseReturnLineVM : ObservableObject
{
    public int PurchaseInvoiceItemId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal OriginalQuantity { get; set; }
    [ObservableProperty] private decimal returnQuantity;
    [ObservableProperty] private string? reason;
}

public partial class PurchaseReturnWindow : Window
{
    private readonly PurchaseInvoice _invoice;
    private readonly PurchaseReturnService _returnService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;
    private readonly List<PurchaseReturnLineVM> _lines;

    public PurchaseReturnWindow(PurchaseInvoice invoice, PurchaseReturnService returnService, ICurrentUserService currentUser, IDialogService dialog)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        _invoice = invoice;
        _returnService = returnService;
        _currentUser = currentUser;
        _dialog = dialog;

        HeaderText.Text = $"مرتجع لفاتورة الشراء {invoice.InvoiceNumber} - المورد: {invoice.Supplier.Name}";
        _lines = invoice.Items.Select(i => new PurchaseReturnLineVM
        {
            PurchaseInvoiceItemId = i.Id,
            ProductName = i.Product.Name,
            OriginalQuantity = i.Quantity
        }).ToList();
        LinesGrid.ItemsSource = _lines;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        var toReturn = _lines.Where(l => l.ReturnQuantity > 0).ToList();
        if (toReturn.Count == 0) { ErrorText.Text = "الرجاء إدخال كمية إرجاع لصنف واحد على الأقل"; return; }

        var lineInputs = toReturn.Select(l => new PurchaseReturnLineInput { PurchaseInvoiceItemId = l.PurchaseInvoiceItemId, Quantity = l.ReturnQuantity, Reason = l.Reason }).ToList();
        var result = await _returnService.CreateAsync(_invoice.Id, lineInputs, null, _currentUser.UserId);
        if (!result.Success) { ErrorText.Text = result.Message; return; }

        _dialog.ShowSuccess(result.Message);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
