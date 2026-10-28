using System.Windows.Controls;
using Abela.App.ViewModels;

namespace Abela.App.Views;

public partial class SalesView : UserControl
{
    public SalesView(SalesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
