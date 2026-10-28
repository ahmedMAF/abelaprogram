using Abela.Application.Common;
using Abela.Domain.Entities;
using Abela.Domain.Enums;
using Abela.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abela.Application.Services;

public class PurchaseService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly NumberSequenceService _seq;
    private readonly InventoryService _inventory;
    private readonly SettingsService _settings;

    public PurchaseService(AppDbContext db, AuditService audit, NumberSequenceService seq, InventoryService inventory, SettingsService settings)
    {
        _db = db; _audit = audit; _seq = seq; _inventory = inventory; _settings = settings;
    }

    public async Task<List<PurchaseInvoice>> SearchAsync(DateTime? from = null, DateTime? to = null, int? supplierId = null, DocumentStatus? status = null)
    {
        var q = _db.PurchaseInvoices.Include(i => i.Supplier).AsQueryable();
        if (from.HasValue) q = q.Where(i => i.InvoiceDate >= from);
        if (to.HasValue) q = q.Where(i => i.InvoiceDate <= to);
        if (supplierId.HasValue) q = q.Where(i => i.SupplierId == supplierId);
        if (status.HasValue) q = q.Where(i => i.Status == status);
        return await q.OrderByDescending(i => i.InvoiceDate).ToListAsync();
    }

    public Task<PurchaseInvoice?> GetByIdAsync(int id) => _db.PurchaseInvoices
        .Include(i => i.Supplier)
        .Include(i => i.Items).ThenInclude(x => x.Product)
        .Include(i => i.Items).ThenInclude(x => x.SerialNumbers)
        .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<ServiceResult<PurchaseInvoice>> CreateAsync(int supplierId, string? supplierInvoiceNumber, PaymentMethod method,
        decimal discountAmount, decimal extraCosts, decimal paidAmount, string? notes, List<PurchaseLineInput> lines, int userId, bool updateSalePriceFromCost = false)
    {
        if (lines.Count == 0) return ServiceResult<PurchaseInvoice>.Fail("لا يمكن حفظ فاتورة بدون أصناف");

        var settings = await _settings.GetApplicationSettingsAsync();

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var invoice = new PurchaseInvoice
            {
                InvoiceNumber = await _seq.NextAsync(settings.PurchaseInvoicePrefix, db => db.PurchaseInvoices, i => i.InvoiceNumber),
                SupplierInvoiceNumber = supplierInvoiceNumber,
                SupplierId = supplierId,
                PaymentMethod = method,
                Notes = notes,
                CreatedByUserId = userId,
                Status = DocumentStatus.Active,
                ExtraCosts = extraCosts
            };
            _db.PurchaseInvoices.Add(invoice);
            await _db.SaveChangesAsync();

            decimal subTotal = 0, taxTotal = 0, lineDiscountTotal = 0;
            foreach (var line in lines)
            {
                if (line.Quantity <= 0) { await tx.RollbackAsync(); return ServiceResult<PurchaseInvoice>.Fail("يجب أن تكون الكمية أكبر من صفر"); }
                var product = await _db.Products.FindAsync(line.ProductId);
                if (product is null) { await tx.RollbackAsync(); return ServiceResult<PurchaseInvoice>.Fail("منتج غير موجود"); }

                if (product.RequiresSerialNumber && line.SerialNumbers.Count != (int)line.Quantity)
                {
                    await tx.RollbackAsync();
                    return ServiceResult<PurchaseInvoice>.Fail($"يجب إدخال الرقم التسلسلي لكل قطعة من {product.Name}");
                }

                var lineTotal = (line.Quantity * line.UnitCost) - line.DiscountAmount + line.TaxAmount;
                var item = new PurchaseInvoiceItem
                {
                    PurchaseInvoiceId = invoice.Id,
                    ProductId = product.Id,
                    Quantity = line.Quantity,
                    UnitCost = line.UnitCost,
                    DiscountAmount = line.DiscountAmount,
                    TaxAmount = line.TaxAmount,
                    LineTotal = lineTotal
                };
                _db.PurchaseInvoiceItems.Add(item);
                await _db.SaveChangesAsync();

                await _inventory.AdjustStockAsync(product, line.Quantity, InventoryTransactionType.Purchase,
                    "PurchaseInvoice", invoice.Id, userId, allowNegativeStock: true);

                product.PurchasePrice = line.UnitCost;
                if (updateSalePriceFromCost) { /* left to user via product edit screen to avoid unintended margin changes */ }

                if (product.RequiresSerialNumber)
                {
                    foreach (var sn in line.SerialNumbers)
                    {
                        var exists = await _db.ProductSerialNumbers.AnyAsync(s => s.ProductId == product.Id && s.SerialNumber == sn);
                        if (exists) { await tx.RollbackAsync(); return ServiceResult<PurchaseInvoice>.Fail($"الرقم التسلسلي {sn} مسجل مسبقًا لهذا المنتج"); }
                        _db.ProductSerialNumbers.Add(new ProductSerialNumber
                        {
                            ProductId = product.Id,
                            SerialNumber = sn,
                            Status = SerialStatus.InStock,
                            PurchaseInvoiceItemId = item.Id
                        });
                    }
                }

                subTotal += line.Quantity * line.UnitCost;
                lineDiscountTotal += line.DiscountAmount;
                taxTotal += line.TaxAmount;
            }

            invoice.SubTotal = subTotal;
            invoice.DiscountAmount = discountAmount;
            invoice.TaxAmount = taxTotal;
            invoice.TotalAmount = subTotal - lineDiscountTotal - discountAmount + taxTotal + extraCosts;
            invoice.PaidAmount = paidAmount;
            invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;

            if (invoice.PaidAmount > 0)
            {
                _db.SupplierPayments.Add(new SupplierPayment
                {
                    VoucherNumber = await _seq.NextAsync(settings.PaymentVoucherPrefix, db => db.SupplierPayments, p => p.VoucherNumber),
                    SupplierId = supplierId,
                    Amount = invoice.PaidAmount,
                    PaymentMethod = method,
                    PurchaseInvoiceId = invoice.Id,
                    Notes = "دفعة عند إنشاء الفاتورة",
                    CreatedByUserId = userId
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await _audit.LogAsync("Create", $"إنشاء فاتورة شراء {invoice.InvoiceNumber}", "PurchaseInvoice", invoice.Id);
            return ServiceResult<PurchaseInvoice>.Ok(invoice, "تم حفظ فاتورة الشراء بنجاح");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ServiceResult> CancelAsync(int invoiceId, string reason, int userId)
    {
        var invoice = await _db.PurchaseInvoices.Include(i => i.Items).ThenInclude(it => it.SerialNumbers)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice is null) return ServiceResult.Fail("الفاتورة غير موجودة");
        if (invoice.Status == DocumentStatus.Cancelled) return ServiceResult.Fail("الفاتورة ملغاة بالفعل");

        var settings = await _settings.GetApplicationSettingsAsync();

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in invoice.Items)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    var soldSerials = item.SerialNumbers.Any(s => s.Status == SerialStatus.Sold);
                    if (soldSerials)
                    {
                        await tx.RollbackAsync();
                        return ServiceResult.Fail($"لا يمكن إلغاء الفاتورة لأن بعض القطع من {product.Name} تم بيعها بالفعل");
                    }

                    var result = await _inventory.AdjustStockAsync(product, -item.Quantity, InventoryTransactionType.InvoiceCancellation,
                        "PurchaseInvoiceCancel", invoice.Id, userId, settings.AllowNegativeStock);
                    if (!result.Success) { await tx.RollbackAsync(); return ServiceResult.Fail(result.Message); }
                }
                _db.ProductSerialNumbers.RemoveRange(item.SerialNumbers);
            }

            invoice.Status = DocumentStatus.Cancelled;
            invoice.CancelledAt = DateTime.Now;
            invoice.CancelledByUserId = userId;
            invoice.CancelReason = reason;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await _audit.LogAsync("Cancel", $"إلغاء فاتورة شراء {invoice.InvoiceNumber} - السبب: {reason}", "PurchaseInvoice", invoice.Id);
            return ServiceResult.Ok("تم إلغاء فاتورة الشراء وتحديث المخزون");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
