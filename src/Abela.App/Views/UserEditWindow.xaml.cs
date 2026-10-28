using System.Windows;
using Abela.Domain.Entities;

namespace Abela.App.Views;

public partial class UserEditWindow : Window
{
    private readonly bool _isNew;

    public string UsernameValue { get; private set; } = "";
    public string PasswordValue { get; private set; } = "";
    public string FullNameValue { get; private set; } = "";
    public int SelectedRoleId { get; private set; }
    public bool IsActiveValue { get; private set; } = true;

    public UserEditWindow(List<Role> roles, User? existing = null)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        RoleCombo.ItemsSource = roles;
        _isNew = existing is null;

        if (existing is null)
        {
            IsActiveCheck.IsChecked = true;
        }
        else
        {
            Title = "تعديل مستخدم";
            UsernameBox.Text = existing.Username;
            UsernameBox.IsEnabled = false;
            PasswordBox.Visibility = Visibility.Collapsed;
            PasswordLabel.Visibility = Visibility.Collapsed;
            FullNameBox.Text = existing.FullName;
            RoleCombo.SelectedItem = roles.FirstOrDefault(r => r.Id == existing.RoleId);
            IsActiveCheck.IsChecked = existing.IsActive;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        if (_isNew && (string.IsNullOrWhiteSpace(UsernameBox.Text) || string.IsNullOrWhiteSpace(PasswordBox.Password)))
        {
            ShowError("اسم المستخدم وكلمة المرور مطلوبان");
            return;
        }
        if (string.IsNullOrWhiteSpace(FullNameBox.Text)) { ShowError("الاسم الكامل مطلوب"); return; }
        if (RoleCombo.SelectedItem is not Role role) { ShowError("الرجاء اختيار الدور"); return; }

        UsernameValue = UsernameBox.Text.Trim();
        PasswordValue = PasswordBox.Password;
        FullNameValue = FullNameBox.Text.Trim();
        SelectedRoleId = role.Id;
        IsActiveValue = IsActiveCheck.IsChecked == true;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
