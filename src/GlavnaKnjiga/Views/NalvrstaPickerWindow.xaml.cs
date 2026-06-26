using GlavnaKnjiga.ViewModels;
using System.Windows;

namespace GlavnaKnjiga.Views;

public partial class NalvrstaPickerWindow : Window
{
    public NalvrstaPickerWindow(NalvrstaPickerViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += ok =>
        {
            DialogResult = ok;
            Close();
        };
    }
}
