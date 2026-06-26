using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Views;

namespace GlavnaKnjiga.ViewModels;

public partial class NalzamenakontaViewModel : ObservableObject
{
    private readonly string _firmPath;

    public event Action? ZatvoriFormu;

    public NalzamenakontaViewModel(string firmPath) => _firmPath = firmPath;

    [RelayCommand] private void OtvoriKlase()
    {
        var vm = new NalzamenakontaKontaViewModel(_firmPath, "kon1n", "K1", "kon1", "KONTA KLASA");
        new NalzamenakontaKontaWindow(vm).ShowDialog();
    }

    [RelayCommand] private void OtvoriGrupe()
    {
        var vm = new NalzamenakontaKontaViewModel(_firmPath, "kon2n", "K2", "kon2", "KONTA GRUPE");
        new NalzamenakontaKontaWindow(vm).ShowDialog();
    }

    [RelayCommand] private void OtvoriSintetika()
    {
        var vm = new NalzamenakontaKontaViewModel(_firmPath, "kon3n", "K3", "kon3", "KONTA SINTETIKE");
        new NalzamenakontaKontaWindow(vm).ShowDialog();
    }

    [RelayCommand] private void OtvoriAnalitika10()
    {
        var vm = new NalzamenakontaKontaViewModel(_firmPath, "konton", "KONTO", "konto", "ANALITIKA 10");
        new NalzamenakontaKontaWindow(vm).ShowDialog();
    }

    [RelayCommand] private void OtvoriPreknjizbKonta()
    {
        var vm = new NalzamenakontaZamViewModel(_firmPath);
        new NalzamenakontaZamWindow(vm).ShowDialog();
    }

    [RelayCommand] private void OtvoriUporedniPregled()
    {
        var vm = new NalzamkonuporViewModel(_firmPath);
        new NalzamkonuporWindow(vm).ShowDialog();
    }

    [RelayCommand] private void Izlaz() => ZatvoriFormu?.Invoke();
}
