using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalbumtrWindow : Window
{
    public NalbumtrWindow(NalbumtrViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is NalbumtrViewModel vm)
            vm.IzlazCommand.Execute(null);
    }
}
