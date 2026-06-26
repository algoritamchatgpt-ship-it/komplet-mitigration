using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>
/// NALPREGKONTO — parametri i formiranje kartice iz dnevnika glavne knjige.
/// </summary>
public partial class NalPregKontoViewModel : ObservableObject
{
    private readonly List<NalpRow> _sviRedovi = [];

    public event Action? ZatvoriFormu;

    [ObservableProperty] private string _konto = string.Empty;
    [ObservableProperty] private DateTime? _datumOd;
    [ObservableProperty] private DateTime? _datumDo;
    [ObservableProperty] private string _dok = string.Empty;
    [ObservableProperty] private string _mp = string.Empty;
    [ObservableProperty] private string _mtr = "0";
    [ObservableProperty] private string _opis = string.Empty;
    [ObservableProperty] private string _saldoPoDanima = "N";
    [ObservableProperty] private string _padajuciSaldo = "N";
    [ObservableProperty] private string _saldoPoMesecima = "N";
    [ObservableProperty] private string _dugPotSve = "S";
    [ObservableProperty] private string _sortiranoPoVrednosti = "N";
    [ObservableProperty] private string _izbaciSlicne = "N";
    [ObservableProperty] private decimal _tolerancija = 5m;

    public NalPregKontoViewModel(string firmPath, int godina, string? pocetniKonto = null)
    {
        Konto = pocetniKonto?.Trim() ?? string.Empty;
        var aktivnaGodina = godina > 0 ? godina : DateTime.Today.Year;
        DatumOd = new DateTime(aktivnaGodina, 1, 1);
        DatumDo = new DateTime(aktivnaGodina, 12, 31);

        var path = Path.Combine(firmPath, "nal.dbf");
        if (!File.Exists(path))
            return;

        try
        {
            _sviRedovi.AddRange(
                new SimpleDbfReader(path).Zapisi().Select(Nalp2ViewModel.NalpRowFromRecord));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Greška pri čitanju nal.dbf: {ex.Message}",
                "PREGLED KONTA",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Pregled()
    {
        var redovi = FormirajPregled(
            _sviRedovi,
            Konto,
            DatumOd,
            DatumDo,
            Dok,
            Mp,
            Mtr,
            Opis,
            SaldoPoDanima,
            PadajuciSaldo,
            SaldoPoMesecima,
            DugPotSve,
            SortiranoPoVrednosti,
            IzbaciSlicne,
            Tolerancija);

        var naslov = string.IsNullOrWhiteSpace(Konto)
            ? "PREGLED KONTA"
            : $"PREGLED KONTA {Konto.Trim()}";
        var vm = new NalogPregledViewModel(
            naslov,
            redovi.Select(NalogPregledViewModel.IzNalp));
        new Views.NalogPregledWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    internal static IReadOnlyList<NalpRow> FormirajPregled(
        IEnumerable<NalpRow> redovi,
        string? konto,
        DateTime? datumOd,
        DateTime? datumDo,
        string? dok,
        string? mp,
        string? mtr,
        string? opis,
        string? saldoPoDanima,
        string? padajuciSaldo,
        string? saldoPoMesecima,
        string? dugPotSve,
        string? sortiranoPoVrednosti,
        string? izbaciSlicne,
        decimal tolerancija)
    {
        var od = datumOd?.Date ?? DateTime.MinValue.Date;
        var doDatuma = datumDo?.Date ?? DateTime.MaxValue.Date;
        if (od > doDatuma)
            (od, doDatuma) = (doDatuma, od);

        var kontoFilter = konto?.Trim() ?? string.Empty;
        var dokFilter = dok?.Trim() ?? string.Empty;
        var opisFilter = opis?.Trim() ?? string.Empty;
        var mpFilter = ParsirajOpcioniBroj(mp, nulaJePrazno: false);
        var mtrFilter = ParsirajOpcioniBroj(mtr, nulaJePrazno: true);

        bool ZajednickiFilter(NalpRow red)
        {
            var redKonto = red.Konto.Trim();
            var kontoOdgovara = string.IsNullOrEmpty(kontoFilter) ||
                (kontoFilter.Length >= 10
                    ? redKonto.Equals(kontoFilter, StringComparison.OrdinalIgnoreCase)
                    : redKonto.StartsWith(kontoFilter, StringComparison.OrdinalIgnoreCase));

            return kontoOdgovara &&
                   (string.IsNullOrEmpty(dokFilter) ||
                    red.Dok.Trim().Equals(dokFilter, StringComparison.OrdinalIgnoreCase)) &&
                   (!mpFilter.HasValue || red.Mp == mpFilter.Value) &&
                   (!mtrFilter.HasValue || red.Mtr == mtrFilter.Value) &&
                   (string.IsNullOrEmpty(opisFilter) ||
                    red.Opis.Trim().StartsWith(opisFilter, StringComparison.OrdinalIgnoreCase));
        }

        var filtrirani = redovi.Where(ZajednickiFilter).ToList();
        var prePerioda = filtrirani
            .Where(r => r.Datdok.HasValue && r.Datdok.Value.Date < od)
            .ToList();
        var period = filtrirani
            .Where(r => r.Datdok.HasValue &&
                        r.Datdok.Value.Date >= od &&
                        r.Datdok.Value.Date <= doDatuma)
            .Select(r => r.Clone())
            .ToList();

        period = PrimeniSmer(period, dugPotSve);
        if (Da(izbaciSlicne))
            period = UkloniSlicneStavke(period, Math.Max(0m, tolerancija));

        var pocetnoDuguje = prePerioda.Sum(r => r.Dug);
        var pocetnoPotrazuje = prePerioda.Sum(r => r.Pot);
        var rezultat = new List<NalpRow>();

        if (pocetnoDuguje != 0 || pocetnoPotrazuje != 0)
        {
            rezultat.Add(new NalpRow
            {
                Konto = kontoFilter,
                Dug = pocetnoDuguje,
                Pot = pocetnoPotrazuje,
                Opis = "POČETNO STANJE",
                Dpsaldo = pocetnoDuguje - pocetnoPotrazuje,
            });
        }

        if (Da(saldoPoMesecima))
        {
            rezultat.AddRange(GrupisiPoMesecima(period));
            return rezultat;
        }

        if (Da(saldoPoDanima))
        {
            rezultat.AddRange(GrupisiPoDanima(
                period,
                Da(padajuciSaldo),
                pocetnoDuguje - pocetnoPotrazuje));
            return rezultat;
        }

        period = Da(sortiranoPoVrednosti)
            ? period
                .OrderByDescending(r => Math.Max(Math.Abs(r.Dug), Math.Abs(r.Pot)))
                .ThenBy(r => r.Datdok)
                .ToList()
            : period
                .OrderBy(r => r.Datdok)
                .ThenBy(r => r.Brnal.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToList();

        var saldo = pocetnoDuguje - pocetnoPotrazuje;
        foreach (var red in period)
        {
            saldo += red.Dug - red.Pot;
            red.Dpsaldo = saldo;
            rezultat.Add(red);
        }

        return rezultat;
    }

    private static List<NalpRow> PrimeniSmer(IEnumerable<NalpRow> redovi, string? smer)
    {
        var izbor = smer?.Trim().ToUpperInvariant();
        return izbor switch
        {
            "D" => redovi.Where(r => r.Dug != 0 || r.Devdug != 0).ToList(),
            "P" => redovi.Where(r => r.Pot != 0 || r.Devpot != 0).ToList(),
            _ => redovi.ToList(),
        };
    }

    private static List<NalpRow> UkloniSlicneStavke(
        IReadOnlyList<NalpRow> redovi,
        decimal tolerancija)
    {
        var uklonjeni = new bool[redovi.Count];
        for (var i = 0; i < redovi.Count; i++)
        {
            if (uklonjeni[i])
                continue;

            var dug = Math.Abs(redovi[i].Dug);
            if (dug == 0)
                continue;

            for (var j = 0; j < redovi.Count; j++)
            {
                if (i == j || uklonjeni[j])
                    continue;

                var pot = Math.Abs(redovi[j].Pot);
                if (pot == 0 || Math.Abs(dug - pot) > tolerancija)
                    continue;

                uklonjeni[i] = true;
                uklonjeni[j] = true;
                break;
            }
        }

        return redovi
            .Where((_, index) => !uklonjeni[index])
            .ToList();
    }

    private static IEnumerable<NalpRow> GrupisiPoDanima(
        IEnumerable<NalpRow> redovi,
        bool kumulativno,
        decimal pocetniSaldo)
    {
        var saldo = pocetniSaldo;
        foreach (var grupa in redovi
                     .GroupBy(r => r.Datdok!.Value.Date)
                     .OrderBy(g => g.Key))
        {
            var dug = grupa.Sum(r => r.Dug);
            var pot = grupa.Sum(r => r.Pot);
            saldo = kumulativno ? saldo + dug - pot : dug - pot;
            yield return new NalpRow
            {
                Konto = grupa.First().Konto,
                Datdok = grupa.Key,
                Dug = dug,
                Pot = pot,
                Opis = "SALDO PO DANU",
                Dpsaldo = saldo,
            };
        }
    }

    private static IEnumerable<NalpRow> GrupisiPoMesecima(IEnumerable<NalpRow> redovi)
    {
        foreach (var grupa in redovi
                     .GroupBy(r => new { r.Datdok!.Value.Year, r.Datdok.Value.Month })
                     .OrderBy(g => g.Key.Year)
                     .ThenBy(g => g.Key.Month))
        {
            var dug = grupa.Sum(r => r.Dug);
            var pot = grupa.Sum(r => r.Pot);
            yield return new NalpRow
            {
                Konto = grupa.First().Konto,
                Datdok = new DateTime(grupa.Key.Year, grupa.Key.Month, 1),
                Dug = dug,
                Pot = pot,
                Opis = "SALDO PO MESECU",
                Dpsaldo = dug - pot,
            };
        }
    }

    private static decimal? ParsirajOpcioniBroj(string? vrednost, bool nulaJePrazno)
    {
        var tekst = vrednost?.Trim();
        if (string.IsNullOrEmpty(tekst) || !decimal.TryParse(tekst, out var broj))
            return null;
        return nulaJePrazno && broj == 0 ? null : broj;
    }

    private static bool Da(string? vrednost) =>
        string.Equals(vrednost?.Trim(), "D", StringComparison.OrdinalIgnoreCase);
}
