using System.Windows.Controls;
using Abela.App.ViewModels;

namespace Abela.App.Views;

public partial class PaymentsView : UserControl
{
    public PaymentsView(PaymentsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
