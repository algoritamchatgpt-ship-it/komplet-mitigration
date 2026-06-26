using GlavnaKnjiga.ViewModels;
using System.Windows;

namespace GlavnaKnjiga.Views;

public partial class NalvrstaKWindow : Window
{
    public NalvrstaKWindow(NalvrstaKViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += ok => { DialogResult = ok; Close(); };
    }
}
