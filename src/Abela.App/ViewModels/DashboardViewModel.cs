using System.Collections.ObjectModel;
using Abela.App.Common;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Abela.App.ViewModels;

public class ChartBarItem
{
    public string Label { get; set; } = "";
    public decimal Value { get; set; }
    public double BarHeight { get; set; }
}

public class RecentInvoiceRow
{
    public string Number { get; set; } = "";
    public DateTime Date { get; set; }
    public string Party { get; set; } = "";
    public decimal Total { get; set; }
}

public class LowStockRow
{
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal MinLevel { get; set; }
}

public partial class DashboardViewModel : ObservableObject
{
    private readonly DashboardService _dashboardService;
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;
    private readonly UnitService _unitService;
    private readonly CustomerService _customerService;
    private readonly SupplierService _supplierService;
    private readonly SalesService _salesService;
    private readonly PurchaseService _purchaseService;
    private readonly ExpenseService _expenseService;
    private readonly SettingsService _settingsService;
    private readonly IDialogService _dialog;
    public NavigationService Navigation { get; }
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private decimal todaySales;
    [ObservableProperty] private decimal todayPurchases;
    [ObservableProperty] private decimal todayExpenses;
    [ObservableProperty] private decimal netToday;
    [ObservableProperty] private int productsCount;
    [ObservableProperty] private int customersCount;
    [ObservableProperty] private int suppliersCount;
    [ObservableProperty] private decimal customerDebts;
    [ObservableProperty] private decimal supplierDebts;
    [ObservableProperty] private bool isLoading;

    public ObservableCollection<LowStockRow> LowStock { get; } = new();
    public ObservableCollection<RecentInvoiceRow> RecentSales { get; } = new();
    public ObservableCollection<RecentInvoiceRow> RecentPurchases { get; } = new();
    public ObservableCollection<ChartBarItem> SalesChart { get; } = new();

    public bool CanAddSale => _currentUser.HasPermission(Domain.Common.PermissionCodes.SalesAdd);
    public bool CanAddPurchase => _currentUser.HasPermission(Domain.Common.PermissionCodes.PurchasesAdd);
    public bool CanAddProduct => _currentUser.HasPermission(Domain.Common.PermissionCodes.ProductsAdd);
    public bool CanAddCustomer => _currentUser.HasPermission(Domain.Common.PermissionCodes.CustomersAdd);
    public bool CanAddSupplier => _currentUser.HasPermission(Domain.Common.PermissionCodes.SuppliersAdd);
    public bool CanAddExpense => _currentUser.HasPermission(Domain.Common.PermissionCodes.ExpensesAdd);
    public bool CanViewReports => _currentUser.HasPermission(Domain.Common.PermissionCodes.ReportsView);

    public DashboardViewModel(DashboardService dashboardService, ProductService productService, CategoryService categoryService,
        UnitService unitService, CustomerService customerService, SupplierService supplierService, SalesService salesService,
        PurchaseService purchaseService, ExpenseService expenseService, SettingsService settingsService,
        NavigationService navigation, ICurrentUserService currentUser, IDialogService dialog)
    {
        _dashboardService = dashboardService;
        _productService = productService;
        _categoryService = categoryService;
        _unitService = unitService;
        _customerService = customerService;
        _supplierService = supplierService;
        _salesService = salesService;
        _purchaseService = purchaseService;
        _expenseService = expenseService;
        _settingsService = settingsService;
        _dialog = dialog;
        Navigation = navigation;
        _currentUser = currentUser;
        LoadAsync().FireAndForgetSafe(_dialog, "لوحة التحكم");
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var s = await _dashboardService.GetSummaryAsync();
            TodaySales = s.TodaySales;
            TodayPurchases = s.TodayPurchases;
            TodayExpenses = s.TodayExpenses;
            NetToday = s.NetToday;
            ProductsCount = s.ProductsCount;
            CustomersCount = s.CustomersCount;
            SuppliersCount = s.SuppliersCount;
            CustomerDebts = s.CustomerDebts;
            SupplierDebts = s.SupplierDebts;

            LowStock.Clear();
            foreach (var p in s.LowStockProducts) LowStock.Add(new LowStockRow { ProductName = p.ProductName, Quantity = p.Quantity, MinLevel = p.MinLevel });

            RecentSales.Clear();
            foreach (var r in s.RecentSales) RecentSales.Add(new RecentInvoiceRow { Number = r.Number, Date = r.Date, Party = r.Party, Total = r.Total });

            RecentPurchases.Clear();
            foreach (var r in s.RecentPurchases) RecentPurchases.Add(new RecentInvoiceRow { Number = r.Number, Date = r.Date, Party = r.Party, Total = r.Total });

            SalesChart.Clear();
            var max = s.SalesLast7Days.Count > 0 ? Math.Max(1, s.SalesLast7Days.Max(x => x.Total)) : 1;
            foreach (var d in s.SalesLast7Days)
                SalesChart.Add(new ChartBarItem { Label = d.Date.ToString("MM/dd"), Value = d.Total, BarHeight = (double)(d.Total / max) * 140 + 4 });
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NewSale()
    {
        var window = new Views.SalesInvoiceWindow(_salesService, _customerService, _productService, _settingsService, _currentUser, _dialog);
        if (window.ShowDialog() == true) await LoadAsync();
    }

    [RelayCommand]
    private async Task NewPurchase()
    {
        var window = new Views.PurchaseInvoiceWindow(_purchaseService, _supplierService, _productService, _currentUser, _dialog);
        if (window.ShowDialog() == true) await LoadAsync();
    }

    [RelayCommand]
    private async Task NewProduct()
    {
        var categories = await _categoryService.GetAllAsync();
        var units = await _unitService.GetAllAsync();
        var code = await _productService.GenerateCodeAsync();
        var product = new Product { Code = code, RequiresSerialNumber = false, IsActive = true };
        var window = new Views.ProductEditWindow(product, new ObservableCollection<ProductCategory>(categories), new ObservableCollection<Unit>(units), isNew: true);
        if (window.ShowDialog() == true)
        {
            var result = await _productService.SaveAsync(product);
            if (result.Success) { _dialog.ShowSuccess(result.Message); await LoadAsync(); }
            else _dialog.ShowError(result.Message);
        }
    }

    [RelayCommand]
    private async Task NewCustomer()
    {
        var code = await _customerService.GenerateCodeAsync();
        var customer = new Customer { Code = code, IsActive = true };
        var window = new Views.CustomerEditWindow(customer);
        if (window.ShowDialog() == true)
        {
            var result = await _customerService.SaveAsync(customer);
            if (result.Success) { _dialog.ShowSuccess(result.Message); await LoadAsync(); }
            else _dialog.ShowError(result.Message);
        }
    }

    [RelayCommand]
    private async Task NewSupplier()
    {
        var code = await _supplierService.GenerateCodeAsync();
        var supplier = new Supplier { Code = code, IsActive = true };
        var window = new Views.SupplierEditWindow(supplier);
        if (window.ShowDialog() == true)
        {
            var result = await _supplierService.SaveAsync(supplier);
            if (result.Success) { _dialog.ShowSuccess(result.Message); await LoadAsync(); }
            else _dialog.ShowError(result.Message);
        }
    }

    [RelayCommand]
    private async Task NewExpense()
    {
        var categories = await _settingsService.GetExpenseCategoriesAsync();
        var expense = new Expense { ExpenseDate = DateTime.Now, PaymentMethod = Domain.Enums.PaymentMethod.Cash, CreatedByUserId = _currentUser.UserId };
        var window = new Views.ExpenseEditWindow(expense, categories, isNew: true);
        if (window.ShowDialog() == true)
        {
            var result = await _expenseService.AddAsync(expense);
            if (result.Success) { _dialog.ShowSuccess(result.Message); await LoadAsync(); }
            else _dialog.ShowError(result.Message);
        }
    }

    [RelayCommand] private void ViewReports() => Navigation.NavigateTo<Views.ReportsView>();
}
