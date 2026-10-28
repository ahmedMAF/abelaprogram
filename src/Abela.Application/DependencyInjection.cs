using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Infrastructure.Data;
using Abela.Infrastructure.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Abela.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAbelaServices(this IServiceCollection services, string dbFilePath)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={dbFilePath}"), ServiceLifetime.Scoped);

        services.AddSingleton<ICurrentUserService, CurrentUserService>();

        services.AddScoped<AuditService>();
        services.AddScoped<NumberSequenceService>();
        services.AddScoped<AuthService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<UnitService>();
        services.AddScoped<ProductService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<SupplierService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<SettingsService>();
        services.AddScoped<SalesService>();
        services.AddScoped<PurchaseService>();
        services.AddScoped<SalesReturnService>();
        services.AddScoped<PurchaseReturnService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<ExpenseService>();
        services.AddScoped<UserService>();
        services.AddScoped<AuditLogService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ReportService>();
        services.AddScoped<BackupService>();

        services.AddScoped<InvoicePdfService>();
        services.AddScoped<ExcelExportService>();

        return services;
    }
}
