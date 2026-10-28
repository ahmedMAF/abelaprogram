namespace Abela.Domain.Entities;

public class CompanySettings
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = "Abela";
    public string? LogoPath { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? InvoiceFooterText { get; set; }
}

public class ApplicationSettings
{
    public int Id { get; set; }
    public string SalesInvoicePrefix { get; set; } = "SI-";
    public string PurchaseInvoicePrefix { get; set; } = "PI-";
    public string ReceiptVoucherPrefix { get; set; } = "RV-";
    public string PaymentVoucherPrefix { get; set; } = "PV-";
    public string SalesReturnPrefix { get; set; } = "SR-";
    public string PurchaseReturnPrefix { get; set; } = "PR-";
    public string StockAdjustmentPrefix { get; set; } = "ADJ-";
    public string ExpensePrefix { get; set; } = "EXP-";
    public decimal DefaultTaxRate { get; set; } = 0;
    public bool TaxEnabled { get; set; } = false;
    public bool AllowPriceEdit { get; set; } = true;
    public bool AllowNegativeStock { get; set; } = false;
    public string? DefaultPrinter { get; set; }
    public string PaperSize { get; set; } = "A4";
    public string Currency { get; set; } = "SAR";
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public string Language { get; set; } = "ar";
    public string? BackupPath { get; set; }
    public int DebtAlertDays { get; set; } = 30;
}
