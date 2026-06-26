using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GlavnaKnjiga.Views;

public partial class NalprkonWindow : Window
{
    private static readonly SolidColorBrush BojaNeg = new(Color.FromRgb(255, 204, 204));
    private static readonly SolidColorBrush BojaPoz = new(Color.FromRgb(198, 255, 198));

    public NalprkonWindow(NalprkonViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }

    private void Grd0_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is NalprkonRow r)
            e.Row.Background = r.Saldo < 0 ? BojaNeg : BojaPoz;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not NalprkonViewModel vm) return;
        switch (e.Key)
        {
            case Key.F10: vm.KarticaF10Command.Execute(null); e.Handled = true; break;
            case Key.F6:  vm.TraziF6Command.Execute(null);    e.Handled = true; break;
            case Key.Escape: vm.IzlazCommand.Execute(null);   e.Handled = true; break;
        }
    }
}
