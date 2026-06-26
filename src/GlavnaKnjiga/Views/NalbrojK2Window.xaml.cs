using GlavnaKnjiga.ViewModels;
using System.Windows;

namespace GlavnaKnjiga.Views;

public partial class NalbrojK2Window : Window
{
    public NalbrojK2Window(NalbrojK2ViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += ok => { DialogResult = ok; Close(); };
        vm.IzborVrsteTrazena += () => OtvoriIzborVrste(vm);
    }

    private void TxtVrnal_LostFocus(object sender, RoutedEventArgs e)
        => ((NalbrojK2ViewModel)DataContext).OnVrnalLostFocus();

    private void TxtBrnal_LostFocus(object sender, RoutedEventArgs e)
        => ((NalbrojK2ViewModel)DataContext).OnBrnalLostFocus();

    private void OtvoriIzborVrste(NalbrojK2ViewModel vm)
    {
        var pickerVm = new NalvrstaPickerViewModel(vm.DostupneVrste);
        var win = new NalvrstaPickerWindow(pickerVm) { Owner = this };
        if (win.ShowDialog() == true && pickerVm.IzabraniVrnal != null)
            vm.PrimeniIzabranuVrstu(pickerVm.IzabraniVrnal);
    }
}
