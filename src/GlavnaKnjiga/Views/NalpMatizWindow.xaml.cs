using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalpMatizWindow : Window
{
    public NalpMatizWindow(NalpMatizViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
        Title = "EVIDENCIJA IZLAZA MATERIJALA";
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not NalpMatizViewModel vm) return;
        if (e.Key == Key.Escape) { vm.IzlazCommand.Execute(null); e.Handled = true; }
    }
}
