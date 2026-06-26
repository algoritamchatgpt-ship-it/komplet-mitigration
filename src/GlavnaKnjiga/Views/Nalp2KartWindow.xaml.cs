using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class Nalp2KartWindow : Window
{
    public Nalp2KartWindow(Nalp2KartViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            ((Nalp2KartViewModel)DataContext).IzlazCommand.Execute(null);
            e.Handled = true;
        }
    }
}
