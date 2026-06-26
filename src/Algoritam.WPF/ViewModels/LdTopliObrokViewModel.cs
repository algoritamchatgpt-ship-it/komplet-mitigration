using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Dialog za unos iznosa toplog obroka i regresa.
/// Ekvivalent FoxPro forme LDTOPLI.SCX (PRENOSI → TOPLI OBROK I REGRES).
/// Korisnik unosi isti iznos koji dobija svaki radnik.
/// </summary>
public partial class LdTopliObrokViewModel : ObservableObject
{
    [ObservableProperty] private decimal _topliIznos;
    [ObservableProperty] private decimal _regresIznos;
    [ObservableProperty] private string _poruka = "Unesite iznose za sve radnike i kliknite OBRAČUN.";

    public bool Potvrdjeno { get; private set; }

    public event Action? ZatvaranjeZahtevano;

    [RelayCommand]
    private void Obracun()
    {
        if (TopliIznos == 0m && RegresIznos == 0m)
        {
            Poruka = "Unesite bar jedan iznos.";
            return;
        }

        Potvrdjeno = true;
        ZatvaranjeZahtevano?.Invoke();
    }

    [RelayCommand]
    private void Izlaz()
    {
        Potvrdjeno = false;
        ZatvaranjeZahtevano?.Invoke();
    }
}
