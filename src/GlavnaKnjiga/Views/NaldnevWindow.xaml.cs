using GlavnaKnjiga.ViewModels;
using System.Windows;

namespace GlavnaKnjiga.Views;

public partial class NaldnevWindow : Window
{
    public NaldnevWindow(NaldnevViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }
}
