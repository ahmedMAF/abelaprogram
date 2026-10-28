using System.Windows.Controls;
using Abela.App.ViewModels;

namespace Abela.App.Views;

public partial class SalesReturnsView : UserControl
{
    public SalesReturnsView(SalesReturnsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
