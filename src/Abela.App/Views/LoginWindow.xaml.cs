using System.Windows;
using Abela.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Abela.App.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.LoginSucceeded += OnLoginSucceeded;
    }

    private void ShowPasswordCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (ShowPasswordCheck.IsChecked == true)
        {
            PasswordVisibleBox.Text = PasswordBox.Password;
            PasswordVisibleBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
        }
        else
        {
            PasswordBox.Password = PasswordVisibleBox.Text;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordVisibleBox.Visibility = Visibility.Collapsed;
        }
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var password = ShowPasswordCheck.IsChecked == true ? PasswordVisibleBox.Text : PasswordBox.Password;
        await _viewModel.LoginCommand.ExecuteAsync(password);
    }

    private void OnLoginSucceeded(bool mustChangePassword)
    {
        var mainWindow = App.AppHost.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        if (mustChangePassword)
        {
            var changePasswordWindow = new ChangePasswordWindow(forced: true) { Owner = mainWindow };
            changePasswordWindow.ShowDialog();
        }

        Close();
    }
}
