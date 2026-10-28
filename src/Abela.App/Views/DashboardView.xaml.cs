using System.Windows.Controls;
using Abela.App.ViewModels;

namespace Abela.App.Views;

public partial class DashboardView : UserControl
{
    public DashboardView(DashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
