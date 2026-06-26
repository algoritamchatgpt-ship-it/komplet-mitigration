using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalpDefkWindow : Window
{
    public NalpDefkWindow(NalpDefkViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
        vm.DodatRed     += ScrollToNew;
    }

    private void ScrollToNew(NalpDefkRow row)
    {
        Grd0.ScrollIntoView(row);
        Grd0.CurrentItem = row;
        Grd0.BeginEdit();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            ((NalpDefkViewModel)DataContext).IzlazCommand.Execute(null);
            e.Handled = true;
        }
    }
}
