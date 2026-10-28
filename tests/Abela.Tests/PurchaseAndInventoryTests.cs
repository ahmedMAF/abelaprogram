using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using Abela.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Abela.Tests;

public class PurchaseAndInventoryTests
{
    private static async Task<(Product product, int supplierId)> SeedAsync(TestContext ctx, decimal quantity = 0)
    {
        var category = await ctx.Db.ProductCategories.FirstAsync();
        var unit = await ctx.Db.Units.FirstAsync();
        var product = new Product
        {
            Code = "P-TEST-" + Guid.NewGuid().ToString("N")[..6],
            Name = "منتج شراء",
            CategoryId = category.Id,
            UnitId = unit.Id,
            PurchasePrice = 40,
            SalePrice = 80,
            CurrentQuantity = quantity
        };
        await ctx.Products.SaveAsync(product);

        var supplier = new Supplier { Code = "S-" + Guid.NewGuid().ToString("N")[..6], Name = "مورد اختبار" };
        ctx.Db.Suppliers.Add(supplier);
        await ctx.Db.SaveChangesAsync();

        return (product, supplier.Id);
    }

    [Fact]
    public async Task CreatePurchase_IncreasesStock_AndUpdatesPurchasePrice()
    {
        using var ctx = new TestContext();
        var (product, supplierId) = await SeedAsync(ctx, quantity: 5);

        var lines = new List<PurchaseLineInput> { new() { ProductId = product.Id, Quantity = 10, UnitCost = 45 } };
        var result = await ctx.Purchases.CreateAsync(supplierId, "SUP-INV-1", PaymentMethod.Cash, 0, 20, 400, null, lines, 1);

        Assert.True(result.Success);
        Assert.Equal(470, result.Data!.TotalAmount); // 450 - 0 discount + 20 extra costs

        var updated = await ctx.Db.Products.FindAsync(product.Id);
        Assert.Equal(15, updated!.CurrentQuantity);
        Assert.Equal(45, updated.PurchasePrice);
    }

    [Fact]
    public async Task CancelPurchase_DecreasesStockBack()
    {
        using var ctx = new TestContext();
        var (product, supplierId) = await SeedAsync(ctx, quantity: 0);

        var lines = new List<PurchaseLineInput> { new() { ProductId = product.Id, Quantity = 8, UnitCost = 40 } };
        var purchase = await ctx.Purchases.CreateAsync(supplierId, null, PaymentMethod.Cash, 0, 0, 0, null, lines, 1);
        Assert.True(purchase.Success);

        var cancel = await ctx.Purchases.CancelAsync(purchase.Data!.Id, "خطأ إدخال", 1);
        Assert.True(cancel.Success);

        var after = await ctx.Db.Products.FindAsync(product.Id);
        Assert.Equal(0, after!.CurrentQuantity);
    }

    [Fact]
    public async Task StockAdjustment_AppliesDifferenceAndLogsMovement()
    {
        using var ctx = new TestContext();
        var (product, _) = await SeedAsync(ctx, quantity: 20);

        var lines = new List<(int ProductId, decimal ActualQuantity, string? Reason)> { (product.Id, 17, "جرد نهاية الشهر") };
        var result = await ctx.Inventory.CreateStockAdjustmentAsync(lines, "جرد دوري", 1, allowNegativeStock: false);

        Assert.True(result.Success);
        var updated = await ctx.Db.Products.FindAsync(product.Id);
        Assert.Equal(17, updated!.CurrentQuantity);

        var movements = await ctx.Inventory.GetMovementsAsync(product.Id);
        Assert.Contains(movements, m => m.TransactionType == InventoryTransactionType.AdjustmentDecrease && m.QuantityChange == -3);
    }

    [Fact]
    public async Task PurchaseReturn_CannotExceedPurchasedQuantity()
    {
        using var ctx = new TestContext();
        var (product, supplierId) = await SeedAsync(ctx, quantity: 0);

        var lines = new List<PurchaseLineInput> { new() { ProductId = product.Id, Quantity = 5, UnitCost = 40 } };
        var purchase = await ctx.Purchases.CreateAsync(supplierId, null, PaymentMethod.Cash, 0, 0, 0, null, lines, 1);
        var purchaseItemId = purchase.Data!.Items.First().Id;

        var returnLines = new List<PurchaseReturnLineInput> { new() { PurchaseInvoiceItemId = purchaseItemId, Quantity = 10 } };
        var result = await ctx.PurchaseReturns.CreateAsync(purchase.Data.Id, returnLines, null, 1);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task PurchaseReturn_ReducesSupplierBalance()
    {
        using var ctx = new TestContext();
        var (product, supplierId) = await SeedAsync(ctx, quantity: 0);

        var lines = new List<PurchaseLineInput> { new() { ProductId = product.Id, Quantity = 10, UnitCost = 40 } };
        var purchase = await ctx.Purchases.CreateAsync(supplierId, null, PaymentMethod.Cash, 0, 0, 0, null, lines, 1);
        Assert.True(purchase.Success);

        var balanceBeforeReturn = await ctx.Suppliers.GetBalanceAsync(supplierId);
        Assert.Equal(400, balanceBeforeReturn); // full unpaid purchase owed to supplier

        var purchaseItemId = purchase.Data!.Items.First().Id;
        var returnResult = await ctx.PurchaseReturns.CreateAsync(purchase.Data.Id,
            new List<PurchaseReturnLineInput> { new() { PurchaseInvoiceItemId = purchaseItemId, Quantity = 3 } }, "تالف", 1);
        Assert.True(returnResult.Success);

        var balanceAfterReturn = await ctx.Suppliers.GetBalanceAsync(supplierId);
        Assert.Equal(280, balanceAfterReturn); // 400 - (3 * 40)
    }

    [Fact]
    public async Task DashboardSupplierDebts_ReflectsPurchaseReturn()
    {
        using var ctx = new TestContext();
        var (product, supplierId) = await SeedAsync(ctx, quantity: 0);

        var lines = new List<PurchaseLineInput> { new() { ProductId = product.Id, Quantity = 10, UnitCost = 40 } };
        var purchase = await ctx.Purchases.CreateAsync(supplierId, null, PaymentMethod.Cash, 0, 0, 0, null, lines, 1);
        var purchaseItemId = purchase.Data!.Items.First().Id;

        await ctx.PurchaseReturns.CreateAsync(purchase.Data.Id,
            new List<PurchaseReturnLineInput> { new() { PurchaseInvoiceItemId = purchaseItemId, Quantity = 5 } }, null, 1);

        var summary = await ctx.Dashboard.GetSummaryAsync();
        Assert.Equal(200, summary.SupplierDebts); // 400 - (5 * 40)
    }
}
