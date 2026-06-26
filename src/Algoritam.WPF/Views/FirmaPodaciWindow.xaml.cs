using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views;

public partial class FirmaPodaciWindow : Window
{
    public FirmaPodaciWindow(FirmaPodaciViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnZatvoriClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
