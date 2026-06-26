using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalplobrWindow : Window
{
    private readonly string _firmPath;

    public NalplobrWindow(NalplobrViewModel vm, string firmPath)
    {
        InitializeComponent();
        DataContext = vm;
        _firmPath   = firmPath;
        vm.ZatvoriFormu  += Close;
        vm.OtvoriNalPlDat += OpenNalPlDat;
    }

    private void OpenNalPlDat()
    {
        var vm  = new NalPlDatViewModel(_firmPath);
        var win = new NalPlDatWindow(vm);
        win.Owner = this;
        win.ShowDialog();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not NalplobrViewModel vm) return;
        if (e.Key == Key.Escape) { vm.IzlazCommand.Execute(null); e.Handled = true; }
    }
}
