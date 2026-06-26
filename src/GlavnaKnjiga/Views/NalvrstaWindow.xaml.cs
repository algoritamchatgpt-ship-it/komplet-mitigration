using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalvrstaWindow : Window
{
    public NalvrstaWindow(NalvrstaViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }

    // F7 = KARTICA, Shift+F10 = BRISANJE
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        var vm = (NalvrstaViewModel)DataContext;
        if (e.Key == Key.F7) { vm.KarticaF7Command.Execute(null); e.Handled = true; }
        if (e.Key == Key.F8) { vm.SabloniF8Command.Execute(null); e.Handled = true; }
        if (e.Key == Key.F10 && (Keyboard.Modifiers & ModifierKeys.Shift) != 0)
        {
            vm.BrisanjeF10Command.Execute(null);
            e.Handled = true;
        }
        // Enter — ako je picker mod, izaberi
        if (e.Key == Key.Return && vm.JePiker)
        {
            vm.IzaberiSelektovani();
            e.Handled = true;
        }
    }

    // Double-click — u picker modu izaberi, inače KARTICA
    private void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        var vm = (NalvrstaViewModel)DataContext;
        if (vm.JePiker)
            vm.IzaberiSelektovani();
        else
            vm.KarticaF7Command.Execute(null);
    }
}
