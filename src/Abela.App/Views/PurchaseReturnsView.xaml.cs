using System.Windows.Controls;
using Abela.App.ViewModels;

namespace Abela.App.Views;

public partial class PurchaseReturnsView : UserControl
{
    public PurchaseReturnsView(PurchaseReturnsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
