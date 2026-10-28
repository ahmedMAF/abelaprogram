using Abela.App.Common;
using System.Collections.ObjectModel;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Abela.App.ViewModels;

public partial class SuppliersViewModel : ObservableObject
{
    private readonly SupplierService _supplierService;
    private readonly ReportService _reportService;
    private readonly IDialogService _dialog;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string searchTerm = string.Empty;
    [ObservableProperty] private Supplier? selectedSupplier;

    public ObservableCollection<Supplier> Suppliers { get; } = new();

    public bool CanAdd => _currentUser.HasPermission(Domain.Common.PermissionCodes.SuppliersAdd);
    public bool CanEdit => _currentUser.HasPermission(Domain.Common.PermissionCodes.SuppliersEdit);

    public SuppliersViewModel(SupplierService supplierService, ReportService reportService, IDialogService dialog, ICurrentUserService currentUser)
    {
        _supplierService = supplierService;
        _reportService = reportService;
        _dialog = dialog;
        _currentUser = currentUser;
        SearchAsync().FireAndForgetSafe(_dialog, "الموردون");
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var results = await _supplierService.SearchAsync(SearchTerm);
        Suppliers.Clear();
        foreach (var s in results) Suppliers.Add(s);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var code = await _supplierService.GenerateCodeAsync();
        var supplier = new Supplier { Code = code, IsActive = true };
        var window = new Views.SupplierEditWindow(supplier);
        if (window.ShowDialog() == true)
        {
            var result = await _supplierService.SaveAsync(supplier);
            if (result.Success) { _dialog.ShowSuccess(result.Message); await SearchAsync(); }
            else _dialog.ShowError(result.Message);
        }
    }

    [RelayCommand]
    private async Task EditAsync()
    {
        if (SelectedSupplier is null) { _dialog.ShowInfo("الرجاء اختيار مورد"); return; }
        var window = new Views.SupplierEditWindow(SelectedSupplier);
        if (window.ShowDialog() == true)
        {
            var result = await _supplierService.SaveAsync(SelectedSupplier);
            if (result.Success) { _dialog.ShowSuccess(result.Message); await SearchAsync(); }
            else _dialog.ShowError(result.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedSupplier is null) { _dialog.ShowInfo("الرجاء اختيار مورد"); return; }
        if (!_dialog.Confirm($"هل تريد حذف المورد {SelectedSupplier.Name}؟")) return;
        var result = await _supplierService.DeleteAsync(SelectedSupplier.Id);
        if (result.Success) { _dialog.ShowSuccess(result.Message); await SearchAsync(); }
        else _dialog.ShowError(result.Message);
    }

    [RelayCommand]
    private async Task ViewStatementAsync()
    {
        if (SelectedSupplier is null) { _dialog.ShowInfo("الرجاء اختيار مورد"); return; }
        var balance = await _supplierService.GetBalanceAsync(SelectedSupplier.Id);
        var rows = await _reportService.SupplierStatementAsync(SelectedSupplier.Id, DateTime.Today.AddMonths(-12), DateTime.Today.AddDays(1));
        var window = new Views.StatementWindow($"كشف حساب المورد: {SelectedSupplier.Name}", rows, balance);
        window.ShowDialog();
    }
}
