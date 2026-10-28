using Abela.Application.Common;
using Abela.Domain.Enums;
using Abela.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abela.Application.Services;

public class ReportService
{
    private readonly AppDbContext _db;
    public ReportService(AppDbContext db) { _db = db; }

    // ----- Sales -----
    public async Task<List<InvoiceSummaryRow>> SalesInvoicesReportAsync(DateTime from, DateTime to, int? customerId = null)
    {
        var q = _db.SalesInvoices.Include(i => i.Customer).Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to);
        if (customerId.HasValue) q = q.Where(i => i.CustomerId == customerId);
        return await q.OrderByDescending(i => i.InvoiceDate)
            .Select(i => new InvoiceSummaryRow { Number = i.InvoiceNumber, Date = i.InvoiceDate, Party = i.Customer.Name, Total = i.TotalAmount, Paid = i.PaidAmount, Remaining = i.RemainingAmount, Status = i.Status == DocumentStatus.Active ? "فعالة" : "ملغاة" })
            .ToListAsync();
    }

    public async Task<List<SalesByProductRow>> SalesByProductAsync(DateTime from, DateTime to)
    {
        var items = await _db.SalesInvoiceItems.Include(i => i.Product).Include(i => i.SalesInvoice)
            .Where(i => i.SalesInvoice.InvoiceDate >= from && i.SalesInvoice.InvoiceDate <= to && i.SalesInvoice.Status == DocumentStatus.Active)
            .ToListAsync();
        return items.GroupBy(i => i.Product.Name)
            .Select(g => new SalesByProductRow { ProductName = g.Key, Quantity = g.Sum(x => x.Quantity), Total = g.Sum(x => x.LineTotal) })
            .OrderByDescending(x => x.Total).ToList();
    }

    public async Task<List<SalesByCustomerRow>> SalesByCustomerAsync(DateTime from, DateTime to)
    {
        var invoices = await _db.SalesInvoices.Include(i => i.Customer)
            .Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to && i.Status == DocumentStatus.Active).ToListAsync();
        return invoices.GroupBy(i => i.Customer.Name)
            .Select(g => new SalesByCustomerRow { CustomerName = g.Key, InvoiceCount = g.Count(), Total = g.Sum(x => x.TotalAmount), Remaining = g.Sum(x => x.RemainingAmount) })
            .OrderByDescending(x => x.Total).ToList();
    }

    public async Task<List<SalesByUserRow>> SalesByUserAsync(DateTime from, DateTime to)
    {
        var invoices = await _db.SalesInvoices.Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to && i.Status == DocumentStatus.Active).ToListAsync();
        var users = await _db.Users.ToDictionaryAsync(u => u.Id, u => u.FullName);
        return invoices.GroupBy(i => i.CreatedByUserId)
            .Select(g => new SalesByUserRow { UserName = users.GetValueOrDefault(g.Key, "غير معروف"), InvoiceCount = g.Count(), Total = g.Sum(x => x.TotalAmount) })
            .OrderByDescending(x => x.Total).ToList();
    }

    public async Task<List<SalesByProductRow>> TopSellingProductsAsync(DateTime from, DateTime to, int top = 10)
    {
        var rows = await SalesByProductAsync(from, to);
        return rows.OrderByDescending(r => r.Quantity).Take(top).ToList();
    }

    // ----- Purchases -----
    public async Task<List<InvoiceSummaryRow>> PurchaseInvoicesReportAsync(DateTime from, DateTime to, int? supplierId = null)
    {
        var q = _db.PurchaseInvoices.Include(i => i.Supplier).Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to);
        if (supplierId.HasValue) q = q.Where(i => i.SupplierId == supplierId);
        return await q.OrderByDescending(i => i.InvoiceDate)
            .Select(i => new InvoiceSummaryRow { Number = i.InvoiceNumber, Date = i.InvoiceDate, Party = i.Supplier.Name, Total = i.TotalAmount, Paid = i.PaidAmount, Remaining = i.RemainingAmount, Status = i.Status == DocumentStatus.Active ? "فعالة" : "ملغاة" })
            .ToListAsync();
    }

    // ----- Inventory -----
    public async Task<List<StockRow>> CurrentStockReportAsync(bool lowStockOnly = false)
    {
        var q = _db.Products.Include(p => p.Category).Where(p => p.IsActive).AsQueryable();
        if (lowStockOnly) q = q.Where(p => p.CurrentQuantity <= (p.MinStockLevel > 0 ? p.MinStockLevel : 3));
        return await q.OrderBy(p => p.Name)
            .Select(p => new StockRow { Code = p.Code, ProductName = p.Name, Category = p.Category.Name, Quantity = p.CurrentQuantity, MinLevel = p.MinStockLevel, PurchasePrice = p.PurchasePrice, StockValue = p.CurrentQuantity * p.PurchasePrice })
            .ToListAsync();
    }

    public async Task<List<SerialRow>> SerialNumbersReportAsync()
    {
        return await _db.ProductSerialNumbers.Include(s => s.Product)
            .OrderBy(s => s.Product.Name).ThenBy(s => s.SerialNumber)
            .Select(s => new SerialRow { ProductName = s.Product.Name, SerialNumber = s.SerialNumber, Status = s.Status == SerialStatus.InStock ? "متوفر" : s.Status == SerialStatus.Sold ? "مباع" : "مرتجع" })
            .ToListAsync();
    }

    public async Task<List<MovementRow>> ProductMovementAsync(int productId)
    {
        return await _db.InventoryTransactions.Include(t => t.Product).Where(t => t.ProductId == productId)
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => new MovementRow { Date = t.TransactionDate, ProductName = t.Product.Name, Type = t.TransactionType.ToString(), Before = t.QuantityBefore, Change = t.QuantityChange, After = t.QuantityAfter, Reference = t.ReferenceType, Notes = t.Notes })
            .ToListAsync();
    }

    // ----- Customers / Suppliers -----
    public async Task<List<StatementRow>> CustomerStatementAsync(int customerId, DateTime from, DateTime to)
    {
        var rows = new List<StatementRow>();
        var invoices = await _db.SalesInvoices.Where(i => i.CustomerId == customerId && i.InvoiceDate >= from && i.InvoiceDate <= to && i.Status == DocumentStatus.Active).ToListAsync();
        foreach (var i in invoices) rows.Add(new StatementRow { Date = i.InvoiceDate, Description = $"فاتورة بيع {i.InvoiceNumber}", Debit = i.TotalAmount, Credit = 0 });
        var payments = await _db.CustomerPayments.Where(p => p.CustomerId == customerId && p.PaymentDate >= from && p.PaymentDate <= to).ToListAsync();
        foreach (var p in payments) rows.Add(new StatementRow { Date = p.PaymentDate, Description = $"سند قبض {p.ReceiptNumber}", Debit = 0, Credit = p.Amount });

        rows = rows.OrderBy(r => r.Date).ToList();
        decimal balance = 0;
        foreach (var r in rows) { balance += r.Debit - r.Credit; r.Balance = balance; }
        return rows;
    }

    public async Task<List<BalanceRow>> CustomerBalancesAsync(CustomerService customerService)
    {
        var customers = await _db.Customers.ToListAsync();
        var result = new List<BalanceRow>();
        foreach (var c in customers)
        {
            var balance = await customerService.GetBalanceAsync(c.Id);
            if (balance != 0) result.Add(new BalanceRow { Code = c.Code, Name = c.Name, Balance = balance });
        }
        return result.OrderByDescending(r => r.Balance).ToList();
    }

    public async Task<List<StatementRow>> SupplierStatementAsync(int supplierId, DateTime from, DateTime to)
    {
        var rows = new List<StatementRow>();
        var invoices = await _db.PurchaseInvoices.Where(i => i.SupplierId == supplierId && i.InvoiceDate >= from && i.InvoiceDate <= to && i.Status == DocumentStatus.Active).ToListAsync();
        foreach (var i in invoices) rows.Add(new StatementRow { Date = i.InvoiceDate, Description = $"فاتورة شراء {i.InvoiceNumber}", Debit = 0, Credit = i.TotalAmount });
        var payments = await _db.SupplierPayments.Where(p => p.SupplierId == supplierId && p.PaymentDate >= from && p.PaymentDate <= to).ToListAsync();
        foreach (var p in payments) rows.Add(new StatementRow { Date = p.PaymentDate, Description = $"سند دفع {p.VoucherNumber}", Debit = p.Amount, Credit = 0 });

        rows = rows.OrderBy(r => r.Date).ToList();
        decimal balance = 0;
        foreach (var r in rows) { balance += r.Credit - r.Debit; r.Balance = balance; }
        return rows;
    }

    public async Task<List<BalanceRow>> SupplierBalancesAsync(SupplierService supplierService)
    {
        var suppliers = await _db.Suppliers.ToListAsync();
        var result = new List<BalanceRow>();
        foreach (var s in suppliers)
        {
            var balance = await supplierService.GetBalanceAsync(s.Id);
            if (balance != 0) result.Add(new BalanceRow { Code = s.Code, Name = s.Name, Balance = balance });
        }
        return result.OrderByDescending(r => r.Balance).ToList();
    }

    // ----- Financial -----
    public async Task<FinancialSummary> FinancialSummaryAsync(DateTime from, DateTime to)
    {
        var sales = await _db.SalesInvoices.Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to && i.Status == DocumentStatus.Active).ToListAsync();
        var purchases = await _db.PurchaseInvoices.Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to && i.Status == DocumentStatus.Active).ToListAsync();
        var expenses = await _db.Expenses.Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to && e.Status == DocumentStatus.Active).SumDecimalAsync(e => e.Amount);
        var collected = await _db.CustomerPayments.Where(p => p.PaymentDate >= from && p.PaymentDate <= to).SumDecimalAsync(p => p.Amount);
        var paid = await _db.SupplierPayments.Where(p => p.PaymentDate >= from && p.PaymentDate <= to).SumDecimalAsync(p => p.Amount);

        var soldItems = await _db.SalesInvoiceItems.Include(i => i.Product).Include(i => i.SalesInvoice)
            .Where(i => i.SalesInvoice.InvoiceDate >= from && i.SalesInvoice.InvoiceDate <= to && i.SalesInvoice.Status == DocumentStatus.Active)
            .ToListAsync();
        var profit = soldItems.Sum(i => (i.UnitPrice - i.Product.PurchasePrice) * i.Quantity - i.DiscountAmount);

        var totalSales = sales.Sum(i => i.TotalAmount);
        var totalPurchases = purchases.Sum(i => i.TotalAmount);

        return new FinancialSummary
        {
            TotalSales = totalSales,
            TotalPurchases = totalPurchases,
            TotalExpenses = expenses,
            TotalCollected = collected,
            TotalPaid = paid,
            EstimatedProfit = profit,
            NetCashFlow = collected - paid - expenses
        };
    }
}
