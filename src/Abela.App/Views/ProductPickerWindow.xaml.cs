using System.Windows;
using System.Windows.Input;
using Abela.Application.Services;
using Abela.Domain.Entities;

namespace Abela.App.Views;

public partial class ProductPickerWindow : Window
{
    private readonly ProductService _productService;
    public Product? SelectedProduct { get; private set; }

    public ProductPickerWindow(ProductService productService)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        _productService = productService;
        _ = SearchAsync();
    }

    private async Task SearchAsync()
    {
        var results = await _productService.SearchAsync(SearchBox.Text);
        ResultsGrid.ItemsSource = results.Where(p => p.IsActive).ToList();
    }

    private async void Search_Click(object sender, RoutedEventArgs e) => await SearchAsync();
    private async void SearchBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) await SearchAsync(); }

    private void ResultsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => Confirm();
    private void Select_Click(object sender, RoutedEventArgs e) => Confirm();

    private void Confirm()
    {
        if (ResultsGrid.SelectedItem is Product p)
        {
            SelectedProduct = p;
            DialogResult = true;
            Close();
        }
    }
}
