using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalSlag2Window : Window
{
    private readonly string _firmPath;

    public NalSlag2Window(NalSlag2ViewModel vm, string firmPath)
    {
        InitializeComponent();
        DataContext = vm;
        _firmPath   = firmPath;
        vm.ZatvoriFormu  += Close;
        vm.OtvoriNalSlag3 += OpenNalSlag3;
    }

    private void OpenNalSlag3()
    {
        var vm2  = new NalSlag3ViewModel(_firmPath, (DataContext as NalSlag2ViewModel)!.Rows);
        var win2 = new NalSlag3Window(vm2);
        win2.ShowDialog();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not NalSlag2ViewModel vm) return;
        if (e.Key == Key.Escape) { vm.IzlazCommand.Execute(null); e.Handled = true; }
    }
}
