using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Algoritam.WPF.ViewModels;

public partial class LdPrevSifraViewModel : ObservableObject
{
    [ObservableProperty] private string _sifraprih = "101110000";

    public bool Potvrdjeno { get; private set; }
    public event Action? ZatvaranjeZahtevano;

    [RelayCommand]
    private void Potvrdi()
    {
        Potvrdjeno = true;
        ZatvaranjeZahtevano?.Invoke();
    }

    [RelayCommand]
    private void Izlaz() => ZatvaranjeZahtevano?.Invoke();
}
