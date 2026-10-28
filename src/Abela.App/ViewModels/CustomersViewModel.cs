using Abela.App.Common;
using System.Collections.ObjectModel;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Abela.App.ViewModels;

public partial class CustomersViewModel : ObservableObject
{
    private readonly CustomerService _customerService;
    private readonly ReportService _reportService;
    private readonly IDialogService _dialog;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string searchTerm = string.Empty;
    [ObservableProperty] private Customer? selectedCustomer;

    public ObservableCollection<Customer> Customers { get; } = new();

    public bool CanAdd => _currentUser.HasPermission(Domain.Common.PermissionCodes.CustomersAdd);
    public bool CanEdit => _currentUser.HasPermission(Domain.Common.PermissionCodes.CustomersEdit);

    public CustomersViewModel(CustomerService customerService, ReportService reportService, IDialogService dialog, ICurrentUserService currentUser)
    {
        _customerService = customerService;
        _reportService = reportService;
        _dialog = dialog;
        _currentUser = currentUser;
        SearchAsync().FireAndForgetSafe(_dialog, "العملاء");
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var results = await _customerService.SearchAsync(SearchTerm);
        Customers.Clear();
        foreach (var c in results) Customers.Add(c);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var code = await _customerService.GenerateCodeAsync();
        var customer = new Customer { Code = code, IsActive = true };
        var window = new Views.CustomerEditWindow(customer);
        if (window.ShowDialog() == true)
        {
            var result = await _customerService.SaveAsync(customer);
            if (result.Success) { _dialog.ShowSuccess(result.Message); await SearchAsync(); }
            else _dialog.ShowError(result.Message);
        }
    }

    [RelayCommand]
    private async Task EditAsync()
    {
        if (SelectedCustomer is null) { _dialog.ShowInfo("الرجاء اختيار عميل"); return; }
        var window = new Views.CustomerEditWindow(SelectedCustomer);
        if (window.ShowDialog() == true)
        {
            var result = await _customerService.SaveAsync(SelectedCustomer);
            if (result.Success) { _dialog.ShowSuccess(result.Message); await SearchAsync(); }
            else _dialog.ShowError(result.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedCustomer is null) { _dialog.ShowInfo("الرجاء اختيار عميل"); return; }
        if (!_dialog.Confirm($"هل تريد حذف العميل {SelectedCustomer.Name}؟")) return;
        var result = await _customerService.DeleteAsync(SelectedCustomer.Id);
        if (result.Success) { _dialog.ShowSuccess(result.Message); await SearchAsync(); }
        else _dialog.ShowError(result.Message);
    }

    [RelayCommand]
    private async Task ViewStatementAsync()
    {
        if (SelectedCustomer is null) { _dialog.ShowInfo("الرجاء اختيار عميل"); return; }
        var balance = await _customerService.GetBalanceAsync(SelectedCustomer.Id);
        var rows = await _reportService.CustomerStatementAsync(SelectedCustomer.Id, DateTime.Today.AddMonths(-12), DateTime.Today.AddDays(1));
        var window = new Views.StatementWindow($"كشف حساب العميل: {SelectedCustomer.Name}", rows, balance);
        window.ShowDialog();
    }
}
