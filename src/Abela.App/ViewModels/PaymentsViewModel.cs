using Abela.App.Common;
using System.Collections.ObjectModel;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Abela.App.ViewModels;

public partial class PaymentsViewModel : ObservableObject
{
    private readonly PaymentService _paymentService;
    private readonly CustomerService _customerService;
    private readonly SupplierService _supplierService;
    private readonly IDialogService _dialog;
    private readonly ICurrentUserService _currentUser;

    public ObservableCollection<CustomerPayment> CustomerPayments { get; } = new();
    public ObservableCollection<SupplierPayment> SupplierPayments { get; } = new();

    public bool CanAdd => _currentUser.HasPermission(Domain.Common.PermissionCodes.PaymentsAdd);

    public PaymentsViewModel(PaymentService paymentService, CustomerService customerService, SupplierService supplierService, IDialogService dialog, ICurrentUserService currentUser)
    {
        _paymentService = paymentService;
        _customerService = customerService;
        _supplierService = supplierService;
        _dialog = dialog;
        _currentUser = currentUser;
        LoadAsync().FireAndForgetSafe(_dialog, "الدفعات");
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        CustomerPayments.Clear();
        foreach (var p in await _paymentService.GetCustomerPaymentsAsync()) CustomerPayments.Add(p);
        SupplierPayments.Clear();
        foreach (var p in await _paymentService.GetSupplierPaymentsAsync()) SupplierPayments.Add(p);
    }

    [RelayCommand]
    private async Task NewCustomerPaymentAsync()
    {
        var customers = await _customerService.SearchAsync();
        var window = new Views.PaymentWindow("سند قبض من عميل", customers.Cast<object>().ToList(), c => ((Customer)c).Name);
        if (window.ShowDialog() != true || window.SelectedParty is not Customer customer) return;

        var result = await _paymentService.AddCustomerPaymentAsync(customer.Id, window.Amount, window.SelectedMethod, window.Reference, null, window.Notes, _currentUser.UserId);
        if (result.Success) { _dialog.ShowSuccess(result.Message); await LoadAsync(); }
        else _dialog.ShowError(result.Message);
    }

    [RelayCommand]
    private async Task NewSupplierPaymentAsync()
    {
        var suppliers = await _supplierService.SearchAsync();
        var window = new Views.PaymentWindow("سند دفع لمورد", suppliers.Cast<object>().ToList(), s => ((Supplier)s).Name);
        if (window.ShowDialog() != true || window.SelectedParty is not Supplier supplier) return;

        var result = await _paymentService.AddSupplierPaymentAsync(supplier.Id, window.Amount, window.SelectedMethod, window.Reference, null, window.Notes, _currentUser.UserId);
        if (result.Success) { _dialog.ShowSuccess(result.Message); await LoadAsync(); }
        else _dialog.ShowError(result.Message);
    }
}
