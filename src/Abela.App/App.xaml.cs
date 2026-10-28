using System.IO;
using System.Windows;
using System.Windows.Threading;
using Abela.App.Common;
using Abela.App.ViewModels;
using Abela.App.Views;
using Abela.Application;
using Abela.Application.Common;
using Abela.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuestPDF.Infrastructure;

namespace Abela.App;

public partial class App : System.Windows.Application
{
    public static IHost AppHost { get; private set; } = null!;
    public static string DataFolder { get; private set; } = string.Empty;
    public static string DbFilePath { get; private set; } = string.Empty;

    protected override async void OnStartup(StartupEventArgs e)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        DataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Abela");
        Directory.CreateDirectory(DataFolder);
        Directory.CreateDirectory(Path.Combine(DataFolder, "Backups"));
        DbFilePath = Path.Combine(DataFolder, "Abela.db");

        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddAbelaServices(DbFilePath);
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<NavigationService>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<MainWindow>();

                services.AddTransient<DashboardView>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<ProductsView>();
                services.AddTransient<ProductsViewModel>();
                services.AddTransient<CustomersView>();
                services.AddTransient<CustomersViewModel>();
                services.AddTransient<SuppliersView>();
                services.AddTransient<SuppliersViewModel>();
                services.AddTransient<SalesView>();
                services.AddTransient<SalesViewModel>();
                services.AddTransient<SalesReturnsView>();
                services.AddTransient<SalesReturnsViewModel>();
                services.AddTransient<PurchasesView>();
                services.AddTransient<PurchasesViewModel>();
                services.AddTransient<PurchaseReturnsView>();
                services.AddTransient<PurchaseReturnsViewModel>();
                services.AddTransient<InventoryView>();
                services.AddTransient<InventoryViewModel>();
                services.AddTransient<PaymentsView>();
                services.AddTransient<PaymentsViewModel>();
                services.AddTransient<ExpensesView>();
                services.AddTransient<ExpensesViewModel>();
                services.AddTransient<ReportsView>();
                services.AddTransient<ReportsViewModel>();
                services.AddTransient<UsersView>();
                services.AddTransient<UsersViewModel>();
                services.AddTransient<AuditLogView>();
                services.AddTransient<AuditLogViewModel>();
                services.AddTransient<SettingsView>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<BackupView>();
                services.AddTransient<BackupViewModel>();
            })
            .Build();

        await AppHost.StartAsync();

        using (var scope = AppHost.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await DbInitializer.InitializeAsync(db);
        }

        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        base.OnStartup(e);

        var loginWindow = AppHost.Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogFatal(e.Exception);
        MessageBox.Show("حدث خطأ غير متوقع:\n" + e.Exception.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost.StopAsync();
        base.OnExit(e);
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        LogFatal(ex);
        MessageBox.Show("حدث خطأ غير متوقع في النظام. تم تسجيل تفاصيل الخطأ.\n" + ex?.Message, "خطأ فادح", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private static void LogFatal(Exception? ex)
    {
        try
        {
            var logPath = Path.Combine(DataFolder, "error.log");
            File.AppendAllText(logPath, $"{DateTime.Now}: {ex}\n\n");
        }
        catch { }
    }
}
