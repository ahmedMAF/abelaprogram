using Abela.App.Common;
using System.Collections.ObjectModel;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Abela.App.ViewModels;

public partial class UsersViewModel : ObservableObject
{
    private readonly UserService _userService;
    private readonly IDialogService _dialog;

    [ObservableProperty] private User? selectedUser;

    public ObservableCollection<User> Users { get; } = new();

    public UsersViewModel(UserService userService, IDialogService dialog)
    {
        _userService = userService;
        _dialog = dialog;
        LoadAsync().FireAndForgetSafe(_dialog, "المستخدمون");
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        Users.Clear();
        foreach (var u in await _userService.GetAllAsync()) Users.Add(u);
    }

    [RelayCommand]
    private async Task AddUserAsync()
    {
        var roles = await _userService.GetRolesAsync();
        var window = new Views.UserEditWindow(roles);
        if (window.ShowDialog() == true)
        {
            var result = await _userService.CreateUserAsync(window.UsernameValue, window.PasswordValue, window.FullNameValue, window.SelectedRoleId);
            if (result.Success) { _dialog.ShowSuccess(result.Message); await LoadAsync(); }
            else _dialog.ShowError(result.Message);
        }
    }

    [RelayCommand]
    private async Task EditUserAsync()
    {
        if (SelectedUser is null) { _dialog.ShowInfo("الرجاء اختيار مستخدم"); return; }
        var roles = await _userService.GetRolesAsync();
        var window = new Views.UserEditWindow(roles, SelectedUser);
        if (window.ShowDialog() == true)
        {
            var result = await _userService.UpdateUserAsync(SelectedUser.Id, window.FullNameValue, window.SelectedRoleId, window.IsActiveValue);
            if (result.Success) { _dialog.ShowSuccess(result.Message); await LoadAsync(); }
            else _dialog.ShowError(result.Message);
        }
    }

    [RelayCommand]
    private async Task ResetPasswordAsync()
    {
        if (SelectedUser is null) { _dialog.ShowInfo("الرجاء اختيار مستخدم"); return; }
        var newPassword = Views.InputDialog.Show("إعادة تعيين كلمة المرور", $"أدخل كلمة مرور جديدة للمستخدم {SelectedUser.Username}:");
        if (string.IsNullOrWhiteSpace(newPassword)) return;
        var result = await _userService.ResetPasswordAsync(SelectedUser.Id, newPassword);
        if (result.Success) _dialog.ShowSuccess("تم إعادة تعيين كلمة المرور، سيُطلب من المستخدم تغييرها عند الدخول");
        else _dialog.ShowError(result.Message);
    }

    [RelayCommand]
    private async Task ManageRolesAsync()
    {
        var window = new Views.RolePermissionsWindow(_userService, _dialog);
        window.ShowDialog();
        await LoadAsync();
    }
}
