using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class Nalp2KartMpWindow : Window
{
    public Nalp2KartMpWindow(Nalp2KartMpViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
        vm.IzborOpisaTrazena += vm.OtvoriTmMeni;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            ((Nalp2KartMpViewModel)DataContext).IzlazCommand.Execute(null);
            e.Handled = true;
        }
    }
}
