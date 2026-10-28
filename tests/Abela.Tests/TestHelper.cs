using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Abela.Tests;

/// <summary>Builds an isolated in-memory SQLite database with seed data for each test.</summary>
public class TestContext : IDisposable
{
    private readonly SqliteConnection _connection;
    public AppDbContext Db { get; }
    public ICurrentUserService CurrentUser { get; } = new CurrentUserService();

    public AuditService Audit { get; }
    public NumberSequenceService Sequence { get; }
    public ProductService Products { get; }
    public CategoryService Categories { get; }
    public UnitService Units { get; }
    public CustomerService Customers { get; }
    public SupplierService Suppliers { get; }
    public SettingsService Settings { get; }
    public InventoryService Inventory { get; }
    public SalesService Sales { get; }
    public PurchaseService Purchases { get; }
    public SalesReturnService SalesReturns { get; }
    public PurchaseReturnService PurchaseReturns { get; }
    public PaymentService Payments { get; }
    public ExpenseService Expenses { get; }
    public DashboardService Dashboard { get; }

    public TestContext()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        Db = new AppDbContext(options);
        DbInitializer.InitializeAsync(Db).GetAwaiter().GetResult();

        CurrentUser.SetUser(1, "Test User", "مدير", new[] { "*" });

        Audit = new AuditService(Db, CurrentUser);
        Sequence = new NumberSequenceService(Db);
        Categories = new CategoryService(Db, Audit);
        Units = new UnitService(Db, Audit);
        Products = new ProductService(Db, Audit);
        Customers = new CustomerService(Db, Audit);
        Suppliers = new SupplierService(Db, Audit);
        Settings = new SettingsService(Db, Audit);
        Inventory = new InventoryService(Db, Audit, Sequence);
        Sales = new SalesService(Db, Audit, Sequence, Inventory, Settings);
        Purchases = new PurchaseService(Db, Audit, Sequence, Inventory, Settings);
        SalesReturns = new SalesReturnService(Db, Audit, Sequence, Inventory);
        PurchaseReturns = new PurchaseReturnService(Db, Audit, Sequence, Inventory, Settings);
        Payments = new PaymentService(Db, Audit, Sequence, Settings);
        Expenses = new ExpenseService(Db, Audit, Sequence, Settings);
        Dashboard = new DashboardService(Db, Customers, Suppliers);
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
