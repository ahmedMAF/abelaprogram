using Abela.App.Common;
using System.Collections.ObjectModel;
using Abela.Application.Common;
using Abela.Application.Services;
using Abela.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Abela.App.ViewModels;

public partial class ProductsViewModel : ObservableObject
{
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;
    private readonly UnitService _unitService;
    private readonly IDialogService _dialog;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string searchTerm = string.Empty;
    [ObservableProperty] private ProductCategory? selectedCategoryFilter;
    [ObservableProperty] private bool lowStockOnly;
    [ObservableProperty] private Product? selectedProduct;

    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<ProductCategory> Categories { get; } = new();
    public ObservableCollection<Unit> Units { get; } = new();

    public bool CanAdd => _currentUser.HasPermission(Domain.Common.PermissionCodes.ProductsAdd);
    public bool CanEdit => _currentUser.HasPermission(Domain.Common.PermissionCodes.ProductsEdit);

    public ProductsViewModel(ProductService productService, CategoryService categoryService, UnitService unitService, IDialogService dialog, ICurrentUserService currentUser)
    {
        _productService = productService;
        _categoryService = categoryService;
        _unitService = unitService;
        _dialog = dialog;
        _currentUser = currentUser;
        InitAsync().FireAndForgetSafe(_dialog, "المنتجات");
    }

    private async Task InitAsync()
    {
        await LoadLookupsAsync();
        await SearchAsync();
    }

    public async Task LoadLookupsAsync()
    {
        Categories.Clear();
        foreach (var c in await _categoryService.GetAllAsync()) Categories.Add(c);
        Units.Clear();
        foreach (var u in await _unitService.GetAllAsync()) Units.Add(u);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var results = await _productService.SearchAsync(SearchTerm, SelectedCategoryFilter?.Id, LowStockOnly);
        Products.Clear();
        foreach (var p in results) Products.Add(p);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var code = await _productService.GenerateCodeAsync();
        var product = new Product { Code = code, RequiresSerialNumber = false, IsActive = true };
        var window = new Views.ProductEditWindow(product, Categories, Units, isNew: true);
        if (window.ShowDialog() == true)
        {
            var result = await _productService.SaveAsync(product);
            if (result.Success) { _dialog.ShowSuccess(result.Message); await SearchAsync(); }
            else _dialog.ShowError(result.Message);
        }
    }

    [RelayCommand]
    private async Task EditAsync()
    {
        if (SelectedProduct is null) { _dialog.ShowInfo("الرجاء اختيار منتج"); return; }
        var full = await _productService.GetByIdAsync(SelectedProduct.Id);
        if (full is null) return;
        var window = new Views.ProductEditWindow(full, Categories, Units, isNew: false);
        if (window.ShowDialog() == true)
        {
            var result = await _productService.SaveAsync(full);
            if (result.Success) { _dialog.ShowSuccess(result.Message); await SearchAsync(); }
            else _dialog.ShowError(result.Message);
        }
    }

    [RelayCommand]
    private async Task DeactivateAsync()
    {
        if (SelectedProduct is null) { _dialog.ShowInfo("الرجاء اختيار منتج"); return; }
        if (!_dialog.Confirm($"هل تريد تعطيل المنتج {SelectedProduct.Name}؟")) return;
        var result = await _productService.DeactivateAsync(SelectedProduct.Id);
        if (result.Success) { _dialog.ShowSuccess(result.Message); await SearchAsync(); }
        else _dialog.ShowError(result.Message);
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedProduct is null) { _dialog.ShowInfo("الرجاء اختيار منتج"); return; }
        if (!_dialog.Confirm($"هل تريد حذف المنتج {SelectedProduct.Name} نهائيًا؟")) return;
        var result = await _productService.DeleteAsync(SelectedProduct.Id);
        if (result.Success) { _dialog.ShowSuccess(result.Message); await SearchAsync(); }
        else _dialog.ShowError(result.Message);
    }

    [RelayCommand]
    private async Task ManageCategoriesAsync()
    {
        var window = new Views.CategoryManagerWindow(_categoryService, _dialog);
        window.ShowDialog();
        await LoadLookupsAsync();
    }

    [RelayCommand]
    private async Task ManageUnitsAsync()
    {
        var window = new Views.UnitManagerWindow(_unitService, _dialog);
        window.ShowDialog();
        await LoadLookupsAsync();
    }
}
