using System.Windows;
using Abela.Application.Common;
using Abela.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Abela.App.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly bool _forced;

    public ChangePasswordWindow(bool forced = false)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        _forced = forced;
        if (forced)
        {
            CancelButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            HeaderText.Text = "تغيير كلمة المرور";
            HeaderText.Foreground = System.Windows.Media.Brushes.Black;
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
        {
            ShowError("كلمة المرور الجديدة وتأكيدها غير متطابقين");
            return;
        }

        using var scope = App.AppHost.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();

        var result = await authService.ChangePasswordAsync(currentUser.UserId, CurrentPasswordBox.Password, NewPasswordBox.Password);
        if (!result.Success)
        {
            ShowError(result.Message);
            return;
        }

        MessageBox.Show("تم تغيير كلمة المرور بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
