using System.Globalization;
using System.Windows.Data;
using Abela.Domain.Enums;

namespace Abela.App.Common;

public class InventoryTransactionTypeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        InventoryTransactionType.OpeningBalance => "رصيد افتتاحي",
        InventoryTransactionType.Purchase => "شراء",
        InventoryTransactionType.Sale => "بيع",
        InventoryTransactionType.SalesReturn => "مرتجع بيع",
        InventoryTransactionType.PurchaseReturn => "مرتجع شراء",
        InventoryTransactionType.AdjustmentIncrease => "تسوية زيادة",
        InventoryTransactionType.AdjustmentDecrease => "تسوية نقصان",
        InventoryTransactionType.InvoiceCancellation => "إلغاء فاتورة",
        _ => value?.ToString() ?? ""
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ReferenceTypeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) => (value as string) switch
    {
        "OpeningBalance" => "رصيد افتتاحي",
        "SalesInvoice" => "فاتورة بيع",
        "PurchaseInvoice" => "فاتورة شراء",
        "SalesReturn" => "مرتجع مبيعات",
        "PurchaseReturn" => "مرتجع مشتريات",
        "StockAdjustment" => "تسوية مخزون",
        "SalesInvoiceCancel" => "إلغاء فاتورة بيع",
        "PurchaseInvoiceCancel" => "إلغاء فاتورة شراء",
        null => "",
        var s => s
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
