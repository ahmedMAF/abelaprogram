using System.Windows.Controls;
using Abela.App.ViewModels;

namespace Abela.App.Views;

public partial class SuppliersView : UserControl
{
    public SuppliersView(SuppliersViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
