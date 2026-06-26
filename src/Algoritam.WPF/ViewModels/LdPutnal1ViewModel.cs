using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// LDPUTNAL1 - detalj jednog putnog naloga (FoxPro: DO FORM LDPUTNAL1).
/// </summary>
public partial class LdPutnal1ViewModel : ObservableObject
{
    private readonly Dictionary<int, string> _radniciByBroj;

    [ObservableProperty] private int _putnal;
    [ObservableProperty] private DateTime _datdok = DateTime.Today;
    [ObservableProperty] private DateTime _datvrac = DateTime.Today;
    [ObservableProperty] private int _broj;
    [ObservableProperty] private string _imeRadnika = string.Empty;
    [ObservableProperty] private string _cilj = string.Empty;
    [ObservableProperty] private string _svrha1 = string.Empty;
    [ObservableProperty] private string _svrha2 = string.Empty;
    [ObservableProperty] private string _izvest1 = string.Empty;
    [ObservableProperty] private string _izvest2 = string.Empty;
    [ObservableProperty] private string _izvest3 = string.Empty;
    [ObservableProperty] private decimal _akontac;
    [ObservableProperty] private string _vozilo = string.Empty;
    [ObservableProperty] private decimal _racun1;
    [ObservableProperty] private string _brrac1 = string.Empty;
    [ObservableProperty] private decimal _racun2;
    [ObservableProperty] private string _brrac2 = string.Empty;
    [ObservableProperty] private decimal _racun3;
    [ObservableProperty] private string _brrac3 = string.Empty;
    [ObservableProperty] private decimal _racun4;
    [ObservableProperty] private string _brrac4 = string.Empty;
    [ObservableProperty] private decimal _racun5;
    [ObservableProperty] private string _brrac5 = string.Empty;
    [ObservableProperty] private decimal _svega;
    [ObservableProperty] private decimal _zaisplat;
    [ObservableProperty] private string _napomena = string.Empty;
    [ObservableProperty] private int _caspol;
    [ObservableProperty] private int _casdol;
    [ObservableProperty] private decimal _brdnev;
    [ObservableProperty] private decimal _dnevnica;
    [ObservableProperty] private decimal _iznosdn;
    [ObservableProperty] private decimal _trosak;
    [ObservableProperty] private string _poruka = string.Empty;

    public bool Sačuvano { get; private set; }
    public event Action? ZatvaranjeZahtevano;

    public LdPutnal1ViewModel(LdPutniNalogStavka stavka, Dictionary<int, string> radniciByBroj)
    {
        _radniciByBroj = radniciByBroj;
        UcitajIzStavke(stavka);
    }

    private void UcitajIzStavke(LdPutniNalogStavka s)
    {
        Putnal = s.Putnal;
        Datdok = s.Datdok;
        Datvrac = s.Datvrac;
        Broj = s.Broj;
        Cilj = s.Cilj;
        Svrha1 = s.Svrha1;
        Svrha2 = s.Svrha2;
        Izvest1 = s.Izvest1;
        Izvest2 = s.Izvest2;
        Izvest3 = s.Izvest3;
        Akontac = s.Akontac;
        Vozilo = s.Vozilo;
        Racun1 = s.Racun1;
        Brrac1 = s.Brrac1;
        Racun2 = s.Racun2;
        Brrac2 = s.Brrac2;
        Racun3 = s.Racun3;
        Brrac3 = s.Brrac3;
        Racun4 = s.Racun4;
        Brrac4 = s.Brrac4;
        Racun5 = s.Racun5;
        Brrac5 = s.Brrac5;
        Svega = s.Svega;
        Zaisplat = s.Zaisplat;
        Napomena = s.Napomena;
        Caspol = s.Caspol;
        Casdol = s.Casdol;
        Brdnev = s.Brdnev;
        Dnevnica = s.Dnevnica;
        Iznosdn = s.Iznosdn;
        Trosak = s.Trosak;

        ImeRadnika = _radniciByBroj.TryGetValue(Broj, out var ime) ? ime : string.Empty;
        Poruka = $"Putni nalog br. {Putnal}";
    }

    public void PrenesiUStavku(LdPutniNalogStavka s)
    {
        s.Putnal = Putnal;
        s.Datdok = Datdok;
        s.Datvrac = Datvrac;
        s.Broj = Broj;
        s.Cilj = Cilj;
        s.Svrha1 = Svrha1;
        s.Svrha2 = Svrha2;
        s.Izvest1 = Izvest1;
        s.Izvest2 = Izvest2;
        s.Izvest3 = Izvest3;
        s.Akontac = Akontac;
        s.Vozilo = Vozilo;
        s.Racun1 = Racun1;
        s.Brrac1 = Brrac1;
        s.Racun2 = Racun2;
        s.Brrac2 = Brrac2;
        s.Racun3 = Racun3;
        s.Brrac3 = Brrac3;
        s.Racun4 = Racun4;
        s.Brrac4 = Brrac4;
        s.Racun5 = Racun5;
        s.Brrac5 = Brrac5;
        s.Svega = Svega;
        s.Zaisplat = Zaisplat;
        s.Napomena = Napomena;
        s.Caspol = Caspol;
        s.Casdol = Casdol;
        s.Brdnev = Brdnev;
        s.Dnevnica = Dnevnica;
        s.Iznosdn = Iznosdn;
        s.Trosak = Trosak;
    }

    partial void OnBrojChanged(int value)
    {
        ImeRadnika = _radniciByBroj.TryGetValue(value, out var ime) ? ime : string.Empty;
    }

    [RelayCommand]
    private void Obracun()
    {
        Iznosdn = Brdnev > 0m && Dnevnica > 0m
            ? Math.Round(Brdnev * Dnevnica, 2)
            : Iznosdn;
        Svega = Math.Round(Racun1 + Racun2 + Racun3 + Racun4 + Racun5 + Iznosdn, 2);
        Trosak = Svega;
        Zaisplat = Math.Round(Svega - Akontac, 2);
        Poruka = $"Obracun: Svega {Svega:N2}, Za isplatu {Zaisplat:N2}";
    }

    [RelayCommand]
    private void Stampa()
    {
        var redovi = new List<Putnal1PrintRow>
        {
            new() { Polje = "Broj naloga", Vrednost = Putnal.ToString(CultureInfo.CurrentCulture) },
            new() { Polje = "Datum polaska", Vrednost = Datdok.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture) },
            new() { Polje = "Datum povratka", Vrednost = Datvrac.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture) },
            new() { Polje = "Broj radnika", Vrednost = Broj.ToString(CultureInfo.CurrentCulture) },
            new() { Polje = "Ime radnika", Vrednost = ImeRadnika },
            new() { Polje = "Cilj putovanja", Vrednost = Cilj },
            new() { Polje = "Svrha 1", Vrednost = Svrha1 },
            new() { Polje = "Svrha 2", Vrednost = Svrha2 },
            new() { Polje = "Vozilo", Vrednost = Vozilo },
            new() { Polje = "Akontacija", Vrednost = Akontac.ToString("N2", CultureInfo.CurrentCulture) },
            new() { Polje = "Racun 1", Vrednost = Racun1.ToString("N2", CultureInfo.CurrentCulture) },
            new() { Polje = "Br. racuna 1", Vrednost = Brrac1 },
            new() { Polje = "Racun 2", Vrednost = Racun2.ToString("N2", CultureInfo.CurrentCulture) },
            new() { Polje = "Br. racuna 2", Vrednost = Brrac2 },
            new() { Polje = "Racun 3", Vrednost = Racun3.ToString("N2", CultureInfo.CurrentCulture) },
            new() { Polje = "Br. racuna 3", Vrednost = Brrac3 },
            new() { Polje = "Racun 4", Vrednost = Racun4.ToString("N2", CultureInfo.CurrentCulture) },
            new() { Polje = "Br. racuna 4", Vrednost = Brrac4 },
            new() { Polje = "Racun 5", Vrednost = Racun5.ToString("N2", CultureInfo.CurrentCulture) },
            new() { Polje = "Br. racuna 5", Vrednost = Brrac5 },
            new() { Polje = "Cas polaska", Vrednost = Caspol.ToString(CultureInfo.CurrentCulture) },
            new() { Polje = "Cas dolaska", Vrednost = Casdol.ToString(CultureInfo.CurrentCulture) },
            new() { Polje = "Broj dnevnica", Vrednost = Brdnev.ToString("N2", CultureInfo.CurrentCulture) },
            new() { Polje = "Dnevnica", Vrednost = Dnevnica.ToString("N2", CultureInfo.CurrentCulture) },
            new() { Polje = "Iznos dnevnica", Vrednost = Iznosdn.ToString("N2", CultureInfo.CurrentCulture) },
            new() { Polje = "Svega", Vrednost = Svega.ToString("N2", CultureInfo.CurrentCulture) },
            new() { Polje = "Za isplatu", Vrednost = Zaisplat.ToString("N2", CultureInfo.CurrentCulture) },
            new() { Polje = "Trosak", Vrednost = Trosak.ToString("N2", CultureInfo.CurrentCulture) },
            new() { Polje = "Napomena", Vrednost = Napomena },
            new() { Polje = "Izvestaj 1", Vrednost = Izvest1 },
            new() { Polje = "Izvestaj 2", Vrednost = Izvest2 },
            new() { Polje = "Izvestaj 3", Vrednost = Izvest3 },
        };

        var view = new Views.Zarade.LdBolGenericReportView(
            $"PUTNI NALOG BR. {Putnal}",
            redovi,
            redovi.Count);

        view.ShowDialog();
        Poruka = "Otvoren pregled za stampu putnog naloga.";
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        Sačuvano = true;
        ZatvaranjeZahtevano?.Invoke();
    }

    [RelayCommand]
    private void Izlaz() => ZatvaranjeZahtevano?.Invoke();
}

public class Putnal1PrintRow
{
    public string Polje { get; set; } = string.Empty;
    public string Vrednost { get; set; } = string.Empty;
}
