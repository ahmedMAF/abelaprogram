using System.Windows.Controls;
using Abela.App.ViewModels;

namespace Abela.App.Views;

public partial class CustomersView : UserControl
{
    public CustomersView(CustomersViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
