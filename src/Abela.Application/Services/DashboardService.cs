using Abela.Application.Common;
using Abela.Domain.Enums;
using Abela.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abela.Application.Services;

public class DashboardSummary
{
    public decimal TodaySales { get; set; }
    public decimal TodayPurchases { get; set; }
    public decimal TodayExpenses { get; set; }
    public decimal NetToday { get; set; }
    public int ProductsCount { get; set; }
    public int CustomersCount { get; set; }
    public int SuppliersCount { get; set; }
    public decimal CustomerDebts { get; set; }
    public decimal SupplierDebts { get; set; }
    public List<(string ProductName, decimal Quantity, decimal MinLevel)> LowStockProducts { get; set; } = new();
    public List<(string Number, DateTime Date, string Party, decimal Total)> RecentSales { get; set; } = new();
    public List<(string Number, DateTime Date, string Party, decimal Total)> RecentPurchases { get; set; } = new();
    public List<(DateTime Date, decimal Total)> SalesLast7Days { get; set; } = new();
}

public class DashboardService
{
    private readonly AppDbContext _db;
    private readonly CustomerService _customerService;
    private readonly SupplierService _supplierService;
    public DashboardService(AppDbContext db, CustomerService customerService, SupplierService supplierService)
    { _db = db; _customerService = customerService; _supplierService = supplierService; }

    public async Task<DashboardSummary> GetSummaryAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var summary = new DashboardSummary
        {
            TodaySales = await _db.SalesInvoices.Where(i => i.InvoiceDate >= today && i.InvoiceDate < tomorrow && i.Status == DocumentStatus.Active).SumDecimalAsync(i => i.TotalAmount),
            TodayPurchases = await _db.PurchaseInvoices.Where(i => i.InvoiceDate >= today && i.InvoiceDate < tomorrow && i.Status == DocumentStatus.Active).SumDecimalAsync(i => i.TotalAmount),
            TodayExpenses = await _db.Expenses.Where(e => e.ExpenseDate >= today && e.ExpenseDate < tomorrow && e.Status == DocumentStatus.Active).SumDecimalAsync(e => e.Amount),
            ProductsCount = await _db.Products.CountAsync(p => p.IsActive),
            CustomersCount = await _db.Customers.CountAsync(c => c.IsActive),
            SuppliersCount = await _db.Suppliers.CountAsync(s => s.IsActive),
        };
        summary.NetToday = summary.TodaySales - summary.TodayPurchases - summary.TodayExpenses;

        // Use the same accurate balance calculation as customer/supplier statements (accounts for direct payments and returns,
        // not just the RemainingAmount stored on individual invoices).
        decimal customerDebts = 0;
        foreach (var customerId in await _db.Customers.Select(c => c.Id).ToListAsync())
        {
            var balance = await _customerService.GetBalanceAsync(customerId);
            if (balance > 0) customerDebts += balance;
        }
        summary.CustomerDebts = customerDebts;

        decimal supplierDebts = 0;
        foreach (var supplierId in await _db.Suppliers.Select(s => s.Id).ToListAsync())
        {
            var balance = await _supplierService.GetBalanceAsync(supplierId);
            if (balance > 0) supplierDebts += balance;
        }
        summary.SupplierDebts = supplierDebts;

        var lowStockRaw = await _db.Products
            .Where(p => p.IsActive && p.CurrentQuantity <= (p.MinStockLevel > 0 ? p.MinStockLevel : 3))
            .Select(p => new { p.Name, p.CurrentQuantity, p.MinStockLevel })
            .ToListAsync();
        summary.LowStockProducts = lowStockRaw.OrderBy(x => x.CurrentQuantity).Take(10).Select(x => (x.Name, x.CurrentQuantity, x.MinStockLevel)).ToList();

        var recentSalesRaw = await _db.SalesInvoices.Include(i => i.Customer)
            .OrderByDescending(i => i.InvoiceDate).Take(5)
            .Select(i => new { i.InvoiceNumber, i.InvoiceDate, Customer = i.Customer.Name, i.TotalAmount })
            .ToListAsync();
        summary.RecentSales = recentSalesRaw.Select(x => (x.InvoiceNumber, x.InvoiceDate, x.Customer, x.TotalAmount)).ToList();

        var recentPurchasesRaw = await _db.PurchaseInvoices.Include(i => i.Supplier)
            .OrderByDescending(i => i.InvoiceDate).Take(5)
            .Select(i => new { i.InvoiceNumber, i.InvoiceDate, Supplier = i.Supplier.Name, i.TotalAmount })
            .ToListAsync();
        summary.RecentPurchases = recentPurchasesRaw.Select(x => (x.InvoiceNumber, x.InvoiceDate, x.Supplier, x.TotalAmount)).ToList();

        var from = today.AddDays(-6);
        var salesByDay = await _db.SalesInvoices.Where(i => i.InvoiceDate >= from && i.Status == DocumentStatus.Active)
            .ToListAsync();
        for (var d = from; d <= today; d = d.AddDays(1))
        {
            var total = salesByDay.Where(i => i.InvoiceDate.Date == d).Sum(i => i.TotalAmount);
            summary.SalesLast7Days.Add((d, total));
        }

        return summary;
    }
}
