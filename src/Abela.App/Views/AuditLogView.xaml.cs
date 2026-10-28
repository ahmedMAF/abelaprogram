using System.Windows.Controls;
using Abela.App.ViewModels;

namespace Abela.App.Views;

public partial class AuditLogView : UserControl
{
    public AuditLogView(AuditLogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
