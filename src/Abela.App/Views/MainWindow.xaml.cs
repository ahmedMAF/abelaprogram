using System.Windows;
using Abela.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Abela.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.LoggedOut += OnLoggedOut;
    }

    private void OnLoggedOut()
    {
        var loginWindow = App.AppHost.Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();
        Close();
    }
}
