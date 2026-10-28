using System.Windows;
using System.Windows.Controls;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;

namespace Abela.App.Views;

public partial class CategoryManagerWindow : Window
{
    private readonly CategoryService _service;
    private readonly IDialogService _dialog;

    public CategoryManagerWindow(CategoryService service, IDialogService dialog)
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
        CategoriesList.ItemsSource = await _service.GetAllAsync();
    }

    private void CategoriesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoriesList.SelectedItem is ProductCategory c) NameBox.Text = c.Name;
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var result = await _service.SaveAsync(new ProductCategory { Name = NameBox.Text.Trim() });
        if (!result.Success) { _dialog.ShowError(result.Message); return; }
        NameBox.Clear();
        await LoadAsync();
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (CategoriesList.SelectedItem is not ProductCategory c) { _dialog.ShowInfo("اختر تصنيفًا"); return; }
        c.Name = NameBox.Text.Trim();
        var result = await _service.SaveAsync(c);
        if (!result.Success) { _dialog.ShowError(result.Message); return; }
        await LoadAsync();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (CategoriesList.SelectedItem is not ProductCategory c) { _dialog.ShowInfo("اختر تصنيفًا"); return; }
        if (!_dialog.Confirm($"هل تريد حذف التصنيف {c.Name}؟")) return;
        var result = await _service.DeleteAsync(c.Id);
        if (!result.Success) { _dialog.ShowError(result.Message); return; }
        await LoadAsync();
    }
}
