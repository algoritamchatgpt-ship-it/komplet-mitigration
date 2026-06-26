using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class Nalpril0Window : Window
{
    public Nalpril0Window(Nalpril0ViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not Nalpril0ViewModel vm) return;
        if (e.Key == Key.Escape) { vm.IzlazCommand.Execute(null); e.Handled = true; }
    }
}
