using GlavnaKnjiga.ViewModels;
using System.Windows;

namespace GlavnaKnjiga.Views;

public partial class NalzatWindow : Window
{
    public NalzatWindow(NalzatViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }
}
