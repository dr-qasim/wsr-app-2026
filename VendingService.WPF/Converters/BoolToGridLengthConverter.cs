using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VendingService.WPF.Converters;

public sealed class BoolToGridLengthConverter : IValueConverter
{
    public double TrueLength { get; set; } = 60;
    public double FalseLength { get; set; } = 220;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var flag = value is true;
        return new GridLength(flag ? TrueLength : FalseLength);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

