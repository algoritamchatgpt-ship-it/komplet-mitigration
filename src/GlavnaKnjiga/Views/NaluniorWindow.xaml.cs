using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GlavnaKnjiga.Views;

public partial class NaluniorWindow : Window
{
    private static readonly SolidColorBrush BrArh  = new(Color.FromRgb(247, 245, 238));
    private static readonly SolidColorBrush BrNorm = new(Color.FromRgb(200, 255, 205));

    public NaluniorWindow(NaluniorViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }

    private void Grd0_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is UniorRow r)
            e.Row.Background = r.Arhiva.Trim() == "*" ? BrArh : BrNorm;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not NaluniorViewModel vm) return;
        if (e.Key == Key.Escape) { vm.IzlazCommand.Execute(null); e.Handled = true; }
    }
}
