using Abela.App.Common;
using System.Collections.ObjectModel;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using Abela.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Abela.App.ViewModels;

public partial class ExpensesViewModel : ObservableObject
{
    private readonly ExpenseService _expenseService;
    private readonly SettingsService _settingsService;
    private readonly IDialogService _dialog;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private DateTime fromDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime toDate = DateTime.Today;
    [ObservableProperty] private Expense? selectedExpense;

    public ObservableCollection<Expense> Expenses { get; } = new();

    public bool CanAdd => _currentUser.HasPermission(Domain.Common.PermissionCodes.ExpensesAdd);
    public bool CanEdit => _currentUser.HasPermission(Domain.Common.PermissionCodes.ExpensesEdit);
    public bool CanCancel => _currentUser.HasPermission(Domain.Common.PermissionCodes.ExpensesCancel);

    public ExpensesViewModel(ExpenseService expenseService, SettingsService settingsService, IDialogService dialog, ICurrentUserService currentUser)
    {
        _expenseService = expenseService;
        _settingsService = settingsService;
        _dialog = dialog;
        _currentUser = currentUser;
        SearchAsync().FireAndForgetSafe(_dialog, "المصروفات");
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var results = await _expenseService.SearchAsync(FromDate, ToDate.AddDays(1).AddSeconds(-1));
        Expenses.Clear();
        foreach (var e in results) Expenses.Add(e);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var categories = await _settingsService.GetExpenseCategoriesAsync();
        var expense = new Expense { ExpenseDate = DateTime.Now, PaymentMethod = PaymentMethod.Cash, CreatedByUserId = _currentUser.UserId };
        var window = new Views.ExpenseEditWindow(expense, categories, isNew: true);
        if (window.ShowDialog() == true)
        {
            var result = await _expenseService.AddAsync(expense);
            if (result.Success) { _dialog.ShowSuccess(result.Message); await SearchAsync(); }
            else _dialog.ShowError(result.Message);
        }
    }

    [RelayCommand]
    private async Task EditAsync()
    {
        if (SelectedExpense is null) { _dialog.ShowInfo("الرجاء اختيار مصروف"); return; }
        var categories = await _settingsService.GetExpenseCategoriesAsync();
        var window = new Views.ExpenseEditWindow(SelectedExpense, categories, isNew: false);
        if (window.ShowDialog() == true)
        {
            var result = await _expenseService.UpdateAsync(SelectedExpense);
            if (result.Success) { _dialog.ShowSuccess(result.Message); await SearchAsync(); }
            else _dialog.ShowError(result.Message);
        }
    }

    [RelayCommand]
    private async Task CancelExpenseAsync()
    {
        if (SelectedExpense is null) { _dialog.ShowInfo("الرجاء اختيار مصروف"); return; }
        if (!_dialog.Confirm($"هل تريد إلغاء المصروف {SelectedExpense.ExpenseNumber}؟")) return;
        var result = await _expenseService.CancelAsync(SelectedExpense.Id);
        if (result.Success) { _dialog.ShowSuccess(result.Message); await SearchAsync(); }
        else _dialog.ShowError(result.Message);
    }
}
