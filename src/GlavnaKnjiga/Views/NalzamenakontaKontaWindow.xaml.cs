using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalzamenakontaKontaWindow : Window
{
    public NalzamenakontaKontaWindow(NalzamenakontaKontaViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
        vm.DodatRed += () =>
        {
            Grd0.ScrollIntoView(vm.SelektovaniRed);
            Grd0.BeginEdit();
        };
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not NalzamenakontaKontaViewModel vm) return;
        if (e.Key == Key.Escape) { vm.IzlazCommand.Execute(null); e.Handled = true; }
    }
}
