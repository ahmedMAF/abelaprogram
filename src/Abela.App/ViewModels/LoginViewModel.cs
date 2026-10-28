using Abela.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Abela.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;

    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private bool isBusy;

    public event Action<bool>? LoginSucceeded; // bool = mustChangePassword

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task LoginAsync(object? passwordParam)
    {
        HasError = false;
        var password = passwordParam as string ?? string.Empty;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "الرجاء إدخال اسم المستخدم وكلمة المرور";
            HasError = true;
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _authService.LoginAsync(Username.Trim(), password);
            if (!result.Success)
            {
                ErrorMessage = result.Message;
                HasError = true;
                return;
            }
            LoginSucceeded?.Invoke(result.Data);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
