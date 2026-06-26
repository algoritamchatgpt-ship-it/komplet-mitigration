using OsnovnaSredstva.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace OsnovnaSredstva.Views;

public partial class OsSaldoPoMestuWindow : Window
{
    public OsSaldoPoMestuWindow(OsSaldoPoMestuViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvaranjeZahtevano += Close;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not OsSaldoPoMestuViewModel vm)
            return;

        if (e.Key == Key.Escape)
        {
            vm.IzlazCommand.Execute(null);
            e.Handled = true;
        }
    }
}
