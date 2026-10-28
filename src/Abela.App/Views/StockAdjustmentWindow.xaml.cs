using System.Windows;
using Abela.Application.Common;
using Abela.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Abela.App.Views;

public partial class StockAdjustmentLineVM : ObservableObject
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal SystemQuantity { get; set; }

    private decimal _actualQuantity;
    public decimal ActualQuantity
    {
        get => _actualQuantity;
        set { SetProperty(ref _actualQuantity, value); Difference = value - SystemQuantity; }
    }

    [ObservableProperty] private decimal difference;
    [ObservableProperty] private string? reason;
}

public partial class StockAdjustmentWindow : Window
{
    private readonly InventoryService _inventoryService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;
    private readonly List<StockAdjustmentLineVM> _lines = new();

    public StockAdjustmentWindow(InventoryService inventoryService, ProductService productService, ICurrentUserService currentUser, IDialogService dialog)
    {
        InitializeComponent();
        if (Owner is null && System.Windows.Application.Current.MainWindow != this) Owner = System.Windows.Application.Current.MainWindow;
        _inventoryService = inventoryService;
        _currentUser = currentUser;
        _dialog = dialog;
        _ = LoadAsync(productService);
    }

    private async Task LoadAsync(ProductService productService)
    {
        var products = await productService.SearchAsync();
        foreach (var p in products.Where(p => p.IsActive))
        {
            _lines.Add(new StockAdjustmentLineVM { ProductId = p.Id, ProductName = p.Name, SystemQuantity = p.CurrentQuantity, ActualQuantity = p.CurrentQuantity });
        }
        ItemsGrid.ItemsSource = _lines;
    }

    private async void Execute_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        var changed = _lines.Where(l => l.Difference != 0).ToList();
        if (changed.Count == 0) { ErrorText.Text = "لا توجد فروقات لتنفيذها"; return; }

        if (!_dialog.Confirm($"سيتم تعديل كميات {changed.Count} صنف بناءً على الفروقات المدخلة. هل أنت متأكد من تنفيذ التسوية؟"))
            return;

        var lines = changed.Select(l => (l.ProductId, l.ActualQuantity, l.Reason)).ToList();
        var result = await _inventoryService.CreateStockAdjustmentAsync(lines, NotesBox.Text, _currentUser.UserId, allowNegativeStock: true);
        if (!result.Success) { ErrorText.Text = result.Message; return; }

        _dialog.ShowSuccess(result.Message);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
