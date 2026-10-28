using System.IO;
using System.Collections;
using System.Collections.ObjectModel;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using Abela.Infrastructure.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Abela.App.ViewModels;

public enum ReportType
{
    SalesByPeriod, SalesByProduct, SalesByCustomer, SalesByUser, TopSellingProducts,
    PurchasesByPeriod,
    CurrentStock, LowStock, SerialNumbers,
    CustomerBalances, SupplierBalances,
    FinancialSummary
}

public record ReportTypeOption(ReportType Value, string Label);

public partial class ReportsViewModel : ObservableObject
{
    private readonly ReportService _reportService;
    private readonly CustomerService _customerService;
    private readonly SupplierService _supplierService;
    private readonly SettingsService _settingsService;
    private readonly InvoicePdfService _pdfService;
    private readonly ExcelExportService _excelService;
    private readonly IDialogService _dialog;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private ReportType selectedReportType = ReportType.SalesByPeriod;
    [ObservableProperty] private DateTime fromDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime toDate = DateTime.Today;
    [ObservableProperty] private IEnumerable? resultRows;
    [ObservableProperty] private string reportTitle = string.Empty;
    [ObservableProperty] private string financialSummaryText = string.Empty;

    public List<ReportTypeOption> ReportTypeOptions { get; } = new()
    {
        new(ReportType.SalesByPeriod, "المبيعات خلال فترة"),
        new(ReportType.SalesByProduct, "المبيعات حسب المنتج"),
        new(ReportType.SalesByCustomer, "المبيعات حسب العميل"),
        new(ReportType.SalesByUser, "المبيعات حسب المستخدم"),
        new(ReportType.TopSellingProducts, "المنتجات الأكثر مبيعًا"),
        new(ReportType.PurchasesByPeriod, "المشتريات خلال فترة"),
        new(ReportType.CurrentStock, "المخزون الحالي"),
        new(ReportType.LowStock, "المنتجات منخفضة المخزون"),
        new(ReportType.SerialNumbers, "الأرقام التسلسلية"),
        new(ReportType.CustomerBalances, "أرصدة العملاء المدينين"),
        new(ReportType.SupplierBalances, "أرصدة الموردين"),
        new(ReportType.FinancialSummary, "الملخص المالي (تقديري)"),
    };

    public bool CanViewProfit => _currentUser.HasPermission(Domain.Common.PermissionCodes.ReportsViewProfit);

    private string[] _lastHeaders = Array.Empty<string>();
    private List<string[]> _lastRows = new();

    /// <summary>Maps the bound row type's C# property names to their Arabic display headers, for the DataGrid's AutoGeneratingColumn handler.</summary>
    public Dictionary<string, string> CurrentHeaderMap { get; private set; } = new();

    public ReportsViewModel(ReportService reportService, CustomerService customerService, SupplierService supplierService,
        SettingsService settingsService, InvoicePdfService pdfService, ExcelExportService excelService, IDialogService dialog, ICurrentUserService currentUser)
    {
        _reportService = reportService;
        _customerService = customerService;
        _supplierService = supplierService;
        _settingsService = settingsService;
        _pdfService = pdfService;
        _excelService = excelService;
        _dialog = dialog;
        _currentUser = currentUser;
    }

    [RelayCommand]
    private async Task RunAsync()
    {
        var to = ToDate.AddDays(1).AddSeconds(-1);
        FinancialSummaryText = string.Empty;

        switch (SelectedReportType)
        {
            case ReportType.SalesByPeriod:
                ReportTitle = "تقرير المبيعات خلال فترة";
                SetRows(await _reportService.SalesInvoicesReportAsync(FromDate, to),
                    new[] { "رقم", "التاريخ", "العميل", "الإجمالي", "المدفوع", "المتبقي", "الحالة" },
                    r => new object[] { r.Number, r.Date, r.Party, r.Total, r.Paid, r.Remaining, r.Status });
                break;
            case ReportType.SalesByProduct:
                ReportTitle = "المبيعات حسب المنتج";
                SetRows(await _reportService.SalesByProductAsync(FromDate, to),
                    new[] { "المنتج", "الكمية", "الإجمالي" }, r => new object[] { r.ProductName, r.Quantity, r.Total });
                break;
            case ReportType.SalesByCustomer:
                ReportTitle = "المبيعات حسب العميل";
                SetRows(await _reportService.SalesByCustomerAsync(FromDate, to),
                    new[] { "العميل", "عدد الفواتير", "الإجمالي", "المتبقي" }, r => new object[] { r.CustomerName, r.InvoiceCount, r.Total, r.Remaining });
                break;
            case ReportType.SalesByUser:
                ReportTitle = "المبيعات حسب المستخدم";
                SetRows(await _reportService.SalesByUserAsync(FromDate, to),
                    new[] { "المستخدم", "عدد الفواتير", "الإجمالي" }, r => new object[] { r.UserName, r.InvoiceCount, r.Total });
                break;
            case ReportType.TopSellingProducts:
                ReportTitle = "المنتجات الأكثر مبيعًا";
                SetRows(await _reportService.TopSellingProductsAsync(FromDate, to),
                    new[] { "المنتج", "الكمية", "الإجمالي" }, r => new object[] { r.ProductName, r.Quantity, r.Total });
                break;
            case ReportType.PurchasesByPeriod:
                ReportTitle = "تقرير المشتريات خلال فترة";
                SetRows(await _reportService.PurchaseInvoicesReportAsync(FromDate, to),
                    new[] { "رقم", "التاريخ", "المورد", "الإجمالي", "المدفوع", "المتبقي", "الحالة" },
                    r => new object[] { r.Number, r.Date, r.Party, r.Total, r.Paid, r.Remaining, r.Status });
                break;
            case ReportType.CurrentStock:
                ReportTitle = "المخزون الحالي";
                SetRows(await _reportService.CurrentStockReportAsync(false),
                    new[] { "الكود", "المنتج", "التصنيف", "الكمية", "الحد الأدنى", "سعر الشراء", "قيمة المخزون" },
                    r => new object[] { r.Code, r.ProductName, r.Category, r.Quantity, r.MinLevel, r.PurchasePrice, r.StockValue });
                break;
            case ReportType.LowStock:
                ReportTitle = "المنتجات منخفضة المخزون";
                SetRows(await _reportService.CurrentStockReportAsync(true),
                    new[] { "الكود", "المنتج", "التصنيف", "الكمية", "الحد الأدنى", "سعر الشراء", "قيمة المخزون" },
                    r => new object[] { r.Code, r.ProductName, r.Category, r.Quantity, r.MinLevel, r.PurchasePrice, r.StockValue });
                break;
            case ReportType.SerialNumbers:
                ReportTitle = "الأرقام التسلسلية";
                SetRows(await _reportService.SerialNumbersReportAsync(),
                    new[] { "المنتج", "الرقم التسلسلي", "الحالة" }, r => new object[] { r.ProductName, r.SerialNumber, r.Status });
                break;
            case ReportType.CustomerBalances:
                ReportTitle = "أرصدة العملاء المدينين";
                SetRows(await _reportService.CustomerBalancesAsync(_customerService),
                    new[] { "الكود", "الاسم", "الرصيد" }, r => new object[] { r.Code, r.Name, r.Balance });
                break;
            case ReportType.SupplierBalances:
                ReportTitle = "أرصدة الموردين";
                SetRows(await _reportService.SupplierBalancesAsync(_supplierService),
                    new[] { "الكود", "الاسم", "الرصيد" }, r => new object[] { r.Code, r.Name, r.Balance });
                break;
            case ReportType.FinancialSummary:
                ReportTitle = "الملخص المالي (تقديري)";
                var summary = await _reportService.FinancialSummaryAsync(FromDate, to);
                var rows = new List<object> { summary };
                _lastHeaders = new[] { "البند", "القيمة" };
                _lastRows = new List<string[]>
                {
                    new[] { "إجمالي المبيعات", summary.TotalSales.ToString("N2") },
                    new[] { "إجمالي المشتريات", summary.TotalPurchases.ToString("N2") },
                    new[] { "إجمالي المصروفات", summary.TotalExpenses.ToString("N2") },
                    new[] { "إجمالي المحصل من العملاء", summary.TotalCollected.ToString("N2") },
                    new[] { "إجمالي المدفوع للموردين", summary.TotalPaid.ToString("N2") },
                    new[] { "الأرباح التقديرية", summary.EstimatedProfit.ToString("N2") },
                    new[] { "صافي الحركة المالية", summary.NetCashFlow.ToString("N2") },
                };
                ResultRows = _lastRows.Select(r => new { البند = r[0], القيمة = r[1] }).ToList();
                FinancialSummaryText = "ملاحظة: الأرباح التقديرية والملخص المالي أرقام تقديرية وليست قوائم محاسبية رسمية معتمدة.";
                break;
        }
    }

    private void SetRows<T>(List<T> data, string[] headers, Func<T, object[]> toRow)
    {
        _lastHeaders = headers;
        _lastRows = data.Select(d => toRow(d).Select(FormatCell).ToArray()).ToList();

        var properties = typeof(T).GetProperties();
        var map = new Dictionary<string, string>();
        for (int i = 0; i < properties.Length && i < headers.Length; i++)
            map[properties[i].Name] = headers[i];
        CurrentHeaderMap = map; // must be set before ResultRows, since setting ResultRows synchronously triggers the DataGrid's AutoGeneratingColumn

        ResultRows = data;
    }

    private static string FormatCell(object value) => value switch
    {
        DateTime dt => dt.ToString("yyyy-MM-dd"),
        decimal dec => dec.ToString("N2"),
        _ => value?.ToString() ?? ""
    };

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        if (_lastRows.Count == 0) { _dialog.ShowInfo("لا توجد بيانات لتصديرها، الرجاء تشغيل التقرير أولاً"); return; }
        var path = _dialog.SaveFile("PDF Files|*.pdf", $"{ReportTitle}.pdf");
        if (path is null) return;

        var company = await _settingsService.GetCompanySettingsAsync();
        var bytes = _pdfService.GenerateTabularReport(ReportTitle, _lastHeaders, _lastRows,
            new CompanyHeaderInfo { CompanyName = company.CompanyName, Phone = company.Phone, Address = company.Address, TaxNumber = company.TaxNumber },
            $"من {FromDate:yyyy-MM-dd} إلى {ToDate:yyyy-MM-dd}");
        await File.WriteAllBytesAsync(path, bytes);
        _dialog.ShowSuccess("تم تصدير التقرير بصيغة PDF بنجاح");
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (_lastRows.Count == 0) { _dialog.ShowInfo("لا توجد بيانات لتصديرها، الرجاء تشغيل التقرير أولاً"); return; }
        var path = _dialog.SaveFile("Excel Files|*.xlsx", $"{ReportTitle}.xlsx");
        if (path is null) return;

        _excelService.Export(path, ReportTitle, _lastHeaders, _lastRows.Select(r => (object[])r));
        _dialog.ShowSuccess("تم تصدير التقرير بصيغة Excel بنجاح");
    }
}
