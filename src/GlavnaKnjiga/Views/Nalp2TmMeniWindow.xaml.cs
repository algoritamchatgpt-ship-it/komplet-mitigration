using GlavnaKnjiga.ViewModels;
using System.Windows;

namespace GlavnaKnjiga.Views;

public partial class Nalp2TmMeniWindow : Window
{
    public Nalp2TmMeniWindow(Nalp2TmMeniViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }
}
