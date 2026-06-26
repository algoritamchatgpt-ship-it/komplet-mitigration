using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Algoritam.WPF.ViewModels;

public partial class LdPrenosKreditaViewModel : ObservableObject
{
    private readonly PlatniSpisakViewModel _platniSpisak;

    [ObservableProperty] private string _naslov = "PRENOS KREDITA U OBRACUN";
    [ObservableProperty] private string _statusPoruka = "DA LI ZELITE PRENOS KREDITA";

    public event Action? ZatvaranjeZahtevano;

    public LdPrenosKreditaViewModel(PlatniSpisakViewModel platniSpisak)
    {
        _platniSpisak = platniSpisak;
    }

    [RelayCommand]
    private void ZelimPrenosKredita()
    {
        StatusPoruka = _platniSpisak.IzvrsiPrenosKredita(zaAkontaciju: false);
    }

    [RelayCommand]
    private void ZelimPrenosAkontacije()
    {
        StatusPoruka = _platniSpisak.IzvrsiPrenosKredita(zaAkontaciju: true);
    }

    [RelayCommand]
    private void Odustajem()
    {
        StatusPoruka = "NIJE IZVRSEN PRENOS KREDITA";
    }

    [RelayCommand]
    private void Izlaz()
    {
        ZatvaranjeZahtevano?.Invoke();
    }
}
