using Abela.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Abela.App.ViewModels;

public partial class SalesLineVM : ObservableObject
{
    public Product Product { get; set; } = null!;

    [ObservableProperty] private decimal quantity = 1;
    [ObservableProperty] private decimal unitPrice;
    [ObservableProperty] private decimal discountAmount;

    public List<string> SerialNumbers { get; set; } = new();

    public decimal LineTotal => (Quantity * UnitPrice) - DiscountAmount;

    partial void OnQuantityChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
    partial void OnDiscountAmountChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
}

public partial class PurchaseLineVM : ObservableObject
{
    public Product Product { get; set; } = null!;

    [ObservableProperty] private decimal quantity = 1;
    [ObservableProperty] private decimal unitCost;
    [ObservableProperty] private decimal discountAmount;

    public List<string> SerialNumbers { get; set; } = new();

    public decimal LineTotal => (Quantity * UnitCost) - DiscountAmount;

    partial void OnQuantityChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
    partial void OnUnitCostChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
    partial void OnDiscountAmountChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
}
