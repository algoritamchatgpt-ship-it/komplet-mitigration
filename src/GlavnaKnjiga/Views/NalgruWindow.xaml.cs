using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalgruWindow : Window
{
    public NalgruWindow(NalgruViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
        vm.DodatRed     += ScrolliDodati;
    }

    private void ScrolliDodati()
    {
        if (DataContext is not NalgruViewModel vm || vm.SelektovaniRed == null) return;
        Grd0.ScrollIntoView(vm.SelektovaniRed);
        Grd0.BeginEdit();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is NalgruViewModel vm)
            vm.IzlazCommand.Execute(null);
    }
}
