using Abela.App.Common;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Abela.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly IDialogService _dialog;

    [ObservableProperty] private CompanySettings company = new();
    [ObservableProperty] private ApplicationSettings application = new();

    public SettingsViewModel(SettingsService settingsService, IDialogService dialog)
    {
        _settingsService = settingsService;
        _dialog = dialog;
        LoadAsync().FireAndForgetSafe(_dialog, "الإعدادات");
    }

    private async Task LoadAsync()
    {
        Company = await _settingsService.GetCompanySettingsAsync();
        Application = await _settingsService.GetApplicationSettingsAsync();
    }

    [RelayCommand]
    private async Task SaveCompanyAsync()
    {
        var result = await _settingsService.SaveCompanySettingsAsync(Company);
        if (result.Success) _dialog.ShowSuccess("تم حفظ بيانات الشركة بنجاح");
        else _dialog.ShowError(result.Message);
    }

    [RelayCommand]
    private async Task SaveApplicationAsync()
    {
        var result = await _settingsService.SaveApplicationSettingsAsync(Application);
        if (result.Success) _dialog.ShowSuccess("تم حفظ إعدادات النظام بنجاح");
        else _dialog.ShowError(result.Message);
    }

    [RelayCommand]
    private void ManageExpenseCategories()
    {
        var window = new Views.ExpenseCategoryManagerWindow(_settingsService, _dialog);
        window.ShowDialog();
    }
}
