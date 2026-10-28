using System.Windows.Controls;
using Abela.App.ViewModels;

namespace Abela.App.Views;

public partial class BackupView : UserControl
{
    public BackupView(BackupViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
