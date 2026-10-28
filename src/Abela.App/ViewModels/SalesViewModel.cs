using Abela.App.Common;
using System.IO;
using System.Collections.ObjectModel;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using Abela.Domain.Enums;
using Abela.Infrastructure.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Abela.App.ViewModels;

public partial class SalesViewModel : ObservableObject
{
    private readonly SalesService _salesService;
    private readonly CustomerService _customerService;
    private readonly ProductService _productService;
    private readonly SettingsService _settingsService;
    private readonly InvoicePdfService _pdfService;
    private readonly IDialogService _dialog;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private DateTime fromDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime toDate = DateTime.Today;
    [ObservableProperty] private SalesInvoice? selectedInvoice;

    public ObservableCollection<SalesInvoice> Invoices { get; } = new();

    public bool CanAdd => _currentUser.HasPermission(Domain.Common.PermissionCodes.SalesAdd);
    public bool CanCancel => _currentUser.HasPermission(Domain.Common.PermissionCodes.SalesCancel);
    public bool CanPrint => _currentUser.HasPermission(Domain.Common.PermissionCodes.SalesPrint);

    public SalesViewModel(SalesService salesService, CustomerService customerService, ProductService productService,
        SettingsService settingsService, InvoicePdfService pdfService, IDialogService dialog, ICurrentUserService currentUser)
    {
        _salesService = salesService;
        _customerService = customerService;
        _productService = productService;
        _settingsService = settingsService;
        _pdfService = pdfService;
        _dialog = dialog;
        _currentUser = currentUser;
        SearchAsync().FireAndForgetSafe(_dialog, "المبيعات");
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var results = await _salesService.SearchAsync(FromDate, ToDate.AddDays(1).AddSeconds(-1));
        Invoices.Clear();
        foreach (var i in results) Invoices.Add(i);
    }

    [RelayCommand]
    private async Task NewInvoiceAsync()
    {
        var window = new Views.SalesInvoiceWindow(_salesService, _customerService, _productService, _settingsService, _currentUser, _dialog);
        if (window.ShowDialog() == true)
            await SearchAsync();
    }

    [RelayCommand]
    private async Task CancelInvoiceAsync()
    {
        if (SelectedInvoice is null) { _dialog.ShowInfo("الرجاء اختيار فاتورة"); return; }
        if (SelectedInvoice.Status == DocumentStatus.Cancelled) { _dialog.ShowInfo("الفاتورة ملغاة بالفعل"); return; }

        var reason = Views.InputDialog.Show("سبب الإلغاء", "الرجاء إدخال سبب إلغاء الفاتورة:");
        if (string.IsNullOrWhiteSpace(reason)) return;
        if (!_dialog.Confirm("سيتم إلغاء الفاتورة وإعادة الكميات إلى المخزون. هل أنت متأكد؟")) return;

        var result = await _salesService.CancelAsync(SelectedInvoice.Id, reason, _currentUser.UserId);
        if (result.Success) { _dialog.ShowSuccess(result.Message); await SearchAsync(); }
        else _dialog.ShowError(result.Message);
    }

    [RelayCommand]
    private async Task PrintInvoiceAsync()
    {
        if (SelectedInvoice is null) { _dialog.ShowInfo("الرجاء اختيار فاتورة"); return; }
        var full = await _salesService.GetByIdAsync(SelectedInvoice.Id);
        if (full is null) return;

        var company = await _settingsService.GetCompanySettingsAsync();
        var pdfData = new InvoicePdfData
        {
            Title = "فاتورة بيع",
            InvoiceNumber = full.InvoiceNumber,
            InvoiceDate = full.InvoiceDate,
            PartyLabel = "العميل",
            PartyName = full.Customer.Name,
            PartyPhone = full.Customer.Phone,
            PaymentMethod = full.PaymentMethod.ToString(),
            SubTotal = full.SubTotal,
            DiscountAmount = full.DiscountAmount,
            TaxAmount = full.TaxAmount,
            TotalAmount = full.TotalAmount,
            PaidAmount = full.PaidAmount,
            RemainingAmount = full.RemainingAmount,
            Notes = full.Notes,
            CreatedBy = _currentUser.UserName,
            Lines = full.Items.Select(i => new InvoicePdfLine { ProductName = i.Product.Name, Quantity = i.Quantity, UnitPrice = i.UnitPrice, Discount = i.DiscountAmount, LineTotal = i.LineTotal }).ToList()
        };

        var bytes = _pdfService.GenerateInvoice(pdfData, new CompanyHeaderInfo { CompanyName = company.CompanyName, Phone = company.Phone, Address = company.Address, TaxNumber = company.TaxNumber, FooterText = company.InvoiceFooterText });

        var path = _dialog.SaveFile("PDF Files|*.pdf", $"{full.InvoiceNumber}.pdf", "حفظ الفاتورة");
        if (path is null) return;
        await File.WriteAllBytesAsync(path, bytes);
        _dialog.ShowSuccess("تم حفظ الفاتورة، يمكنك فتحها للطباعة");
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true }); } catch { }
    }
}
