using System.Windows;
using System.Windows.Controls;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;

namespace Abela.App.Views;

public partial class UnitManagerWindow : Window
{
    private readonly UnitService _service;
    private readonly IDialogService _dialog;

    public UnitManagerWindow(UnitService service, IDialogService dialog)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        _service = service;
        _dialog = dialog;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        UnitsList.ItemsSource = null;
        UnitsList.ItemsSource = await _service.GetAllAsync();
    }

    private void UnitsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UnitsList.SelectedItem is Unit u) NameBox.Text = u.Name;
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var result = await _service.SaveAsync(new Unit { Name = NameBox.Text.Trim() });
        if (!result.Success) { _dialog.ShowError(result.Message); return; }
        NameBox.Clear();
        await LoadAsync();
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (UnitsList.SelectedItem is not Unit u) { _dialog.ShowInfo("اختر وحدة"); return; }
        u.Name = NameBox.Text.Trim();
        var result = await _service.SaveAsync(u);
        if (!result.Success) { _dialog.ShowError(result.Message); return; }
        await LoadAsync();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (UnitsList.SelectedItem is not Unit u) { _dialog.ShowInfo("اختر وحدة"); return; }
        if (!_dialog.Confirm($"هل تريد حذف الوحدة {u.Name}؟")) return;
        var result = await _service.DeleteAsync(u.Id);
        if (!result.Success) { _dialog.ShowError(result.Message); return; }
        await LoadAsync();
    }
}
