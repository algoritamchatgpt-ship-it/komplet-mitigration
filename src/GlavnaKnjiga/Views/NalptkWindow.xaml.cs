using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalptkWindow : Window
{
    public NalptkWindow(NalptkViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not NalptkViewModel vm)
            return;

        if (e.Key == Key.Escape)
        {
            vm.IzlazCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
        {
            vm.DodajUNalogCommand.Execute(null);
            e.Handled = true;
        }
    }
}
