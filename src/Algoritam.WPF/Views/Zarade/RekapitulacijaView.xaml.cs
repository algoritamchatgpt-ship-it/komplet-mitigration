using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Algoritam.WPF.Views.Zarade;

public partial class RekapitulacijaView : Window
{
    public RekapitulacijaView()
    {
        InitializeComponent();
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

/// <summary>
/// Converter koji vraća true ako je decimal vrednost negativna (za crvenu boju u Razlika koloni).
/// </summary>
public class NegativeConverter : IValueConverter
{
    public static readonly NegativeConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return d < 0;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
