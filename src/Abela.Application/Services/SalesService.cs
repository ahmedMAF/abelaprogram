using Abela.Application.Common;
using Abela.Domain.Entities;
using Abela.Domain.Enums;
using Abela.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abela.Application.Services;

public class SalesService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly NumberSequenceService _seq;
    private readonly InventoryService _inventory;
    private readonly SettingsService _settings;

    public SalesService(AppDbContext db, AuditService audit, NumberSequenceService seq, InventoryService inventory, SettingsService settings)
    {
        _db = db; _audit = audit; _seq = seq; _inventory = inventory; _settings = settings;
    }

    public async Task<List<SalesInvoice>> SearchAsync(DateTime? from = null, DateTime? to = null, int? customerId = null, DocumentStatus? status = null)
    {
        var q = _db.SalesInvoices.Include(i => i.Customer).AsQueryable();
        if (from.HasValue) q = q.Where(i => i.InvoiceDate >= from);
        if (to.HasValue) q = q.Where(i => i.InvoiceDate <= to);
        if (customerId.HasValue) q = q.Where(i => i.CustomerId == customerId);
        if (status.HasValue) q = q.Where(i => i.Status == status);
        return await q.OrderByDescending(i => i.InvoiceDate).ToListAsync();
    }

    public Task<SalesInvoice?> GetByIdAsync(int id) => _db.SalesInvoices
        .Include(i => i.Customer)
        .Include(i => i.Items).ThenInclude(x => x.Product)
        .Include(i => i.Items).ThenInclude(x => x.SerialNumbers)
        .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<ServiceResult<SalesInvoice>> CreateAsync(int customerId, SaleType saleType, PaymentMethod method,
        decimal discountAmount, decimal paidAmount, string? notes, List<SalesLineInput> lines, int userId)
    {
        if (lines.Count == 0) return ServiceResult<SalesInvoice>.Fail("لا يمكن حفظ فاتورة بدون أصناف");

        var settings = await _settings.GetApplicationSettingsAsync();

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var invoice = new SalesInvoice
            {
                InvoiceNumber = await _seq.NextAsync(settings.SalesInvoicePrefix, db => db.SalesInvoices, i => i.InvoiceNumber),
                CustomerId = customerId,
                SaleType = saleType,
                PaymentMethod = method,
                Notes = notes,
                CreatedByUserId = userId,
                Status = DocumentStatus.Active
            };
            _db.SalesInvoices.Add(invoice);
            await _db.SaveChangesAsync();

            decimal subTotal = 0, taxTotal = 0, lineDiscountTotal = 0;
            foreach (var line in lines)
            {
                if (line.Quantity <= 0) { await tx.RollbackAsync(); return ServiceResult<SalesInvoice>.Fail("يجب أن تكون الكمية أكبر من صفر"); }
                var product = await _db.Products.FindAsync(line.ProductId);
                if (product is null) { await tx.RollbackAsync(); return ServiceResult<SalesInvoice>.Fail("منتج غير موجود"); }

                if (product.RequiresSerialNumber && line.SerialNumbers.Count != (int)line.Quantity)
                {
                    await tx.RollbackAsync();
                    return ServiceResult<SalesInvoice>.Fail($"يجب إدخال الرقم التسلسلي لكل قطعة من {product.Name}");
                }

                var lineTotal = (line.Quantity * line.UnitPrice) - line.DiscountAmount + line.TaxAmount;
                var item = new SalesInvoiceItem
                {
                    SalesInvoiceId = invoice.Id,
                    ProductId = product.Id,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    DiscountAmount = line.DiscountAmount,
                    TaxAmount = line.TaxAmount,
                    LineTotal = lineTotal
                };
                _db.SalesInvoiceItems.Add(item);
                await _db.SaveChangesAsync();

                var stockResult = await _inventory.AdjustStockAsync(product, -line.Quantity, InventoryTransactionType.Sale,
                    "SalesInvoice", invoice.Id, userId, settings.AllowNegativeStock);
                if (!stockResult.Success) { await tx.RollbackAsync(); return ServiceResult<SalesInvoice>.Fail(stockResult.Message); }

                if (product.RequiresSerialNumber)
                {
                    foreach (var sn in line.SerialNumbers)
                    {
                        var serial = await _db.ProductSerialNumbers.FirstOrDefaultAsync(s => s.ProductId == product.Id && s.SerialNumber == sn);
                        if (serial is null || serial.Status == SerialStatus.Sold)
                        {
                            await tx.RollbackAsync();
                            return ServiceResult<SalesInvoice>.Fail($"الرقم التسلسلي {sn} غير متوفر أو تم بيعه مسبقًا");
                        }
                        serial.Status = SerialStatus.Sold;
                        serial.SalesInvoiceItemId = item.Id;
                    }
                }

                subTotal += line.Quantity * line.UnitPrice;
                lineDiscountTotal += line.DiscountAmount;
                taxTotal += line.TaxAmount;
            }

            invoice.SubTotal = subTotal;
            invoice.DiscountAmount = discountAmount;
            invoice.TaxAmount = taxTotal;
            invoice.TotalAmount = subTotal - lineDiscountTotal - discountAmount + taxTotal;
            invoice.PaidAmount = Math.Min(paidAmount, invoice.TotalAmount < 0 ? 0 : paidAmount);
            invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;

            if (invoice.PaidAmount > 0)
            {
                _db.CustomerPayments.Add(new CustomerPayment
                {
                    ReceiptNumber = await _seq.NextAsync(settings.ReceiptVoucherPrefix, db => db.CustomerPayments, p => p.ReceiptNumber),
                    CustomerId = customerId,
                    Amount = invoice.PaidAmount,
                    PaymentMethod = method,
                    SalesInvoiceId = invoice.Id,
                    Notes = "دفعة عند إنشاء الفاتورة",
                    CreatedByUserId = userId
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await _audit.LogAsync("Create", $"إنشاء فاتورة بيع {invoice.InvoiceNumber}", "SalesInvoice", invoice.Id);
            return ServiceResult<SalesInvoice>.Ok(invoice, "تم حفظ الفاتورة بنجاح");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ServiceResult> CancelAsync(int invoiceId, string reason, int userId)
    {
        var invoice = await _db.SalesInvoices.Include(i => i.Items).ThenInclude(it => it.SerialNumbers)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice is null) return ServiceResult.Fail("الفاتورة غير موجودة");
        if (invoice.Status == DocumentStatus.Cancelled) return ServiceResult.Fail("الفاتورة ملغاة بالفعل");

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in invoice.Items)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    await _inventory.AdjustStockAsync(product, item.Quantity, InventoryTransactionType.InvoiceCancellation,
                        "SalesInvoiceCancel", invoice.Id, userId, allowNegativeStock: true);
                }
                foreach (var serial in item.SerialNumbers)
                    serial.Status = SerialStatus.InStock;
            }

            invoice.Status = DocumentStatus.Cancelled;
            invoice.CancelledAt = DateTime.Now;
            invoice.CancelledByUserId = userId;
            invoice.CancelReason = reason;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await _audit.LogAsync("Cancel", $"إلغاء فاتورة بيع {invoice.InvoiceNumber} - السبب: {reason}", "SalesInvoice", invoice.Id);
            return ServiceResult.Ok("تم إلغاء الفاتورة وإعادة الكميات للمخزون");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
