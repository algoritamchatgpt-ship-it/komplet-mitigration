using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalprNalWindow : Window
{
    public NalprNalWindow(NalprNalViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not NalprNalViewModel vm) return;
        switch (e.Key)
        {
            case Key.F10:    vm.NalogSintetikaF10Command.Execute(null); e.Handled = true; break;
            case Key.F6:     vm.TraziF6Command.Execute(null);           e.Handled = true; break;
            case Key.Escape: vm.IzlazCommand.Execute(null);             e.Handled = true; break;
        }
    }
}
