using Abela.Application.Common;
using Abela.Domain.Entities;
using Abela.Domain.Enums;
using Abela.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abela.Application.Services;

public class SalesReturnLineInput
{
    public int SalesInvoiceItemId { get; set; }
    public decimal Quantity { get; set; }
    public string? Reason { get; set; }
}

public class SalesReturnService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly NumberSequenceService _seq;
    private readonly InventoryService _inventory;

    public SalesReturnService(AppDbContext db, AuditService audit, NumberSequenceService seq, InventoryService inventory)
    { _db = db; _audit = audit; _seq = seq; _inventory = inventory; }

    public Task<List<SalesReturn>> SearchAsync() => _db.SalesReturns.Include(r => r.Customer).Include(r => r.SalesInvoice)
        .OrderByDescending(r => r.ReturnDate).ToListAsync();

    public async Task<ServiceResult<SalesReturn>> CreateAsync(int salesInvoiceId, RefundMethod refundMethod, List<SalesReturnLineInput> lines, string? notes, int userId)
    {
        var invoice = await _db.SalesInvoices.Include(i => i.Items).ThenInclude(it => it.SerialNumbers).FirstOrDefaultAsync(i => i.Id == salesInvoiceId);
        if (invoice is null) return ServiceResult<SalesReturn>.Fail("فاتورة البيع غير موجودة");
        if (invoice.Status == DocumentStatus.Cancelled) return ServiceResult<SalesReturn>.Fail("لا يمكن إرجاع أصناف من فاتورة ملغاة");
        if (lines.Count == 0) return ServiceResult<SalesReturn>.Fail("لا توجد أصناف للإرجاع");

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var salesReturn = new SalesReturn
            {
                ReturnNumber = await _seq.NextAsync("SR-", db => db.SalesReturns, r => r.ReturnNumber),
                SalesInvoiceId = invoice.Id,
                CustomerId = invoice.CustomerId,
                RefundMethod = refundMethod,
                Notes = notes,
                CreatedByUserId = userId
            };
            _db.SalesReturns.Add(salesReturn);
            await _db.SaveChangesAsync();

            decimal total = 0;
            foreach (var line in lines)
            {
                var invoiceItem = invoice.Items.FirstOrDefault(i => i.Id == line.SalesInvoiceItemId);
                if (invoiceItem is null) { await tx.RollbackAsync(); return ServiceResult<SalesReturn>.Fail("صنف الفاتورة غير موجود"); }

                var alreadyReturned = await _db.SalesReturnItems.Where(x => x.SalesInvoiceItemId == invoiceItem.Id).SumDecimalAsync(x => x.Quantity);
                if (line.Quantity <= 0 || line.Quantity > invoiceItem.Quantity - alreadyReturned)
                {
                    await tx.RollbackAsync();
                    return ServiceResult<SalesReturn>.Fail("كمية الإرجاع أكبر من الكمية المتاحة للإرجاع في الفاتورة الأصلية");
                }

                var lineTotal = line.Quantity * invoiceItem.UnitPrice;
                _db.SalesReturnItems.Add(new SalesReturnItem
                {
                    SalesReturnId = salesReturn.Id,
                    SalesInvoiceItemId = invoiceItem.Id,
                    ProductId = invoiceItem.ProductId,
                    Quantity = line.Quantity,
                    UnitPrice = invoiceItem.UnitPrice,
                    LineTotal = lineTotal,
                    Reason = line.Reason
                });

                var product = await _db.Products.FindAsync(invoiceItem.ProductId);
                if (product != null)
                    await _inventory.AdjustStockAsync(product, line.Quantity, InventoryTransactionType.SalesReturn, "SalesReturn", salesReturn.Id, userId, allowNegativeStock: true);

                foreach (var serial in invoiceItem.SerialNumbers.Where(s => s.Status == SerialStatus.Sold).Take((int)line.Quantity))
                    serial.Status = SerialStatus.Returned;

                total += lineTotal;
            }

            salesReturn.TotalAmount = total;
            // Customer balance automatically reflects this return (see CustomerService.GetBalanceAsync),
            // regardless of refund method — no separate payment record needed here.

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await _audit.LogAsync("Create", $"مرتجع مبيعات {salesReturn.ReturnNumber}", "SalesReturn", salesReturn.Id);
            return ServiceResult<SalesReturn>.Ok(salesReturn, "تم تسجيل المرتجع بنجاح");
        }
        catch { await tx.RollbackAsync(); throw; }
    }
}

public class PurchaseReturnLineInput
{
    public int PurchaseInvoiceItemId { get; set; }
    public decimal Quantity { get; set; }
    public string? Reason { get; set; }
}

public class PurchaseReturnService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly NumberSequenceService _seq;
    private readonly InventoryService _inventory;
    private readonly SettingsService _settings;

    public PurchaseReturnService(AppDbContext db, AuditService audit, NumberSequenceService seq, InventoryService inventory, SettingsService settings)
    { _db = db; _audit = audit; _seq = seq; _inventory = inventory; _settings = settings; }

    public Task<List<PurchaseReturn>> SearchAsync() => _db.PurchaseReturns.Include(r => r.Supplier).Include(r => r.PurchaseInvoice)
        .OrderByDescending(r => r.ReturnDate).ToListAsync();

    public async Task<ServiceResult<PurchaseReturn>> CreateAsync(int purchaseInvoiceId, List<PurchaseReturnLineInput> lines, string? notes, int userId)
    {
        var invoice = await _db.PurchaseInvoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == purchaseInvoiceId);
        if (invoice is null) return ServiceResult<PurchaseReturn>.Fail("فاتورة الشراء غير موجودة");
        if (invoice.Status == DocumentStatus.Cancelled) return ServiceResult<PurchaseReturn>.Fail("لا يمكن إرجاع أصناف من فاتورة ملغاة");
        if (lines.Count == 0) return ServiceResult<PurchaseReturn>.Fail("لا توجد أصناف للإرجاع");

        var settings = await _settings.GetApplicationSettingsAsync();

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var purchaseReturn = new PurchaseReturn
            {
                ReturnNumber = await _seq.NextAsync("PR-", db => db.PurchaseReturns, r => r.ReturnNumber),
                PurchaseInvoiceId = invoice.Id,
                SupplierId = invoice.SupplierId,
                Notes = notes,
                CreatedByUserId = userId
            };
            _db.PurchaseReturns.Add(purchaseReturn);
            await _db.SaveChangesAsync();

            decimal total = 0;
            foreach (var line in lines)
            {
                var invoiceItem = invoice.Items.FirstOrDefault(i => i.Id == line.PurchaseInvoiceItemId);
                if (invoiceItem is null) { await tx.RollbackAsync(); return ServiceResult<PurchaseReturn>.Fail("صنف الفاتورة غير موجود"); }

                var alreadyReturned = await _db.PurchaseReturnItems.Where(x => x.PurchaseInvoiceItemId == invoiceItem.Id).SumDecimalAsync(x => x.Quantity);
                if (line.Quantity <= 0 || line.Quantity > invoiceItem.Quantity - alreadyReturned)
                {
                    await tx.RollbackAsync();
                    return ServiceResult<PurchaseReturn>.Fail("كمية الإرجاع أكبر من الكمية المتاحة للإرجاع في فاتورة الشراء الأصلية");
                }

                var lineTotal = line.Quantity * invoiceItem.UnitCost;
                _db.PurchaseReturnItems.Add(new PurchaseReturnItem
                {
                    PurchaseReturnId = purchaseReturn.Id,
                    PurchaseInvoiceItemId = invoiceItem.Id,
                    ProductId = invoiceItem.ProductId,
                    Quantity = line.Quantity,
                    UnitCost = invoiceItem.UnitCost,
                    LineTotal = lineTotal,
                    Reason = line.Reason
                });

                var product = await _db.Products.FindAsync(invoiceItem.ProductId);
                if (product != null)
                {
                    var result = await _inventory.AdjustStockAsync(product, -line.Quantity, InventoryTransactionType.PurchaseReturn, "PurchaseReturn", purchaseReturn.Id, userId, settings.AllowNegativeStock);
                    if (!result.Success) { await tx.RollbackAsync(); return ServiceResult<PurchaseReturn>.Fail(result.Message); }
                }

                total += lineTotal;
            }

            purchaseReturn.TotalAmount = total;
            // Supplier balance automatically reflects this return (see SupplierService.GetBalanceAsync) — no separate payment record needed here.

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await _audit.LogAsync("Create", $"مرتجع مشتريات {purchaseReturn.ReturnNumber}", "PurchaseReturn", purchaseReturn.Id);
            return ServiceResult<PurchaseReturn>.Ok(purchaseReturn, "تم تسجيل مرتجع الشراء بنجاح");
        }
        catch { await tx.RollbackAsync(); throw; }
    }
}
