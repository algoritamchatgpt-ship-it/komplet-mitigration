using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalpNWindow : Window
{
    public NalpNWindow(NalpNViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }

    private void Grd0_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Grd0.SelectedItem != null)
            Grd0.ScrollIntoView(Grd0.SelectedItem);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        var vm = (NalpNViewModel)DataContext;
        switch (e.Key)
        {
            case Key.F7 when Keyboard.IsKeyDown(Key.LeftShift)
                          || Keyboard.IsKeyDown(Key.RightShift):
                vm.PrazniNalogCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Escape:
                vm.IzlazCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
