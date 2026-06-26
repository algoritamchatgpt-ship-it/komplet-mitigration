using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// PUTAR1 - pregled putarina za period (FoxPro: DO FORM PUTAR1).
/// </summary>
public partial class PutarinePregledViewModel : ObservableObject
{
    private readonly IReadOnlyList<PutarinaStavka> _sveStavke;

    [ObservableProperty] private DateTime _datumOd = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime _datumDo = DateTime.Today;
    [ObservableProperty] private string _grupaFilter = string.Empty;
    [ObservableProperty] private ObservableCollection<PutarinaPregledRed> _rezultati = [];
    [ObservableProperty] private string _poruka = "Unesite period i kliknite PREGLED.";

    public event Action? ZatvaranjeZahtevano;

    public PutarinePregledViewModel(IReadOnlyList<PutarinaStavka> stavke)
    {
        _sveStavke = stavke;
    }

    [RelayCommand]
    private void Pregled()
    {
        var od = DatumOd.Date;
        var do_ = DatumDo.Date;
        var grupa = GrupaFilter.Trim();

        var lista = _sveStavke
            .Where(s => s.Datdok.Date >= od && s.Datdok.Date <= do_)
            .Where(s => string.IsNullOrEmpty(grupa) || s.Grupa.Equals(grupa, StringComparison.OrdinalIgnoreCase))
            .Select(s => new PutarinaPregledRed
            {
                Redbr = s.Redbr,
                Putnal = s.Putnal,
                Datdok = s.Datdok,
                Relacija = s.Relacija,
                Iznos = s.Iznos,
                Grupa = s.Grupa,
            })
            .OrderBy(r => r.Datdok)
            .ToList();

        Rezultati = new ObservableCollection<PutarinaPregledRed>(lista);
        var ukupno = lista.Sum(r => r.Iznos);
        Poruka = $"Period {od:dd.MM.yyyy} - {do_:dd.MM.yyyy}: {lista.Count} zapisa, ukupno {ukupno:N2}";
    }

    [RelayCommand]
    private void SaldoRelacija()
    {
        var od = DatumOd.Date;
        var do_ = DatumDo.Date;
        var grupa = GrupaFilter.Trim();

        var saldo = _sveStavke
            .Where(s => s.Datdok.Date >= od && s.Datdok.Date <= do_)
            .Where(s => string.IsNullOrEmpty(grupa) || s.Grupa.Equals(grupa, StringComparison.OrdinalIgnoreCase))
            .GroupBy(s => s.Relacija)
            .Select(g => new PutarinaPregledRed
            {
                Relacija = g.Key,
                Iznos = g.Sum(x => x.Iznos),
            })
            .OrderBy(r => r.Relacija)
            .ToList();

        Rezultati = new ObservableCollection<PutarinaPregledRed>(saldo);
        var ukupno = saldo.Sum(r => r.Iznos);
        Poruka = $"Saldo relacija {od:dd.MM.yyyy} - {do_:dd.MM.yyyy}: {saldo.Count} relacija, ukupno {ukupno:N2}";
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
        var naslov = $"PUTARINE - PERIOD {DatumOd:dd.MM.yyyy} - {DatumDo:dd.MM.yyyy}";
        if (!string.IsNullOrWhiteSpace(GrupaFilter))
            naslov += $" (GRUPA {GrupaFilter.Trim()})";

        var view = new Views.Zarade.LdBolGenericReportView(naslov, redovi, redovi.Count);
        view.ShowDialog();

        Poruka = $"Otvoren pregled za stampu ({redovi.Count} zapisa).";
    }

    [RelayCommand]
    private void Izlaz() => ZatvaranjeZahtevano?.Invoke();
}

public class PutarinaPregledRed
{
    public string Redbr { get; set; } = string.Empty;
    public string Putnal { get; set; } = string.Empty;
    public DateTime Datdok { get; set; }
    public string Relacija { get; set; } = string.Empty;
    public decimal Iznos { get; set; }
    public string Grupa { get; set; } = string.Empty;
}
