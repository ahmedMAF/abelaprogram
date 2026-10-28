using System.Windows.Controls;
using Abela.App.ViewModels;

namespace Abela.App.Views;

public partial class UsersView : UserControl
{
    public UsersView(UsersViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
