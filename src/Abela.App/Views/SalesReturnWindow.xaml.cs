using System.Windows;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using Abela.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Abela.App.Views;

public partial class SalesReturnLineVM : ObservableObject
{
    public int SalesInvoiceItemId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal OriginalQuantity { get; set; }
    [ObservableProperty] private decimal returnQuantity;
    [ObservableProperty] private string? reason;
}

public partial class SalesReturnWindow : Window
{
    private readonly SalesInvoice _invoice;
    private readonly SalesReturnService _returnService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;
    private readonly List<SalesReturnLineVM> _lines;

    public SalesReturnWindow(SalesInvoice invoice, SalesReturnService returnService, ICurrentUserService currentUser, IDialogService dialog)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        _invoice = invoice;
        _returnService = returnService;
        _currentUser = currentUser;
        _dialog = dialog;

        HeaderText.Text = $"مرتجع للفاتورة {invoice.InvoiceNumber} - العميل: {invoice.Customer.Name}";
        _lines = invoice.Items.Select(i => new SalesReturnLineVM
        {
            SalesInvoiceItemId = i.Id,
            ProductName = i.Product.Name,
            OriginalQuantity = i.Quantity
        }).ToList();
        LinesGrid.ItemsSource = _lines;

        RefundMethodCombo.ItemsSource = Enum.GetValues<RefundMethod>();
        RefundMethodCombo.SelectedItem = RefundMethod.CashRefund;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        var toReturn = _lines.Where(l => l.ReturnQuantity > 0).ToList();
        if (toReturn.Count == 0) { ErrorText.Text = "الرجاء إدخال كمية إرجاع لصنف واحد على الأقل"; return; }
        if (RefundMethodCombo.SelectedItem is not RefundMethod method) return;

        var lineInputs = toReturn.Select(l => new SalesReturnLineInput { SalesInvoiceItemId = l.SalesInvoiceItemId, Quantity = l.ReturnQuantity, Reason = l.Reason }).ToList();
        var result = await _returnService.CreateAsync(_invoice.Id, method, lineInputs, null, _currentUser.UserId);
        if (!result.Success) { ErrorText.Text = result.Message; return; }

        _dialog.ShowSuccess(result.Message);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
