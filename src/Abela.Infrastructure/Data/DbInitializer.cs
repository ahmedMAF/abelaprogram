using Abela.Domain.Common;
using Abela.Domain.Entities;

namespace Abela.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (!db.Permissions.Any())
        {
            foreach (var (code, name, module) in PermissionCodes.All)
            {
                db.Permissions.Add(new Permission { Code = code, Name = name, Module = module });
            }
            await db.SaveChangesAsync();
        }

        Role? adminRole = db.Roles.FirstOrDefault(r => r.Name == "مدير");
        if (adminRole is null)
        {
            adminRole = new Role { Name = "مدير", IsSystem = true };
            db.Roles.Add(adminRole);
            await db.SaveChangesAsync();

            foreach (var p in db.Permissions)
                db.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionId = p.Id });
            await db.SaveChangesAsync();
        }

        if (!db.Roles.Any(r => r.Name == "موظف مبيعات"))
        {
            var salesRole = new Role { Name = "موظف مبيعات", IsSystem = true };
            db.Roles.Add(salesRole);
            await db.SaveChangesAsync();

            var codes = new[] { PermissionCodes.SalesView, PermissionCodes.SalesAdd, PermissionCodes.SalesPrint,
                PermissionCodes.ProductsView, PermissionCodes.CustomersView, PermissionCodes.CustomersAdd,
                PermissionCodes.PaymentsView, PermissionCodes.PaymentsAdd, PermissionCodes.ReportsView };
            foreach (var p in db.Permissions.Where(p => codes.Contains(p.Code)))
                db.RolePermissions.Add(new RolePermission { RoleId = salesRole.Id, PermissionId = p.Id });
            await db.SaveChangesAsync();
        }

        if (!db.Roles.Any(r => r.Name == "موظف مخزن"))
        {
            var stockRole = new Role { Name = "موظف مخزن", IsSystem = true };
            db.Roles.Add(stockRole);
            await db.SaveChangesAsync();

            var codes = new[] { PermissionCodes.ProductsView, PermissionCodes.InventoryView, PermissionCodes.InventoryAdjust,
                PermissionCodes.PurchasesView, PermissionCodes.PurchasesAdd, PermissionCodes.SuppliersView };
            foreach (var p in db.Permissions.Where(p => codes.Contains(p.Code)))
                db.RolePermissions.Add(new RolePermission { RoleId = stockRole.Id, PermissionId = p.Id });
            await db.SaveChangesAsync();
        }

        if (!db.Users.Any())
        {
            db.Users.Add(new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                FullName = "مدير النظام",
                RoleId = adminRole.Id,
                MustChangePassword = true,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        if (!db.Units.Any())
        {
            db.Units.AddRange(
                new Unit { Name = "قطعة" },
                new Unit { Name = "جهاز" },
                new Unit { Name = "ماكينة" },
                new Unit { Name = "متر" },
                new Unit { Name = "صندوق" },
                new Unit { Name = "حزمة" });
            await db.SaveChangesAsync();
        }

        if (!db.ProductCategories.Any())
        {
            db.ProductCategories.AddRange(
                new ProductCategory { Name = "ماكينات CNC" },
                new ProductCategory { Name = "إلكترونيات" },
                new ProductCategory { Name = "قطع غيار" },
                new ProductCategory { Name = "إكسسوارات" },
                new ProductCategory { Name = "معدات" },
                new ProductCategory { Name = "أخرى" });
            await db.SaveChangesAsync();
        }

        if (!db.Customers.Any(c => c.IsDefault))
        {
            db.Customers.Add(new Customer { Code = "C-0001", Name = "عميل نقدي", IsDefault = true });
            await db.SaveChangesAsync();
        }

        if (!db.ExpenseCategories.Any())
        {
            db.ExpenseCategories.AddRange(
                new ExpenseCategory { Name = "إيجار" },
                new ExpenseCategory { Name = "كهرباء" },
                new ExpenseCategory { Name = "رواتب" },
                new ExpenseCategory { Name = "نقل وشحن" },
                new ExpenseCategory { Name = "صيانة" },
                new ExpenseCategory { Name = "مشتريات مكتبية" },
                new ExpenseCategory { Name = "تسويق" },
                new ExpenseCategory { Name = "إنترنت واتصالات" },
                new ExpenseCategory { Name = "أخرى" });
            await db.SaveChangesAsync();
        }

        if (!db.CompanySettings.Any())
        {
            db.CompanySettings.Add(new CompanySettings
            {
                CompanyName = "Abela",
                Phone = "",
                Address = "",
                InvoiceFooterText = "شكرًا لتعاملكم مع Abela"
            });
            await db.SaveChangesAsync();
        }

        if (!db.ApplicationSettings.Any())
        {
            db.ApplicationSettings.Add(new ApplicationSettings());
            await db.SaveChangesAsync();
        }
    }
}
