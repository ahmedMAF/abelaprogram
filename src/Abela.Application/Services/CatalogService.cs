using Abela.Application.Common;
using Abela.Domain.Entities;
using Abela.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abela.Application.Services;

public class CategoryService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    public CategoryService(AppDbContext db, AuditService audit) { _db = db; _audit = audit; }

    public Task<List<ProductCategory>> GetAllAsync() => _db.ProductCategories.OrderBy(c => c.Name).ToListAsync();

    public async Task<ServiceResult> SaveAsync(ProductCategory category)
    {
        if (string.IsNullOrWhiteSpace(category.Name)) return ServiceResult.Fail("اسم التصنيف مطلوب");
        if (category.Id == 0) { _db.ProductCategories.Add(category); await _audit.LogAsync("Create", $"إضافة تصنيف {category.Name}"); }
        else { category.UpdatedAt = DateTime.Now; _db.ProductCategories.Update(category); await _audit.LogAsync("Update", $"تعديل تصنيف {category.Name}"); }
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var used = await _db.Products.AnyAsync(p => p.CategoryId == id);
        if (used) return ServiceResult.Fail("لا يمكن حذف التصنيف لارتباطه بمنتجات");
        var cat = await _db.ProductCategories.FindAsync(id);
        if (cat is null) return ServiceResult.Fail("التصنيف غير موجود");
        _db.ProductCategories.Remove(cat);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Delete", $"حذف تصنيف {cat.Name}");
        return ServiceResult.Ok();
    }
}

public class UnitService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    public UnitService(AppDbContext db, AuditService audit) { _db = db; _audit = audit; }

    public Task<List<Unit>> GetAllAsync() => _db.Units.OrderBy(u => u.Name).ToListAsync();

    public async Task<ServiceResult> SaveAsync(Unit unit)
    {
        if (string.IsNullOrWhiteSpace(unit.Name)) return ServiceResult.Fail("اسم الوحدة مطلوب");
        if (unit.Id == 0) { _db.Units.Add(unit); await _audit.LogAsync("Create", $"إضافة وحدة {unit.Name}"); }
        else { unit.UpdatedAt = DateTime.Now; _db.Units.Update(unit); await _audit.LogAsync("Update", $"تعديل وحدة {unit.Name}"); }
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var used = await _db.Products.AnyAsync(p => p.UnitId == id);
        if (used) return ServiceResult.Fail("لا يمكن حذف الوحدة لارتباطها بمنتجات");
        var unit = await _db.Units.FindAsync(id);
        if (unit is null) return ServiceResult.Fail("الوحدة غير موجودة");
        _db.Units.Remove(unit);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Delete", $"حذف وحدة {unit.Name}");
        return ServiceResult.Ok();
    }
}

public class ProductService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    public ProductService(AppDbContext db, AuditService audit) { _db = db; _audit = audit; }

    public async Task<List<Product>> SearchAsync(string? term = null, int? categoryId = null, bool lowStockOnly = false)
    {
        var q = _db.Products.Include(p => p.Category).Include(p => p.Unit).AsQueryable();
        if (!string.IsNullOrWhiteSpace(term))
            q = q.Where(p => p.Name.Contains(term) || p.Code.Contains(term) || (p.Barcode != null && p.Barcode.Contains(term)));
        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId);
        if (lowStockOnly) q = q.Where(p => p.CurrentQuantity <= (p.MinStockLevel > 0 ? p.MinStockLevel : 3));
        return await q.OrderBy(p => p.Name).ToListAsync();
    }

    public Task<Product?> GetByIdAsync(int id) => _db.Products.Include(p => p.Category).Include(p => p.Unit)
        .Include(p => p.SerialNumbers).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<string> GenerateCodeAsync()
    {
        var max = await _db.Products.Select(p => p.Code).ToListAsync();
        int maxNum = 0;
        foreach (var c in max)
            if (c.StartsWith("P-") && int.TryParse(c[2..], out var n) && n > maxNum) maxNum = n;
        return "P-" + (maxNum + 1).ToString().PadLeft(5, '0');
    }

    public async Task<ServiceResult> SaveAsync(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name)) return ServiceResult.Fail("اسم المنتج مطلوب");
        if (string.IsNullOrWhiteSpace(product.Code)) return ServiceResult.Fail("كود المنتج مطلوب");
        if (product.SalePrice < 0 || product.PurchasePrice < 0) return ServiceResult.Fail("لا يمكن إدخال أسعار سالبة");

        var duplicateCode = await _db.Products.AnyAsync(p => p.Code == product.Code && p.Id != product.Id);
        if (duplicateCode) return ServiceResult.Fail("كود المنتج مستخدم بالفعل");

        if (product.Id == 0)
        {
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            if (product.CurrentQuantity > 0)
            {
                _db.InventoryTransactions.Add(new Domain.Entities.InventoryTransaction
                {
                    ProductId = product.Id,
                    TransactionType = Domain.Enums.InventoryTransactionType.OpeningBalance,
                    QuantityBefore = 0,
                    QuantityChange = product.CurrentQuantity,
                    QuantityAfter = product.CurrentQuantity,
                    ReferenceType = "OpeningBalance",
                    UserId = 0
                });
                await _db.SaveChangesAsync();
            }
            await _audit.LogAsync("Create", $"إضافة منتج {product.Name}", "Product", product.Id);
        }
        else
        {
            // product is already tracked by this context (loaded via GetByIdAsync in the same scope);
            // calling Update() here would cascade-mark the whole navigation graph (including SerialNumbers) as Modified.
            product.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            await _audit.LogAsync("Update", $"تعديل منتج {product.Name}", "Product", product.Id);
        }
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeactivateAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p is null) return ServiceResult.Fail("المنتج غير موجود");
        p.IsActive = false;
        p.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Update", $"تعطيل منتج {p.Name}", "Product", p.Id);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var used = await _db.SalesInvoiceItems.AnyAsync(i => i.ProductId == id)
                   || await _db.PurchaseInvoiceItems.AnyAsync(i => i.ProductId == id)
                   || await _db.InventoryTransactions.AnyAsync(t => t.ProductId == id)
                   || await _db.StockAdjustmentItems.AnyAsync(i => i.ProductId == id)
                   || await _db.ProductSerialNumbers.AnyAsync(s => s.ProductId == id);
        if (used) return ServiceResult.Fail("لا يمكن حذف المنتج لارتباطه بحركات مخزون أو فواتير، يمكن تعطيله بدلاً من ذلك");
        var p = await _db.Products.FindAsync(id);
        if (p is null) return ServiceResult.Fail("المنتج غير موجود");
        _db.Products.Remove(p);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Delete", $"حذف منتج {p.Name}", "Product", id);
        return ServiceResult.Ok();
    }
}
