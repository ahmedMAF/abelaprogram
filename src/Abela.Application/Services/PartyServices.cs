using Abela.Application.Common;
using Abela.Domain.Entities;
using Abela.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abela.Application.Services;

public class CustomerService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    public CustomerService(AppDbContext db, AuditService audit) { _db = db; _audit = audit; }

    public async Task<List<Customer>> SearchAsync(string? term = null)
    {
        var q = _db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(term))
            q = q.Where(c => c.Name.Contains(term) || c.Code.Contains(term) || (c.Phone != null && c.Phone.Contains(term)));
        return await q.OrderBy(c => c.Name).ToListAsync();
    }

    public Task<Customer?> GetByIdAsync(int id) => _db.Customers.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<string> GenerateCodeAsync()
    {
        var codes = await _db.Customers.IgnoreQueryFilters().Select(c => c.Code).ToListAsync();
        int max = 0;
        foreach (var c in codes) if (c.StartsWith("C-") && int.TryParse(c[2..], out var n) && n > max) max = n;
        return "C-" + (max + 1).ToString().PadLeft(4, '0');
    }

    public async Task<decimal> GetBalanceAsync(int customerId)
    {
        var customer = await _db.Customers.IgnoreQueryFilters().FirstAsync(c => c.Id == customerId);
        var invoicesTotal = await _db.SalesInvoices.Where(i => i.CustomerId == customerId && i.Status == Domain.Enums.DocumentStatus.Active).SumDecimalAsync(i => i.TotalAmount);
        var paid = await _db.SalesInvoices.Where(i => i.CustomerId == customerId && i.Status == Domain.Enums.DocumentStatus.Active).SumDecimalAsync(i => i.PaidAmount);
        var directPayments = await _db.CustomerPayments.Where(p => p.CustomerId == customerId && p.SalesInvoiceId == null).SumDecimalAsync(p => p.Amount);
        // Returned goods reduce what the customer owes regardless of refund method (cash refund vs. balance credit is just how it's settled).
        var returns = await _db.SalesReturns.Where(r => r.CustomerId == customerId).SumDecimalAsync(r => r.TotalAmount);
        return customer.OpeningBalance + invoicesTotal - paid - directPayments - returns;
    }

    public async Task<ServiceResult> SaveAsync(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Name)) return ServiceResult.Fail("اسم العميل مطلوب");
        if (customer.Id == 0) { _db.Customers.Add(customer); await _db.SaveChangesAsync(); await _audit.LogAsync("Create", $"إضافة عميل {customer.Name}", "Customer", customer.Id); }
        else { customer.UpdatedAt = DateTime.Now; _db.Customers.Update(customer); await _db.SaveChangesAsync(); await _audit.LogAsync("Update", $"تعديل عميل {customer.Name}", "Customer", customer.Id); }
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        if (await _db.Customers.Where(c => c.Id == id).Select(c => c.IsDefault).FirstOrDefaultAsync())
            return ServiceResult.Fail("لا يمكن حذف العميل النقدي الافتراضي");
        var used = await _db.SalesInvoices.AnyAsync(i => i.CustomerId == id) || await _db.CustomerPayments.AnyAsync(p => p.CustomerId == id);
        if (used) return ServiceResult.Fail("لا يمكن حذف العميل لارتباطه بعمليات، يمكن تعطيله");
        var c = await _db.Customers.FindAsync(id);
        if (c is null) return ServiceResult.Fail("العميل غير موجود");
        _db.Customers.Remove(c);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Delete", $"حذف عميل {c.Name}", "Customer", id);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeactivateAsync(int id)
    {
        var c = await _db.Customers.FindAsync(id);
        if (c is null) return ServiceResult.Fail("العميل غير موجود");
        c.IsActive = false;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}

public class SupplierService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    public SupplierService(AppDbContext db, AuditService audit) { _db = db; _audit = audit; }

    public async Task<List<Supplier>> SearchAsync(string? term = null)
    {
        var q = _db.Suppliers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(term))
            q = q.Where(s => s.Name.Contains(term) || s.Code.Contains(term) || (s.Phone != null && s.Phone.Contains(term)));
        return await q.OrderBy(s => s.Name).ToListAsync();
    }

    public Task<Supplier?> GetByIdAsync(int id) => _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id);

    public async Task<string> GenerateCodeAsync()
    {
        var codes = await _db.Suppliers.IgnoreQueryFilters().Select(s => s.Code).ToListAsync();
        int max = 0;
        foreach (var c in codes) if (c.StartsWith("S-") && int.TryParse(c[2..], out var n) && n > max) max = n;
        return "S-" + (max + 1).ToString().PadLeft(4, '0');
    }

    public async Task<decimal> GetBalanceAsync(int supplierId)
    {
        var supplier = await _db.Suppliers.IgnoreQueryFilters().FirstAsync(s => s.Id == supplierId);
        var invoicesTotal = await _db.PurchaseInvoices.Where(i => i.SupplierId == supplierId && i.Status == Domain.Enums.DocumentStatus.Active).SumDecimalAsync(i => i.TotalAmount);
        var paid = await _db.PurchaseInvoices.Where(i => i.SupplierId == supplierId && i.Status == Domain.Enums.DocumentStatus.Active).SumDecimalAsync(i => i.PaidAmount);
        var directPayments = await _db.SupplierPayments.Where(p => p.SupplierId == supplierId && p.PurchaseInvoiceId == null).SumDecimalAsync(p => p.Amount);
        // Returned goods reduce what we owe the supplier.
        var returns = await _db.PurchaseReturns.Where(r => r.SupplierId == supplierId).SumDecimalAsync(r => r.TotalAmount);
        return supplier.OpeningBalance + invoicesTotal - paid - directPayments - returns;
    }

    public async Task<ServiceResult> SaveAsync(Supplier supplier)
    {
        if (string.IsNullOrWhiteSpace(supplier.Name)) return ServiceResult.Fail("اسم المورد مطلوب");
        if (supplier.Id == 0) { _db.Suppliers.Add(supplier); await _db.SaveChangesAsync(); await _audit.LogAsync("Create", $"إضافة مورد {supplier.Name}", "Supplier", supplier.Id); }
        else { supplier.UpdatedAt = DateTime.Now; _db.Suppliers.Update(supplier); await _db.SaveChangesAsync(); await _audit.LogAsync("Update", $"تعديل مورد {supplier.Name}", "Supplier", supplier.Id); }
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var used = await _db.PurchaseInvoices.AnyAsync(i => i.SupplierId == id) || await _db.SupplierPayments.AnyAsync(p => p.SupplierId == id);
        if (used) return ServiceResult.Fail("لا يمكن حذف المورد لارتباطه بعمليات، يمكن تعطيله");
        var s = await _db.Suppliers.FindAsync(id);
        if (s is null) return ServiceResult.Fail("المورد غير موجود");
        _db.Suppliers.Remove(s);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Delete", $"حذف مورد {s.Name}", "Supplier", id);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeactivateAsync(int id)
    {
        var s = await _db.Suppliers.FindAsync(id);
        if (s is null) return ServiceResult.Fail("المورد غير موجود");
        s.IsActive = false;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}
