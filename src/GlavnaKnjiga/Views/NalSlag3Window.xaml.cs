using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalSlag3Window : Window
{
    public NalSlag3Window(NalSlag3ViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not NalSlag3ViewModel vm) return;
        if (e.Key == Key.Escape) { vm.IzlazCommand.Execute(null); e.Handled = true; }
    }
}
