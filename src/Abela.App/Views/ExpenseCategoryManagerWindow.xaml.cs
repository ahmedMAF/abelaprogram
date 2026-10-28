using System.Windows;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;

namespace Abela.App.Views;

public partial class ExpenseCategoryManagerWindow : Window
{
    private readonly SettingsService _service;
    private readonly IDialogService _dialog;

    public ExpenseCategoryManagerWindow(SettingsService service, IDialogService dialog)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        _service = service;
        _dialog = dialog;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        CategoriesList.ItemsSource = null;
        CategoriesList.ItemsSource = await _service.GetExpenseCategoriesAsync();
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text)) return;
        var result = await _service.SaveExpenseCategoryAsync(new ExpenseCategory { Name = NameBox.Text.Trim() });
        if (!result.Success) { _dialog.ShowError(result.Message); return; }
        NameBox.Clear();
        await LoadAsync();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (CategoriesList.SelectedItem is not ExpenseCategory c) { _dialog.ShowInfo("اختر تصنيفًا"); return; }
        if (!_dialog.Confirm($"هل تريد حذف التصنيف {c.Name}؟")) return;
        var result = await _service.DeleteExpenseCategoryAsync(c.Id);
        if (!result.Success) { _dialog.ShowError(result.Message); return; }
        await LoadAsync();
    }
}
