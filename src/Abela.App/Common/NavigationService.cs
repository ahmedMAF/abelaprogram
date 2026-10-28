using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Abela.App.Common;

public partial class NavigationService : ObservableObject
{
    [ObservableProperty] private object? currentView;

    private IServiceScope? _currentScope;

    public void NavigateTo<TView>() where TView : UserControl
    {
        var newScope = App.AppHost.Services.CreateScope();
        var view = newScope.ServiceProvider.GetRequiredService<TView>();

        var oldScope = _currentScope;
        _currentScope = newScope;
        CurrentView = view;
        oldScope?.Dispose();
    }
}
