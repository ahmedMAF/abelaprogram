using Abela.Domain.Common;
using Abela.Domain.Enums;

namespace Abela.Domain.Entities;

public class CustomerPayment : BaseEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public int? SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }
    public string? Notes { get; set; }
    public int CreatedByUserId { get; set; }
}

public class SupplierPayment : BaseEntity
{
    public string VoucherNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public int? PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }
    public string? Notes { get; set; }
    public int CreatedByUserId { get; set; }
}

public class ExpenseCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}

public class Expense : BaseEntity
{
    public string ExpenseNumber { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; } = DateTime.Now;
    public int ExpenseCategoryId { get; set; }
    public ExpenseCategory ExpenseCategory { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? PayeeName { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public int CreatedByUserId { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Active;
}
