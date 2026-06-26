using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalZak1bWindow : Window
{
    public NalZak1bWindow(NalZak1bViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu  += Close;
        vm.OtvoriNalZak2 += OpenNalZak2;
    }

    private void OpenNalZak2()
    {
        var vm2  = new NalZak2ViewModel();
        var win2 = new NalZak2Window(vm2);
        win2.ShowDialog();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not NalZak1bViewModel vm) return;
        if (e.Key == Key.Escape) { vm.IzlazCommand.Execute(null); e.Handled = true; }
    }
}
