using OsnovnaSredstva.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace OsnovnaSredstva.Views;

public partial class OsSifarnikPopisnaListaWindow : Window
{
    public OsSifarnikPopisnaListaWindow(OsSifarnikPopisnaListaViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnZatvoriClick(object sender, RoutedEventArgs e) => Close();

    private void OnStampajClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is OsSifarnikPopisnaListaViewModel vm)
            vm.StampaCommand.Execute(null);
    }
}
