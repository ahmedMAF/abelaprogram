using System.Windows.Controls;
using Abela.App.ViewModels;

namespace Abela.App.Views;

public partial class InventoryView : UserControl
{
    public InventoryView(InventoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
