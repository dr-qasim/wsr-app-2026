using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace VendingService.WPF.Converters;

public sealed class EquipmentOkTotalToBrushConverter : IMultiValueConverter
{
    private static readonly Brush Green = new SolidColorBrush(Color.FromRgb(46, 125, 50));
    private static readonly Brush Orange = new SolidColorBrush(Color.FromRgb(245, 124, 0));
    private static readonly Brush Red = new SolidColorBrush(Color.FromRgb(211, 47, 47));
    private static readonly Brush Gray = new SolidColorBrush(Color.FromRgb(120, 120, 120));

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return Gray;
        }

        var ok = values[0] is int i1 ? i1 : TryParseInt(values[0]);
        var total = values[1] is int i2 ? i2 : TryParseInt(values[1]);

        if (ok is null || total is null || total.Value <= 0)
        {
            return Gray;
        }

        if (ok.Value <= 0)
        {
            return Red;
        }

        if (ok.Value >= total.Value)
        {
            return Green;
        }

        return Orange;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static int? TryParseInt(object? value) =>
        value is null ? null : int.TryParse(value.ToString(), out var i) ? i : null;
}

