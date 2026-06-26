using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalbrojWindow : Window
{
    public NalbrojWindow(NalbrojViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }

    // F7 = KARTICA, F6 = TRAŽENJE
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        var vm = (NalbrojViewModel)DataContext;
        if (e.Key == Key.F7) { vm.KarticaF7Command.Execute(null); e.Handled = true; }
        if (e.Key == Key.F6) { vm.TraziF6Command.Execute(null);   e.Handled = true; }
    }

    // Double-click na red → KARTICA F7
    private void Grid_DoubleClick(object sender, MouseButtonEventArgs e)
        => ((NalbrojViewModel)DataContext).KarticaF7Command.Execute(null);
}
