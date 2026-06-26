using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalzamkonuporWindow : Window
{
    public NalzamkonuporWindow(NalzamkonuporViewModel vm)
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
        if (DataContext is not NalzamkonuporViewModel vm) return;
        if (e.Key == Key.Escape) { vm.IzlazCommand.Execute(null); e.Handled = true; }
    }
}
