using Abela.App.Common;
using Abela.App.Views;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Abela.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ICurrentUserService _currentUser;
    private readonly AuthService _authService;
    public NavigationService Navigation { get; }

    [ObservableProperty] private string userDisplayName = string.Empty;
    [ObservableProperty] private string roleDisplayName = string.Empty;
    [ObservableProperty] private string activeMenu = "Dashboard";

    public bool CanSales => _currentUser.HasPermission(PermissionCodes.SalesView);
    public bool CanPurchases => _currentUser.HasPermission(PermissionCodes.PurchasesView);
    public bool CanProducts => _currentUser.HasPermission(PermissionCodes.ProductsView);
    public bool CanInventory => _currentUser.HasPermission(PermissionCodes.InventoryView);
    public bool CanCustomers => _currentUser.HasPermission(PermissionCodes.CustomersView);
    public bool CanSuppliers => _currentUser.HasPermission(PermissionCodes.SuppliersView);
    public bool CanPayments => _currentUser.HasPermission(PermissionCodes.PaymentsView);
    public bool CanExpenses => _currentUser.HasPermission(PermissionCodes.ExpensesView);
    public bool CanReports => _currentUser.HasPermission(PermissionCodes.ReportsView);
    public bool CanUsers => _currentUser.HasPermission(PermissionCodes.UsersManage);
    public bool CanAudit => _currentUser.HasPermission(PermissionCodes.AuditView);
    public bool CanSettings => _currentUser.HasPermission(PermissionCodes.SettingsManage);
    public bool CanBackup => _currentUser.HasPermission(PermissionCodes.BackupManage);

    public MainViewModel(ICurrentUserService currentUser, AuthService authService, NavigationService navigation)
    {
        _currentUser = currentUser;
        _authService = authService;
        Navigation = navigation;
        UserDisplayName = currentUser.UserName;
        RoleDisplayName = currentUser.RoleName;
        GoDashboard();
    }

    [RelayCommand] private void GoDashboard() { ActiveMenu = "Dashboard"; Navigation.NavigateTo<DashboardView>(); }
    [RelayCommand] private void GoSales() { ActiveMenu = "Sales"; Navigation.NavigateTo<SalesView>(); }
    [RelayCommand] private void GoSalesReturns() { ActiveMenu = "SalesReturns"; Navigation.NavigateTo<SalesReturnsView>(); }
    [RelayCommand] private void GoPurchases() { ActiveMenu = "Purchases"; Navigation.NavigateTo<PurchasesView>(); }
    [RelayCommand] private void GoPurchaseReturns() { ActiveMenu = "PurchaseReturns"; Navigation.NavigateTo<PurchaseReturnsView>(); }
    [RelayCommand] private void GoProducts() { ActiveMenu = "Products"; Navigation.NavigateTo<ProductsView>(); }
    [RelayCommand] private void GoInventory() { ActiveMenu = "Inventory"; Navigation.NavigateTo<InventoryView>(); }
    [RelayCommand] private void GoCustomers() { ActiveMenu = "Customers"; Navigation.NavigateTo<CustomersView>(); }
    [RelayCommand] private void GoSuppliers() { ActiveMenu = "Suppliers"; Navigation.NavigateTo<SuppliersView>(); }
    [RelayCommand] private void GoPayments() { ActiveMenu = "Payments"; Navigation.NavigateTo<PaymentsView>(); }
    [RelayCommand] private void GoExpenses() { ActiveMenu = "Expenses"; Navigation.NavigateTo<ExpensesView>(); }
    [RelayCommand] private void GoReports() { ActiveMenu = "Reports"; Navigation.NavigateTo<ReportsView>(); }
    [RelayCommand] private void GoUsers() { ActiveMenu = "Users"; Navigation.NavigateTo<UsersView>(); }
    [RelayCommand] private void GoAudit() { ActiveMenu = "Audit"; Navigation.NavigateTo<AuditLogView>(); }
    [RelayCommand] private void GoSettings() { ActiveMenu = "Settings"; Navigation.NavigateTo<SettingsView>(); }
    [RelayCommand] private void GoBackup() { ActiveMenu = "Backup"; Navigation.NavigateTo<BackupView>(); }

    [RelayCommand]
    private void ChangeMyPassword()
    {
        var wnd = new ChangePasswordWindow(forced: false);
        wnd.ShowDialog();
    }

    public event Action? LoggedOut;

    [RelayCommand]
    private async Task Logout()
    {
        await _authService.LogoutAsync();
        LoggedOut?.Invoke();
    }
}
