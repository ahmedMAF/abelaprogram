using Abela.Application.Common;
using Abela.Domain.Entities;
using Abela.Domain.Enums;
using Abela.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abela.Application.Services;

public class PaymentService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly NumberSequenceService _seq;
    private readonly SettingsService _settings;

    public PaymentService(AppDbContext db, AuditService audit, NumberSequenceService seq, SettingsService settings)
    { _db = db; _audit = audit; _seq = seq; _settings = settings; }

    public Task<List<CustomerPayment>> GetCustomerPaymentsAsync(int? customerId = null) =>
        (customerId.HasValue ? _db.CustomerPayments.Where(p => p.CustomerId == customerId) : _db.CustomerPayments)
        .Include(p => p.Customer).OrderByDescending(p => p.PaymentDate).ToListAsync();

    public Task<List<SupplierPayment>> GetSupplierPaymentsAsync(int? supplierId = null) =>
        (supplierId.HasValue ? _db.SupplierPayments.Where(p => p.SupplierId == supplierId) : _db.SupplierPayments)
        .Include(p => p.Supplier).OrderByDescending(p => p.PaymentDate).ToListAsync();

    public async Task<ServiceResult<CustomerPayment>> AddCustomerPaymentAsync(int customerId, decimal amount, PaymentMethod method, string? reference, int? salesInvoiceId, string? notes, int userId)
    {
        if (amount <= 0) return ServiceResult<CustomerPayment>.Fail("يجب أن يكون المبلغ أكبر من صفر");
        var settings = await _settings.GetApplicationSettingsAsync();
        var payment = new CustomerPayment
        {
            ReceiptNumber = await _seq.NextAsync(settings.ReceiptVoucherPrefix, db => db.CustomerPayments, p => p.ReceiptNumber),
            CustomerId = customerId,
            Amount = amount,
            PaymentMethod = method,
            ReferenceNumber = reference,
            SalesInvoiceId = salesInvoiceId,
            Notes = notes,
            CreatedByUserId = userId
        };
        _db.CustomerPayments.Add(payment);

        if (salesInvoiceId.HasValue)
        {
            var invoice = await _db.SalesInvoices.FindAsync(salesInvoiceId.Value);
            if (invoice != null)
            {
                invoice.PaidAmount += amount;
                invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;
            }
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync("Payment", $"سند قبض {payment.ReceiptNumber} بمبلغ {amount}", "CustomerPayment", payment.Id);
        return ServiceResult<CustomerPayment>.Ok(payment, "تم تسجيل الدفعة بنجاح");
    }

    public async Task<ServiceResult<SupplierPayment>> AddSupplierPaymentAsync(int supplierId, decimal amount, PaymentMethod method, string? reference, int? purchaseInvoiceId, string? notes, int userId)
    {
        if (amount <= 0) return ServiceResult<SupplierPayment>.Fail("يجب أن يكون المبلغ أكبر من صفر");
        var settings = await _settings.GetApplicationSettingsAsync();
        var payment = new SupplierPayment
        {
            VoucherNumber = await _seq.NextAsync(settings.PaymentVoucherPrefix, db => db.SupplierPayments, p => p.VoucherNumber),
            SupplierId = supplierId,
            Amount = amount,
            PaymentMethod = method,
            ReferenceNumber = reference,
            PurchaseInvoiceId = purchaseInvoiceId,
            Notes = notes,
            CreatedByUserId = userId
        };
        _db.SupplierPayments.Add(payment);

        if (purchaseInvoiceId.HasValue)
        {
            var invoice = await _db.PurchaseInvoices.FindAsync(purchaseInvoiceId.Value);
            if (invoice != null)
            {
                invoice.PaidAmount += amount;
                invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;
            }
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync("Payment", $"سند دفع {payment.VoucherNumber} بمبلغ {amount}", "SupplierPayment", payment.Id);
        return ServiceResult<SupplierPayment>.Ok(payment, "تم تسجيل الدفعة بنجاح");
    }
}

public class ExpenseService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly NumberSequenceService _seq;
    private readonly SettingsService _settings;

    public ExpenseService(AppDbContext db, AuditService audit, NumberSequenceService seq, SettingsService settings)
    { _db = db; _audit = audit; _seq = seq; _settings = settings; }

    public async Task<List<Expense>> SearchAsync(DateTime? from = null, DateTime? to = null, int? categoryId = null)
    {
        var q = _db.Expenses.Include(e => e.ExpenseCategory).AsQueryable();
        if (from.HasValue) q = q.Where(e => e.ExpenseDate >= from);
        if (to.HasValue) q = q.Where(e => e.ExpenseDate <= to);
        if (categoryId.HasValue) q = q.Where(e => e.ExpenseCategoryId == categoryId);
        return await q.OrderByDescending(e => e.ExpenseDate).ToListAsync();
    }

    public async Task<ServiceResult<Expense>> AddAsync(Expense expense)
    {
        if (expense.Amount <= 0) return ServiceResult<Expense>.Fail("يجب أن يكون مبلغ المصروف أكبر من صفر");
        if (string.IsNullOrWhiteSpace(expense.Description)) return ServiceResult<Expense>.Fail("وصف المصروف مطلوب");
        var settings = await _settings.GetApplicationSettingsAsync();
        expense.ExpenseNumber = await _seq.NextAsync(settings.ExpensePrefix, db => db.Expenses, e => e.ExpenseNumber);
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Create", $"تسجيل مصروف {expense.ExpenseNumber} بمبلغ {expense.Amount}", "Expense", expense.Id);
        return ServiceResult<Expense>.Ok(expense, "تم تسجيل المصروف بنجاح");
    }

    public async Task<ServiceResult> UpdateAsync(Expense expense)
    {
        if (expense.Amount <= 0) return ServiceResult.Fail("يجب أن يكون مبلغ المصروف أكبر من صفر");
        expense.UpdatedAt = DateTime.Now;
        _db.Expenses.Update(expense);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Update", $"تعديل مصروف {expense.ExpenseNumber}", "Expense", expense.Id);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> CancelAsync(int id)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense is null) return ServiceResult.Fail("المصروف غير موجود");
        expense.Status = DocumentStatus.Cancelled;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Cancel", $"إلغاء مصروف {expense.ExpenseNumber}", "Expense", id);
        return ServiceResult.Ok();
    }
}
