using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalWindow : Window
{
    public NalWindow(NalViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
        vm.PozicioniranjeTrazena += Pozicioniraj;
    }

    private void Pozicioniraj(NalpRow red)
    {
        GrdNal.ScrollIntoView(red);
        GrdNal.Focus();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not NalViewModel vm)
            return;

        if (e.Key == Key.F6)
        {
            vm.PregledKontaCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F10)
        {
            vm.PregledNalogaCommand.Execute(null);
            e.Handled = true;
        }
    }
}
