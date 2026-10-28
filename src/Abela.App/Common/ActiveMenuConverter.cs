using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Abela.App.Common;

/// <summary>Returns a highlighted brush when the bound ActiveMenu value matches the button's ConverterParameter key.</summary>
public class ActiveMenuToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        var isActive = string.Equals(value as string, parameter as string, StringComparison.Ordinal);
        return isActive ? new SolidColorBrush(Color.FromRgb(0x1F, 0x6F, 0xEB)) : Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
