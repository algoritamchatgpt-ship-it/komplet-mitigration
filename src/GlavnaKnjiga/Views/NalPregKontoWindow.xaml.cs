using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalPregKontoWindow : Window
{
    public NalPregKontoWindow(NalPregKontoViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not NalPregKontoViewModel vm)
            return;

        if (e.Key == Key.Escape)
        {
            vm.IzlazCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
        {
            vm.PregledCommand.Execute(null);
            e.Handled = true;
        }
    }
}
