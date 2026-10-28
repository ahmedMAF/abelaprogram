using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using Abela.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Abela.Tests;

public class SalesServiceTests
{
    private static async Task<(Product product, int customerId)> SeedProductAndCustomerAsync(TestContext ctx, decimal quantity = 10, decimal salePrice = 100, bool requiresSerial = false)
    {
        var category = await ctx.Db.ProductCategories.FirstAsync();
        var unit = await ctx.Db.Units.FirstAsync();
        var product = new Product
        {
            Code = "P-TEST-" + Guid.NewGuid().ToString("N")[..6],
            Name = "منتج اختبار",
            CategoryId = category.Id,
            UnitId = unit.Id,
            PurchasePrice = 50,
            SalePrice = salePrice,
            CurrentQuantity = quantity,
            RequiresSerialNumber = requiresSerial
        };
        await ctx.Products.SaveAsync(product);

        var customer = await ctx.Db.Customers.FirstAsync(c => c.IsDefault);
        return (product, customer.Id);
    }

    [Fact]
    public async Task CreateSale_ReducesStock_AndComputesTotals()
    {
        using var ctx = new TestContext();
        var (product, customerId) = await SeedProductAndCustomerAsync(ctx, quantity: 10, salePrice: 100);

        var lines = new List<SalesLineInput> { new() { ProductId = product.Id, Quantity = 3, UnitPrice = 100, DiscountAmount = 10 } };
        var result = await ctx.Sales.CreateAsync(customerId, SaleType.Cash, PaymentMethod.Cash, discountAmount: 0, paidAmount: 290, notes: null, lines, userId: 1);

        Assert.True(result.Success);
        Assert.Equal(290, result.Data!.TotalAmount);
        Assert.Equal(0, result.Data!.RemainingAmount);

        var updatedProduct = await ctx.Db.Products.FindAsync(product.Id);
        Assert.Equal(7, updatedProduct!.CurrentQuantity);
    }

    [Fact]
    public async Task CreateSale_RejectsWhenStockInsufficientAndNegativeStockDisallowed()
    {
        using var ctx = new TestContext();
        var (product, customerId) = await SeedProductAndCustomerAsync(ctx, quantity: 2);

        var lines = new List<SalesLineInput> { new() { ProductId = product.Id, Quantity = 5, UnitPrice = 100 } };
        var result = await ctx.Sales.CreateAsync(customerId, SaleType.Cash, PaymentMethod.Cash, 0, 0, null, lines, 1);

        Assert.False(result.Success);
        var unchanged = await ctx.Db.Products.FindAsync(product.Id);
        Assert.Equal(2, unchanged!.CurrentQuantity);
    }

    [Fact]
    public async Task CancelSale_RestoresStock()
    {
        using var ctx = new TestContext();
        var (product, customerId) = await SeedProductAndCustomerAsync(ctx, quantity: 10);

        var lines = new List<SalesLineInput> { new() { ProductId = product.Id, Quantity = 4, UnitPrice = 100 } };
        var created = await ctx.Sales.CreateAsync(customerId, SaleType.Cash, PaymentMethod.Cash, 0, 400, null, lines, 1);
        Assert.True(created.Success);

        var cancelResult = await ctx.Sales.CancelAsync(created.Data!.Id, "خطأ في الفاتورة", 1);
        Assert.True(cancelResult.Success);

        var restored = await ctx.Db.Products.FindAsync(product.Id);
        Assert.Equal(10, restored!.CurrentQuantity);
    }

    [Fact]
    public async Task CreateSale_WithPartialPayment_CalculatesRemainingAsCustomerDebt()
    {
        using var ctx = new TestContext();
        var (product, customerId) = await SeedProductAndCustomerAsync(ctx, quantity: 10, salePrice: 100);

        var lines = new List<SalesLineInput> { new() { ProductId = product.Id, Quantity = 2, UnitPrice = 100 } };
        var result = await ctx.Sales.CreateAsync(customerId, SaleType.Partial, PaymentMethod.Cash, 0, 50, null, lines, 1);

        Assert.True(result.Success);
        Assert.Equal(200, result.Data!.TotalAmount);
        Assert.Equal(50, result.Data!.PaidAmount);
        Assert.Equal(150, result.Data!.RemainingAmount);

        var balance = await ctx.Customers.GetBalanceAsync(customerId);
        Assert.Equal(150, balance);
    }

    [Fact]
    public async Task CreateSale_RequiresSerialNumber_RejectsSaleOfAlreadySoldSerial()
    {
        using var ctx = new TestContext();
        var (product, customerId) = await SeedProductAndCustomerAsync(ctx, quantity: 0, requiresSerial: true);

        // Purchase in 2 serialized units first
        var supplier = new Supplier { Code = "S-TEST", Name = "مورد اختبار" };
        ctx.Db.Suppliers.Add(supplier);
        await ctx.Db.SaveChangesAsync();

        var purchaseLines = new List<PurchaseLineInput>
        {
            new() { ProductId = product.Id, Quantity = 2, UnitCost = 50, SerialNumbers = new List<string> { "SN-001", "SN-002" } }
        };
        var purchase = await ctx.Purchases.CreateAsync(supplier.Id, null, PaymentMethod.Cash, 0, 0, 100, null, purchaseLines, 1);
        Assert.True(purchase.Success);

        var saleLines = new List<SalesLineInput>
        {
            new() { ProductId = product.Id, Quantity = 1, UnitPrice = 100, SerialNumbers = new List<string> { "SN-001" } }
        };
        var firstSale = await ctx.Sales.CreateAsync(customerId, SaleType.Cash, PaymentMethod.Cash, 0, 100, null, saleLines, 1);
        Assert.True(firstSale.Success);

        // Attempt to sell the same serial again
        var secondSale = await ctx.Sales.CreateAsync(customerId, SaleType.Cash, PaymentMethod.Cash, 0, 100, null, saleLines, 1);
        Assert.False(secondSale.Success);
    }

    [Fact]
    public async Task SalesReturn_ReducesCustomerBalance_RegardlessOfRefundMethod()
    {
        using var ctx = new TestContext();
        var (product, customerId) = await SeedProductAndCustomerAsync(ctx, quantity: 10, salePrice: 100);

        var lines = new List<SalesLineInput> { new() { ProductId = product.Id, Quantity = 5, UnitPrice = 100 } };
        var sale = await ctx.Sales.CreateAsync(customerId, SaleType.Credit, PaymentMethod.Credit, 0, 0, null, lines, 1);
        Assert.True(sale.Success);

        var balanceBeforeReturn = await ctx.Customers.GetBalanceAsync(customerId);
        Assert.Equal(500, balanceBeforeReturn); // fully unpaid credit sale

        var saleItemId = sale.Data!.Items.First().Id;
        var returnResult = await ctx.SalesReturns.CreateAsync(sale.Data.Id, RefundMethod.CashRefund,
            new List<SalesReturnLineInput> { new() { SalesInvoiceItemId = saleItemId, Quantity = 2, Reason = "عيب تصنيع" } }, null, 1);
        Assert.True(returnResult.Success);

        var balanceAfterReturn = await ctx.Customers.GetBalanceAsync(customerId);
        Assert.Equal(300, balanceAfterReturn); // 500 - (2 * 100)
    }

    [Fact]
    public async Task DashboardCustomerDebts_ReflectsSalesReturn()
    {
        using var ctx = new TestContext();
        var (product, customerId) = await SeedProductAndCustomerAsync(ctx, quantity: 10, salePrice: 100);

        var lines = new List<SalesLineInput> { new() { ProductId = product.Id, Quantity = 5, UnitPrice = 100 } };
        var sale = await ctx.Sales.CreateAsync(customerId, SaleType.Credit, PaymentMethod.Credit, 0, 0, null, lines, 1);
        var saleItemId = sale.Data!.Items.First().Id;

        await ctx.SalesReturns.CreateAsync(sale.Data.Id, RefundMethod.DeductFromBalance,
            new List<SalesReturnLineInput> { new() { SalesInvoiceItemId = saleItemId, Quantity = 3 } }, null, 1);

        var summary = await ctx.Dashboard.GetSummaryAsync();
        Assert.Equal(200, summary.CustomerDebts); // 500 - (3 * 100)
    }
}
