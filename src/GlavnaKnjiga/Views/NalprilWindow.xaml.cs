using GlavnaKnjiga.ViewModels;
using GlavnaKnjiga.Models;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalprilWindow : Window
{
    private readonly string _firmPath;

    public NalprilWindow(NalprilViewModel vm, string firmPath)
    {
        InitializeComponent();
        DataContext = vm;
        _firmPath   = firmPath;
        vm.ZatvoriFormu    += Close;
        vm.OtvoriNalpril0  += OpenNalpril0;
        vm.OtvoriNalpril1  += OpenNalpril1;
        vm.OtvoriPregled   += OpenPregled;
    }

    private void OpenNalpril0()
    {
        var vm2  = new Nalpril0ViewModel(_firmPath);
        var win2 = new Nalpril0Window(vm2);
        win2.ShowDialog();
    }

    private void OpenNalpril1()
    {
        var vm = (NalprilViewModel)DataContext;
        var vm2 = new Nalpril1ViewModel(_firmPath);
        var win2 = new Nalpril1Window(vm2) { Owner = this };
        win2.ShowDialog();
        vm.Osvezi();
    }

    private void OpenPregled(string naslov, IReadOnlyList<NalprilPregledRow> redovi)
    {
        var vm = new NalprilPregledViewModel(naslov, redovi);
        new NalprilPregledWindow(vm) { Owner = this }.ShowDialog();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not NalprilViewModel vm) return;
        if (e.Key == Key.Escape) { vm.IzlazCommand.Execute(null); e.Handled = true; }
    }
}
