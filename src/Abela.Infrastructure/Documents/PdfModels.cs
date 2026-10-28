namespace Abela.Infrastructure.Documents;

public class CompanyHeaderInfo
{
    public string CompanyName { get; set; } = "Abela";
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? FooterText { get; set; }
}

public class InvoicePdfLine
{
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
}

public class InvoicePdfData
{
    public string Title { get; set; } = "فاتورة";
    public string InvoiceNumber { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public string PartyLabel { get; set; } = "العميل";
    public string PartyName { get; set; } = "";
    public string? PartyPhone { get; set; }
    public string PaymentMethod { get; set; } = "";
    public List<InvoicePdfLine> Lines { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ExtraCosts { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string? Notes { get; set; }
    public string CreatedBy { get; set; } = "";
}

public class VoucherPdfData
{
    public string Title { get; set; } = "سند";
    public string Number { get; set; } = "";
    public DateTime Date { get; set; }
    public string PartyLabel { get; set; } = "";
    public string PartyName { get; set; } = "";
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public string CreatedBy { get; set; } = "";
}

public class ReturnPdfData
{
    public string Title { get; set; } = "مستند مرتجع";
    public string ReturnNumber { get; set; } = "";
    public DateTime ReturnDate { get; set; }
    public string OriginalInvoiceNumber { get; set; } = "";
    public string PartyLabel { get; set; } = "";
    public string PartyName { get; set; } = "";
    public List<InvoicePdfLine> Lines { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public string CreatedBy { get; set; } = "";
}
