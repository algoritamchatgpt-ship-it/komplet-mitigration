using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Algoritam.WPF.ViewModels;

public partial class LdPrevParamViewModel : ObservableObject
{
    [ObservableProperty] private decimal _dana = 1m;
    [ObservableProperty] private decimal _karta;
    [ObservableProperty] private decimal _neoporez;
    [ObservableProperty] private DateTime _datum = DateTime.Today;
    [ObservableProperty] private int _mesec = DateTime.Today.Month;
    [ObservableProperty] private string _nazmes = string.Empty;

    public bool Potvrdjeno { get; private set; }
    public event Action? ZatvaranjeZahtevano;

    [RelayCommand]
    private void Unos()
    {
        Potvrdjeno = true;
        ZatvaranjeZahtevano?.Invoke();
    }

    [RelayCommand]
    private void Izlaz() => ZatvaranjeZahtevano?.Invoke();
}
