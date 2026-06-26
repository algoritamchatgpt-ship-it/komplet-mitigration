using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// LDPUTNAL2 - pregled putnih naloga za period (FoxPro: DO FORM LDPUTNAL2).
/// </summary>
public partial class LdPutnal2ViewModel : ObservableObject
{
    private readonly IReadOnlyList<LdPutniNalogStavka> _sveStavke;
    private readonly Dictionary<int, string> _radniciByBroj;

    [ObservableProperty] private DateTime _datumOd = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime _datumDo = DateTime.Today;
    [ObservableProperty] private ObservableCollection<LdPutnal2Red> _rezultati = [];
    [ObservableProperty] private string _poruka = "Unesite period i kliknite PREGLED.";

    public event Action? ZatvaranjeZahtevano;

    public LdPutnal2ViewModel(
        IReadOnlyList<LdPutniNalogStavka> stavke,
        Dictionary<int, string> radniciByBroj)
    {
        _sveStavke = stavke;
        _radniciByBroj = radniciByBroj;
    }

    [RelayCommand]
    private void Pregled()
    {
        var od = DatumOd.Date;
        var do_ = DatumDo.Date;

        var lista = _sveStavke
            .Where(s => s.Datdok.Date >= od && s.Datdok.Date <= do_)
            .Select(s => new LdPutnal2Red
            {
                Putnal = s.Putnal,
                Datdok = s.Datdok,
                ImeRadnika = _radniciByBroj.TryGetValue(s.Broj, out var ime) ? ime : $"({s.Broj})",
                Cilj = s.Cilj,
                Svega = s.Svega,
                Zaisplat = s.Zaisplat,
                Napomena = s.Napomena,
            })
            .OrderBy(r => r.Datdok)
            .ToList();

        Rezultati = new ObservableCollection<LdPutnal2Red>(lista);
        var ukupno = lista.Sum(r => r.Svega);
        Poruka = $"Period {od:dd.MM.yyyy} - {do_:dd.MM.yyyy}: {lista.Count} naloga, ukupno {ukupno:N2}";
    }

    [RelayCommand]
    private void Stampa()
    {
        if (Rezultati.Count == 0)
            Pregled();

        if (Rezultati.Count == 0)
        {
            Poruka = "Nema podataka za stampu za zadati period.";
            return;
        }

        var redovi = Rezultati.ToList();
        var naslov = $"PUTNI NALOZI - PERIOD {DatumOd:dd.MM.yyyy} - {DatumDo:dd.MM.yyyy}";
        var view = new Views.Zarade.LdBolGenericReportView(naslov, redovi, redovi.Count);
        view.ShowDialog();

        Poruka = $"Otvoren pregled za stampu ({redovi.Count} naloga).";
    }

    [RelayCommand]
    private void Izlaz() => ZatvaranjeZahtevano?.Invoke();
}

public class LdPutnal2Red
{
    public int Putnal { get; set; }
    public DateTime Datdok { get; set; }
    public string ImeRadnika { get; set; } = string.Empty;
    public string Cilj { get; set; } = string.Empty;
    public decimal Svega { get; set; }
    public decimal Zaisplat { get; set; }
    public string Napomena { get; set; } = string.Empty;
}
