using Abela.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Abela.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductSerialNumber> ProductSerialNumbers => Set<ProductSerialNumber>();

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<SalesReturnItem> SalesReturnItems => Set<SalesReturnItem>();

    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems => Set<PurchaseInvoiceItem>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();

    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();

    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockAdjustmentItem> StockAdjustmentItems => Set<StockAdjustmentItem>();

    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<ApplicationSettings> ApplicationSettings => Set<ApplicationSettings>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        foreach (var property in b.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }

        // Security
        b.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        b.Entity<Permission>().HasIndex(x => x.Code).IsUnique();
        b.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });
        b.Entity<RolePermission>().HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<RolePermission>().HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<User>().HasIndex(x => x.Username).IsUnique();
        b.Entity<User>().HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);

        // Catalog
        b.Entity<Product>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Product>().HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Product>().HasOne(x => x.Unit).WithMany(x => x.Products).HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProductSerialNumber>().HasIndex(x => new { x.ProductId, x.SerialNumber }).IsUnique();
        b.Entity<ProductSerialNumber>().HasOne(x => x.Product).WithMany(x => x.SerialNumbers).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProductSerialNumber>().HasOne(x => x.PurchaseInvoiceItem).WithMany(x => x.SerialNumbers).HasForeignKey(x => x.PurchaseInvoiceItemId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProductSerialNumber>().HasOne(x => x.SalesInvoiceItem).WithMany(x => x.SerialNumbers).HasForeignKey(x => x.SalesInvoiceItemId).OnDelete(DeleteBehavior.Restrict);

        // Parties
        b.Entity<Customer>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Supplier>().HasIndex(x => x.Code).IsUnique();

        // Sales
        b.Entity<SalesInvoice>().HasIndex(x => x.InvoiceNumber).IsUnique();
        b.Entity<SalesInvoice>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SalesInvoiceItem>().HasOne(x => x.SalesInvoice).WithMany(x => x.Items).HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<SalesInvoiceItem>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<SalesReturn>().HasIndex(x => x.ReturnNumber).IsUnique();
        b.Entity<SalesReturn>().HasOne(x => x.SalesInvoice).WithMany().HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SalesReturn>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SalesReturnItem>().HasOne(x => x.SalesReturn).WithMany(x => x.Items).HasForeignKey(x => x.SalesReturnId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<SalesReturnItem>().HasOne(x => x.SalesInvoiceItem).WithMany().HasForeignKey(x => x.SalesInvoiceItemId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SalesReturnItem>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        // Purchases
        b.Entity<PurchaseInvoice>().HasIndex(x => x.InvoiceNumber).IsUnique();
        b.Entity<PurchaseInvoice>().HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PurchaseInvoiceItem>().HasOne(x => x.PurchaseInvoice).WithMany(x => x.Items).HasForeignKey(x => x.PurchaseInvoiceId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<PurchaseInvoiceItem>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<PurchaseReturn>().HasIndex(x => x.ReturnNumber).IsUnique();
        b.Entity<PurchaseReturn>().HasOne(x => x.PurchaseInvoice).WithMany().HasForeignKey(x => x.PurchaseInvoiceId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PurchaseReturn>().HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PurchaseReturnItem>().HasOne(x => x.PurchaseReturn).WithMany(x => x.Items).HasForeignKey(x => x.PurchaseReturnId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<PurchaseReturnItem>().HasOne(x => x.PurchaseInvoiceItem).WithMany().HasForeignKey(x => x.PurchaseInvoiceItemId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PurchaseReturnItem>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        // Finance
        b.Entity<CustomerPayment>().HasIndex(x => x.ReceiptNumber).IsUnique();
        b.Entity<CustomerPayment>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CustomerPayment>().HasOne(x => x.SalesInvoice).WithMany(x => x.Payments).HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<SupplierPayment>().HasIndex(x => x.VoucherNumber).IsUnique();
        b.Entity<SupplierPayment>().HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SupplierPayment>().HasOne(x => x.PurchaseInvoice).WithMany(x => x.Payments).HasForeignKey(x => x.PurchaseInvoiceId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Expense>().HasIndex(x => x.ExpenseNumber).IsUnique();
        b.Entity<Expense>().HasOne(x => x.ExpenseCategory).WithMany().HasForeignKey(x => x.ExpenseCategoryId).OnDelete(DeleteBehavior.Restrict);

        // Inventory
        b.Entity<InventoryTransaction>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StockAdjustment>().HasIndex(x => x.AdjustmentNumber).IsUnique();
        b.Entity<StockAdjustmentItem>().HasOne(x => x.StockAdjustment).WithMany(x => x.Items).HasForeignKey(x => x.StockAdjustmentId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<StockAdjustmentItem>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        // Global query filters for soft delete
        b.Entity<Product>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Customer>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Supplier>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<ProductCategory>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Unit>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<User>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<ExpenseCategory>().HasQueryFilter(x => !x.IsDeleted);
    }
}
