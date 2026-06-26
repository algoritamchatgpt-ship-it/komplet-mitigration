using Algoritam.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// ViewModel za dijalog unosa časova — editovanje CASxx polja za jednog radnika.
/// Ekvivalent FoxPro forme LDUNOSCAS.
/// </summary>
public partial class UnosCasovaViewModel : ObservableObject
{
    private readonly LdObracunStavka _izvornaStavka;
    private readonly LdObracunStavka _radnaStavka;
    private readonly string _naslovRadnika;

    public string NaslovRadnika => _naslovRadnika;
    public int FondCasova { get; }

    // ── Časovi — bindovani na stavku ─────────────────────────────────
    public decimal Casuc { get => _radnaStavka.Casuc; set { _radnaStavka.Casuc = value; OnPropertyChanged(); } }
    public decimal Casnoc { get => _radnaStavka.Casnoc; set { _radnaStavka.Casnoc = value; OnPropertyChanged(); } }
    public decimal Casprod { get => _radnaStavka.Casprod; set { _radnaStavka.Casprod = value; OnPropertyChanged(); } }
    public decimal Casradnap { get => _radnaStavka.Casradnap; set { _radnaStavka.Casradnap = value; OnPropertyChanged(); } }
    public decimal Casned { get => _radnaStavka.Casned; set { _radnaStavka.Casned = value; OnPropertyChanged(); } }
    public decimal Casdor { get => _radnaStavka.Casdor; set { _radnaStavka.Casdor = value; OnPropertyChanged(); } }
    public decimal Cslput { get => _radnaStavka.Cslput; set { _radnaStavka.Cslput = value; OnPropertyChanged(); } }
    public decimal Caspraz { get => _radnaStavka.Caspraz; set { _radnaStavka.Caspraz = value; OnPropertyChanged(); } }
    public decimal Casbol { get => _radnaStavka.Casbol; set { _radnaStavka.Casbol = value; OnPropertyChanged(); } }
    public decimal Casbol2 { get => _radnaStavka.Casbol2; set { _radnaStavka.Casbol2 = value; OnPropertyChanged(); } }
    public decimal Casplac { get => _radnaStavka.Casplac; set { _radnaStavka.Casplac = value; OnPropertyChanged(); } }
    public decimal Casplac2 { get => _radnaStavka.Casplac2; set { _radnaStavka.Casplac2 = value; OnPropertyChanged(); } }
    public decimal Casgod { get => _radnaStavka.Casgod; set { _radnaStavka.Casgod = value; OnPropertyChanged(); } }
    public decimal Casvv { get => _radnaStavka.Casvv; set { _radnaStavka.Casvv = value; OnPropertyChanged(); } }
    public decimal Cas1 { get => _radnaStavka.Cas1; set { _radnaStavka.Cas1 = value; OnPropertyChanged(); } }
    public decimal Cas2 { get => _radnaStavka.Cas2; set { _radnaStavka.Cas2 = value; OnPropertyChanged(); } }
    public decimal Cas3 { get => _radnaStavka.Cas3; set { _radnaStavka.Cas3 = value; OnPropertyChanged(); } }
    public decimal Cassus { get => _radnaStavka.Cassus; set { _radnaStavka.Cassus = value; OnPropertyChanged(); } }
    public decimal Casneplac { get => _radnaStavka.Casneplac; set { _radnaStavka.Casneplac = value; OnPropertyChanged(); } }
    public decimal Caspriprav { get => _radnaStavka.Caspriprav; set { _radnaStavka.Caspriprav = value; OnPropertyChanged(); } }

    // ── Dodaci (dinarski) ────────────────────────────────────────────
    public decimal Topli { get => _radnaStavka.Topli; set { _radnaStavka.Topli = value; OnPropertyChanged(); } }
    public decimal Regres { get => _radnaStavka.Regres; set { _radnaStavka.Regres = value; OnPropertyChanged(); } }
    public decimal Terenski { get => _radnaStavka.Terenski; set { _radnaStavka.Terenski = value; OnPropertyChanged(); } }
    public decimal Fiksna { get => _radnaStavka.Fiksna; set { _radnaStavka.Fiksna = value; OnPropertyChanged(); } }
    public decimal Prevoz { get => _radnaStavka.Prevoz; set { _radnaStavka.Prevoz = value; OnPropertyChanged(); } }

    // ── Stimulacije ──────────────────────────────────────────────────
    public decimal Stim1proc { get => _radnaStavka.Stim1proc; set { _radnaStavka.Stim1proc = value; OnPropertyChanged(); } }
    public decimal Stim2proc { get => _radnaStavka.Stim2proc; set { _radnaStavka.Stim2proc = value; OnPropertyChanged(); } }
    public decimal Stim3proc { get => _radnaStavka.Stim3proc; set { _radnaStavka.Stim3proc = value; OnPropertyChanged(); } }

    public bool Sacuvan { get; private set; }

    public UnosCasovaViewModel(LdObracunStavka stavka, LdParametar param)
    {
        _izvornaStavka = stavka;
        _radnaStavka = CloneStavka(stavka);
        _naslovRadnika = $"{_radnaStavka.ImePrez}  (br. {_radnaStavka.Broj})";
        FondCasova = param.Czakon;
    }

    public UnosCasovaViewModel(LdObracunStavka stavka, LdParametar param, string naslovRadnika)
    {
        _izvornaStavka = stavka;
        _radnaStavka = CloneStavka(stavka);
        _naslovRadnika = string.IsNullOrWhiteSpace(naslovRadnika)
            ? $"{_radnaStavka.ImePrez}  (br. {_radnaStavka.Broj})"
            : naslovRadnika;
        FondCasova = param.Czakon;
    }

    [RelayCommand]
    private void Sacuvaj(System.Windows.Window? window)
    {
        KopirajStavku(_radnaStavka, _izvornaStavka);
        Sacuvan = true;
        if (window != null) window.DialogResult = true;
    }

    [RelayCommand]
    private void Otkazi(System.Windows.Window? window)
    {
        if (window != null) window.DialogResult = false;
    }

    [RelayCommand]
    private void PunFond()
    {
        Casuc = FondCasova;
    }

    private static LdObracunStavka CloneStavka(LdObracunStavka izvor)
    {
        var kopija = new LdObracunStavka();
        KopirajStavku(izvor, kopija);
        return kopija;
    }

    private static void KopirajStavku(LdObracunStavka izvor, LdObracunStavka cilj)
    {
        foreach (var prop in typeof(LdObracunStavka).GetProperties())
        {
            if (!prop.CanRead || !prop.CanWrite) continue;
            prop.SetValue(cilj, prop.GetValue(izvor));
        }
    }
}
