using Abela.Application.Common;
using Abela.Domain.Entities;
using Abela.Domain.Enums;
using Abela.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abela.Application.Services;

public class InventoryService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly NumberSequenceService _seq;
    public InventoryService(AppDbContext db, AuditService audit, NumberSequenceService seq) { _db = db; _audit = audit; _seq = seq; }

    /// <summary>Applies a stock change and records a documented inventory transaction. quantityChange may be negative.</summary>
    public async Task<ServiceResult> AdjustStockAsync(Product product, decimal quantityChange, InventoryTransactionType type,
        string referenceType, int? referenceId, int userId, bool allowNegativeStock, string? notes = null)
    {
        var before = product.CurrentQuantity;
        var after = before + quantityChange;
        if (after < 0 && !allowNegativeStock)
            return ServiceResult.Fail($"الكمية المتوفرة من {product.Name} غير كافية لإتمام هذه العملية");

        product.CurrentQuantity = after;
        product.UpdatedAt = DateTime.Now;

        _db.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = product.Id,
            TransactionType = type,
            QuantityBefore = before,
            QuantityChange = quantityChange,
            QuantityAfter = after,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Notes = notes,
            UserId = userId,
            TransactionDate = DateTime.Now
        });

        return ServiceResult.Ok();
    }

    public async Task<List<InventoryTransaction>> GetMovementsAsync(int? productId = null, DateTime? from = null, DateTime? to = null)
    {
        var q = _db.InventoryTransactions.Include(t => t.Product).AsQueryable();
        if (productId.HasValue) q = q.Where(t => t.ProductId == productId);
        if (from.HasValue) q = q.Where(t => t.TransactionDate >= from);
        if (to.HasValue) q = q.Where(t => t.TransactionDate <= to);
        return await q.OrderByDescending(t => t.TransactionDate).ToListAsync();
    }

    public async Task<ServiceResult<StockAdjustment>> CreateStockAdjustmentAsync(List<(int ProductId, decimal ActualQuantity, string? Reason)> lines, string? notes, int userId, bool allowNegativeStock)
    {
        if (lines.Count == 0) return ServiceResult<StockAdjustment>.Fail("لا توجد أصناف للتسوية");

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var number = await _seq.NextAsync("ADJ-", db => db.StockAdjustments, a => a.AdjustmentNumber);
            var adjustment = new StockAdjustment { AdjustmentNumber = number, Notes = notes, CreatedByUserId = userId };
            _db.StockAdjustments.Add(adjustment);
            await _db.SaveChangesAsync();

            foreach (var line in lines)
            {
                var product = await _db.Products.FindAsync(line.ProductId);
                if (product is null) { await tx.RollbackAsync(); return ServiceResult<StockAdjustment>.Fail("منتج غير موجود"); }

                var diff = line.ActualQuantity - product.CurrentQuantity;
                if (diff == 0) continue;

                adjustment.Items.Add(new StockAdjustmentItem
                {
                    StockAdjustmentId = adjustment.Id,
                    ProductId = product.Id,
                    SystemQuantity = product.CurrentQuantity,
                    ActualQuantity = line.ActualQuantity,
                    Difference = diff,
                    Reason = line.Reason
                });

                var type = diff > 0 ? InventoryTransactionType.AdjustmentIncrease : InventoryTransactionType.AdjustmentDecrease;
                var result = await AdjustStockAsync(product, diff, type, "StockAdjustment", adjustment.Id, userId, allowNegativeStock, line.Reason);
                if (!result.Success) { await tx.RollbackAsync(); return ServiceResult<StockAdjustment>.Fail(result.Message); }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await _audit.LogAsync("StockAdjustment", $"تنفيذ جرد وتسوية مخزون {number}", "StockAdjustment", adjustment.Id);
            return ServiceResult<StockAdjustment>.Ok(adjustment, "تم تنفيذ التسوية بنجاح");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
