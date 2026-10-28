using System.Windows.Controls;
using Abela.App.ViewModels;

namespace Abela.App.Views;

public partial class ProductsView : UserControl
{
    public ProductsView(ProductsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
