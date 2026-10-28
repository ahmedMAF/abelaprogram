using Abela.Application.Common;
using Abela.Domain.Entities;
using Abela.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Abela.Tests;

public class PaymentAndCalculationTests
{
    [Fact]
    public async Task CustomerPayment_ReducesRemainingAmountOnLinkedInvoice()
    {
        using var ctx = new TestContext();
        var category = await ctx.Db.ProductCategories.FirstAsync();
        var unit = await ctx.Db.Units.FirstAsync();
        var product = new Product { Code = "P-PAY-1", Name = "منتج", CategoryId = category.Id, UnitId = unit.Id, PurchasePrice = 10, SalePrice = 50, CurrentQuantity = 10 };
        await ctx.Products.SaveAsync(product);
        var customer = await ctx.Db.Customers.FirstAsync(c => c.IsDefault);

        var lines = new List<SalesLineInput> { new() { ProductId = product.Id, Quantity = 2, UnitPrice = 50 } };
        var invoice = await ctx.Sales.CreateAsync(customer.Id, SaleType.Credit, PaymentMethod.Credit, 0, 0, null, lines, 1);
        Assert.Equal(100, invoice.Data!.RemainingAmount);

        var payment = await ctx.Payments.AddCustomerPaymentAsync(customer.Id, 60, PaymentMethod.Cash, null, invoice.Data.Id, null, 1);
        Assert.True(payment.Success);

        var updatedInvoice = await ctx.Db.SalesInvoices.FindAsync(invoice.Data.Id);
        Assert.Equal(40, updatedInvoice!.RemainingAmount);
    }

    [Fact]
    public async Task Payment_RejectsZeroOrNegativeAmount()
    {
        using var ctx = new TestContext();
        var customer = await ctx.Db.Customers.FirstAsync(c => c.IsDefault);
        var result = await ctx.Payments.AddCustomerPaymentAsync(customer.Id, 0, PaymentMethod.Cash, null, null, null, 1);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Expense_RequiresPositiveAmountAndDescription()
    {
        using var ctx = new TestContext();
        var category = (await ctx.Settings.GetExpenseCategoriesAsync()).First();

        var badResult = await ctx.Expenses.AddAsync(new Domain.Entities.Expense { ExpenseCategoryId = category.Id, Description = "", Amount = 100, CreatedByUserId = 1 });
        Assert.False(badResult.Success);

        var goodResult = await ctx.Expenses.AddAsync(new Domain.Entities.Expense { ExpenseCategoryId = category.Id, Description = "إيجار المحل", Amount = 500, CreatedByUserId = 1 });
        Assert.True(goodResult.Success);
    }

    [Fact]
    public async Task SalesInvoice_TotalsAccountForDiscountAndMultipleLines()
    {
        using var ctx = new TestContext();
        var category = await ctx.Db.ProductCategories.FirstAsync();
        var unit = await ctx.Db.Units.FirstAsync();
        var p1 = new Product { Code = "P-CALC-1", Name = "صنف 1", CategoryId = category.Id, UnitId = unit.Id, PurchasePrice = 10, SalePrice = 30, CurrentQuantity = 10 };
        var p2 = new Product { Code = "P-CALC-2", Name = "صنف 2", CategoryId = category.Id, UnitId = unit.Id, PurchasePrice = 20, SalePrice = 60, CurrentQuantity = 10 };
        await ctx.Products.SaveAsync(p1);
        await ctx.Products.SaveAsync(p2);
        var customer = await ctx.Db.Customers.FirstAsync(c => c.IsDefault);

        var lines = new List<SalesLineInput>
        {
            new() { ProductId = p1.Id, Quantity = 2, UnitPrice = 30, DiscountAmount = 5 }, // 60 - 5 = 55
            new() { ProductId = p2.Id, Quantity = 1, UnitPrice = 60 } // 60
        };
        // subtotal = 2*30 + 1*60 = 120, line discounts = 5, overall discount = 10 => total = 105
        var result = await ctx.Sales.CreateAsync(customer.Id, SaleType.Cash, PaymentMethod.Cash, discountAmount: 10, paidAmount: 105, null, lines, 1);

        Assert.True(result.Success);
        Assert.Equal(120, result.Data!.SubTotal);
        Assert.Equal(10, result.Data.DiscountAmount);
        Assert.Equal(105, result.Data.TotalAmount);
        Assert.Equal(0, result.Data.RemainingAmount);
    }
}
