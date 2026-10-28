using Abela.App.Common;
using System.Collections.ObjectModel;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Abela.App.ViewModels;

public partial class PurchaseReturnsViewModel : ObservableObject
{
    private readonly PurchaseReturnService _returnService;
    private readonly PurchaseService _purchaseService;
    private readonly IDialogService _dialog;
    private readonly ICurrentUserService _currentUser;

    public ObservableCollection<PurchaseReturn> Returns { get; } = new();

    public bool CanAdd => _currentUser.HasPermission(Domain.Common.PermissionCodes.PurchasesAdd);

    public PurchaseReturnsViewModel(PurchaseReturnService returnService, PurchaseService purchaseService, IDialogService dialog, ICurrentUserService currentUser)
    {
        _returnService = returnService;
        _purchaseService = purchaseService;
        _dialog = dialog;
        _currentUser = currentUser;
        LoadAsync().FireAndForgetSafe(_dialog, "مرتجعات المشتريات");
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var results = await _returnService.SearchAsync();
        Returns.Clear();
        foreach (var r in results) Returns.Add(r);
    }

    [RelayCommand]
    private async Task NewReturnAsync()
    {
        var invoiceNumber = Views.InputDialog.Show("فاتورة الشراء", "أدخل رقم فاتورة الشراء المراد إرجاع أصناف منها:");
        if (string.IsNullOrWhiteSpace(invoiceNumber)) return;

        var invoices = await _purchaseService.SearchAsync();
        var invoice = invoices.FirstOrDefault(i => i.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase));
        if (invoice is null) { _dialog.ShowError("لم يتم العثور على فاتورة بهذا الرقم"); return; }

        var full = await _purchaseService.GetByIdAsync(invoice.Id);
        if (full is null) return;

        var window = new Views.PurchaseReturnWindow(full, _returnService, _currentUser, _dialog);
        if (window.ShowDialog() == true)
            await LoadAsync();
    }
}
