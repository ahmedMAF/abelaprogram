namespace Abela.Application.Services;

public class SalesByProductRow { public string ProductName { get; set; } = ""; public decimal Quantity { get; set; } public decimal Total { get; set; } }
public class SalesByCustomerRow { public string CustomerName { get; set; } = ""; public int InvoiceCount { get; set; } public decimal Total { get; set; } public decimal Remaining { get; set; } }
public class SalesByUserRow { public string UserName { get; set; } = ""; public int InvoiceCount { get; set; } public decimal Total { get; set; } }
public class InvoiceSummaryRow { public string Number { get; set; } = ""; public DateTime Date { get; set; } public string Party { get; set; } = ""; public decimal Total { get; set; } public decimal Paid { get; set; } public decimal Remaining { get; set; } public string Status { get; set; } = ""; }
public class StockRow { public string Code { get; set; } = ""; public string ProductName { get; set; } = ""; public string Category { get; set; } = ""; public decimal Quantity { get; set; } public decimal MinLevel { get; set; } public decimal PurchasePrice { get; set; } public decimal StockValue { get; set; } }
public class SerialRow { public string ProductName { get; set; } = ""; public string SerialNumber { get; set; } = ""; public string Status { get; set; } = ""; }
public class MovementRow { public DateTime Date { get; set; } public string ProductName { get; set; } = ""; public string Type { get; set; } = ""; public decimal Before { get; set; } public decimal Change { get; set; } public decimal After { get; set; } public string? Reference { get; set; } public string? Notes { get; set; } }
public class StatementRow { public DateTime Date { get; set; } public string Description { get; set; } = ""; public decimal Debit { get; set; } public decimal Credit { get; set; } public decimal Balance { get; set; } }
public class BalanceRow { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public decimal Balance { get; set; } }
public class PaymentRow { public string Number { get; set; } = ""; public DateTime Date { get; set; } public string Party { get; set; } = ""; public decimal Amount { get; set; } public string Method { get; set; } = ""; }

public class FinancialSummary
{
    public decimal TotalSales { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal EstimatedProfit { get; set; }
    public decimal NetCashFlow { get; set; }
}
