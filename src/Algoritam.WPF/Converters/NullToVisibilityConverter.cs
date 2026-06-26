using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Algoritam.WPF.Converters;

/// <summary>Visible kad je vrednost NULL (placeholder kad nema selekcije).</summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        => value is null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Visible kad vrednost NIJE NULL (prikazuje detalje selektovanog).</summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        => value is not null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
