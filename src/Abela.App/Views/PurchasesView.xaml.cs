using System.Windows.Controls;
using Abela.App.ViewModels;

namespace Abela.App.Views;

public partial class PurchasesView : UserControl
{
    public PurchasesView(PurchasesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
