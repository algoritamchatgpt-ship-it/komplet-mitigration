using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Dialog za popunjavanje jedne kolone u platnom spisku — F8.
/// Ekvivalent FoxPro forme LDPOPUNIKOL.SCX.
/// Korisnik bira kolonu i unosi vrednost koja se upisuje svim radnicima.
/// </summary>
public partial class LdPopuniKolonuViewModel : ObservableObject
{
    public static readonly IReadOnlyList<string> DostupneKolone = new[]
    {
        "CASUC", "CASNOC", "CASPROD", "CASRADNAP", "CASNED",
        "CASDOR", "CSLPUT", "CASPRAZ", "CASBOL", "CASBOL2",
        "CASPLAC", "CASPLAC2", "CASGOD", "CASVV", "CASSUS",
        "TOPLI", "REGRES", "TERENSKI", "FIKSNA",
        "STIM1PROC", "STIM2PROC", "STIM3PROC",
        "PREVOZ"
    };

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UnesiCommand))]
    private string _izabranaKolona = "TOPLI";

    [ObservableProperty] private decimal _vrednost;
    [ObservableProperty] private string _poruka = "Izaberite kolonu i unesite vrednost.";

    public bool Potvrdjeno { get; private set; }

    public event Action? ZatvaranjeZahtevano;

    private bool MozeUneti() => !string.IsNullOrWhiteSpace(IzabranaKolona);

    [RelayCommand(CanExecute = nameof(MozeUneti))]
    private void Unesi()
    {
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
