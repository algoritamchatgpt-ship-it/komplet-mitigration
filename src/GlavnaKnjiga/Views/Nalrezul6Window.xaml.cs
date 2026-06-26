using GlavnaKnjiga.ViewModels;
using System.Windows;

namespace GlavnaKnjiga.Views;

public partial class Nalrezul6Window : Window
{
    public Nalrezul6Window(Nalrezul6ViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }
}
