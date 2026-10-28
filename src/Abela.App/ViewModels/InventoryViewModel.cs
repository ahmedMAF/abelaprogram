using Abela.App.Common;
using System.Collections.ObjectModel;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Abela.App.ViewModels;

public partial class InventoryViewModel : ObservableObject
{
    private readonly InventoryService _inventoryService;
    private readonly ProductService _productService;
    private readonly IDialogService _dialog;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private DateTime fromDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime toDate = DateTime.Today;
    [ObservableProperty] private Product? selectedProductFilter;

    public ObservableCollection<InventoryTransaction> Movements { get; } = new();
    public ObservableCollection<Product> Products { get; } = new();

    public bool CanAdjust => _currentUser.HasPermission(Domain.Common.PermissionCodes.InventoryAdjust);

    public InventoryViewModel(InventoryService inventoryService, ProductService productService, IDialogService dialog, ICurrentUserService currentUser)
    {
        _inventoryService = inventoryService;
        _productService = productService;
        _dialog = dialog;
        _currentUser = currentUser;
        InitAsync().FireAndForgetSafe(_dialog, "المخزون");
    }

    private async Task InitAsync()
    {
        foreach (var p in await _productService.SearchAsync()) Products.Add(p);
        await SearchAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var results = await _inventoryService.GetMovementsAsync(SelectedProductFilter?.Id, FromDate, ToDate.AddDays(1).AddSeconds(-1));
        Movements.Clear();
        foreach (var m in results) Movements.Add(m);
    }

    [RelayCommand]
    private async Task NewStockAdjustmentAsync()
    {
        var window = new Views.StockAdjustmentWindow(_inventoryService, _productService, _currentUser, _dialog);
        if (window.ShowDialog() == true)
            await SearchAsync();
    }
}
