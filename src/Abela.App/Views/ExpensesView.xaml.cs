using System.Windows.Controls;
using Abela.App.ViewModels;

namespace Abela.App.Views;

public partial class ExpensesView : UserControl
{
    public ExpensesView(ExpensesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
