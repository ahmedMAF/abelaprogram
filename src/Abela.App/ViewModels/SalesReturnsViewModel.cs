using Abela.App.Common;
using System.Collections.ObjectModel;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Abela.App.ViewModels;

public partial class SalesReturnsViewModel : ObservableObject
{
    private readonly SalesReturnService _returnService;
    private readonly SalesService _salesService;
    private readonly IDialogService _dialog;
    private readonly ICurrentUserService _currentUser;

    public ObservableCollection<SalesReturn> Returns { get; } = new();

    public bool CanAdd => _currentUser.HasPermission(Domain.Common.PermissionCodes.SalesAdd);

    public SalesReturnsViewModel(SalesReturnService returnService, SalesService salesService, IDialogService dialog, ICurrentUserService currentUser)
    {
        _returnService = returnService;
        _salesService = salesService;
        _dialog = dialog;
        _currentUser = currentUser;
        LoadAsync().FireAndForgetSafe(_dialog, "مرتجعات المبيعات");
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
        var invoiceNumber = Views.InputDialog.Show("فاتورة البيع", "أدخل رقم فاتورة البيع المراد إرجاع أصناف منها:");
        if (string.IsNullOrWhiteSpace(invoiceNumber)) return;

        var invoices = await _salesService.SearchAsync();
        var invoice = invoices.FirstOrDefault(i => i.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase));
        if (invoice is null) { _dialog.ShowError("لم يتم العثور على فاتورة بهذا الرقم"); return; }

        var full = await _salesService.GetByIdAsync(invoice.Id);
        if (full is null) return;

        var window = new Views.SalesReturnWindow(full, _returnService, _currentUser, _dialog);
        if (window.ShowDialog() == true)
            await LoadAsync();
    }
}
