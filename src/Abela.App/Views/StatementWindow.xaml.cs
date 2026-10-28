using System.Windows;
using Abela.Application.Services;

namespace Abela.App.Views;

public partial class StatementWindow : Window
{
    public StatementWindow(string title, List<StatementRow> rows, decimal currentBalance)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        TitleText.Text = title;
        BalanceText.Text = $"الرصيد الحالي: {currentBalance:N2}";
        RowsGrid.ItemsSource = rows;
    }
}
