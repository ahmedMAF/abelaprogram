using System.Windows;
using System.Windows.Controls;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;

namespace Abela.App.Views;

public partial class RolePermissionsWindow : Window
{
    private readonly UserService _userService;
    private readonly IDialogService _dialog;
    private List<Permission> _allPermissions = new();
    private readonly List<CheckBox> _checkBoxes = new();

    public RolePermissionsWindow(UserService userService, IDialogService dialog)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        _userService = userService;
        _dialog = dialog;
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        _allPermissions = await _userService.GetPermissionsAsync();
        var roles = await _userService.GetRolesAsync();
        RoleCombo.ItemsSource = roles;
        if (roles.Count > 0) RoleCombo.SelectedIndex = 0;
    }

    private async void RoleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RoleCombo.SelectedItem is not Role role) return;
        var assignedIds = await _userService.GetRolePermissionIdsAsync(role.Id);

        _checkBoxes.Clear();
        string? lastModule = null;
        var panel = new StackPanel();
        foreach (var p in _allPermissions)
        {
            if (p.Module != lastModule)
            {
                panel.Children.Add(new TextBlock { Text = p.Module, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 4) });
                lastModule = p.Module;
            }
            var cb = new CheckBox { Content = p.Name, Tag = p.Id, IsChecked = assignedIds.Contains(p.Id), Margin = new Thickness(16, 2, 0, 2) };
            _checkBoxes.Add(cb);
            panel.Children.Add(cb);
        }
        PermissionsPanel.ItemsSource = panel.Children.Cast<object>().ToList();
    }

    private async void AddRole_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewRoleNameBox.Text)) return;
        var result = await _userService.SaveRoleAsync(new Role { Name = NewRoleNameBox.Text.Trim() });
        if (!result.Success) { _dialog.ShowError(result.Message); return; }
        NewRoleNameBox.Clear();
        var roles = await _userService.GetRolesAsync();
        RoleCombo.ItemsSource = roles;
        RoleCombo.SelectedIndex = roles.Count - 1;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (RoleCombo.SelectedItem is not Role role) return;
        var selectedIds = _checkBoxes.Where(c => c.IsChecked == true).Select(c => (int)c.Tag).ToList();
        var result = await _userService.SaveRolePermissionsAsync(role.Id, selectedIds);
        if (result.Success) _dialog.ShowSuccess("تم حفظ الصلاحيات بنجاح");
        else _dialog.ShowError(result.Message);
    }
}
