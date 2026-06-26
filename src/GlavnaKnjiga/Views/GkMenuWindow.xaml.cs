using GlavnaKnjiga.ViewModels;
using System.Windows;

namespace GlavnaKnjiga.Views;

public partial class GkMenuWindow : Window
{
    public GkMenuWindow(GkMenuViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        vm.OdjavaSeTrazena += () => { /* handled in App */ };
        vm.VratiseFirmaIzboru += () => { /* handled in App */ };
    }
}
