using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalpMatulWindow : Window
{
    public NalpMatulWindow(NalpMatulViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
        Title = "EVIDENCIJA ULAZA MATERIJALA";
    }

    private void TxtUlaz_LostFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is NalpMatulViewModel vm) vm.OnUlazLostFocus();
    }

    private void TxtCena_LostFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is NalpMatulViewModel vm) vm.OnCenaLostFocus();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not NalpMatulViewModel vm) return;
        if (e.Key == Key.Escape) { vm.IzlazCommand.Execute(null); e.Handled = true; }
    }
}
