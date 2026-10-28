using System.Windows.Controls;
using Abela.App.ViewModels;

namespace Abela.App.Views;

public partial class ReportsView : UserControl
{
    private readonly ReportsViewModel _viewModel;

    public ReportsView(ReportsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void ResultGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (_viewModel.CurrentHeaderMap.TryGetValue(e.PropertyName, out var arabicHeader))
            e.Column.Header = arabicHeader;
    }
}
