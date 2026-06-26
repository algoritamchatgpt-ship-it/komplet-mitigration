using GlavnaKnjiga.ViewModels;
using System.Windows;

namespace GlavnaKnjiga.Views;

public partial class NalbrojKWindow : Window
{
    public NalbrojKWindow(NalbrojKViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += ok => { DialogResult = ok; Close(); };
        vm.IzborVrsteTrazena += () => OtvoriIzborVrste(vm);
    }

    private void TxtVrnal_LostFocus(object sender, RoutedEventArgs e)
        => ((NalbrojKViewModel)DataContext).OnVrnalLostFocus();

    private void TxtBrnal_LostFocus(object sender, RoutedEventArgs e)
        => ((NalbrojKViewModel)DataContext).OnBrnalLostFocus();

    private void OtvoriIzborVrste(NalbrojKViewModel vm)
    {
        var pickerVm = new NalvrstaPickerViewModel(vm.DostupneVrste);
        var win = new NalvrstaPickerWindow(pickerVm) { Owner = this };
        if (win.ShowDialog() == true && pickerVm.IzabraniVrnal != null)
            vm.PrimeniIzabranuVrstu(pickerVm.IzabraniVrnal);
    }
}
