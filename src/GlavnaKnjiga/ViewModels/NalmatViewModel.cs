using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalmatViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly int _godina;
    private readonly string _nalmatPath;
    private List<NalmatRow> _sviRedovi = [];

    public event Action? ZatvoriFormu;

    [ObservableProperty] private ObservableCollection<NalmatRow> _redovi = [];
    [ObservableProperty] private NalmatRow? _selectedRow;
    [ObservableProperty] private string _lblKonto = string.Empty;
    [ObservableProperty] private string _lblRec = string.Empty;
    [ObservableProperty] private string _status = string.Empty;

    public NalmatViewModel(string firmPath, int godina)
    {
        _firmPath = firmPath;
        _godina = godina;
        _nalmatPath = Path.Combine(firmPath, "nalmat.dbf");
        Ucitaj();
    }

    partial void OnSelectedRowChanged(NalmatRow? value)
    {
        if (value == null)
        {
            LblKonto = string.Empty;
            LblRec = string.Empty;
            return;
        }

        LblKonto = $"{value.Konto.Trim()}  {value.NazivKonta}".Trim();
        LblRec = $"{Redovi.IndexOf(value) + 1,6}/{Redovi.Count,6}";
    }

    [RelayCommand]
    private void PrijemKontaMaterijala()
    {
        var nalPath = Path.Combine(_firmPath, "nal.dbf");
        if (!File.Exists(nalPath))
        {
            MessageBox.Show("nal.dbf ne postoji.", "PRIJEM KONTA MATERIJALA",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show(
                "Postojeća tabela nalmat.dbf biće zamenjena podacima iz nal.dbf. Nastaviti?",
                "PRIJEM KONTA MATERIJALA", MessageBoxButton.YesNo, MessageBoxImage.Warning) !=
            MessageBoxResult.Yes)
            return;

        try
        {
            var nalRows = new SimpleDbfReader(nalPath).Zapisi()
                .Select(Nalp2ViewModel.NalpRowFromRecord)
                .ToList();
            _sviRedovi = FormirajPrijem(nalRows);
            UcitajNaziveKonta(_sviRedovi);
            Prikazi(_sviRedovi, "Primljena konta materijala");
            SnimiNalmat();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri prijemu materijala:\n{ex.Message}",
                "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void SrediProsecneCene()
    {
        if (_sviRedovi.Count == 0) return;

        SrediCene(_sviRedovi);
        SnimiNalmat();
        Prikazi(_sviRedovi, "Prosečne cene su sređene");
    }

    [RelayCommand]
    private void KarticaMaterijala()
    {
        if (SelectedRow == null) return;
        var konto = SelectedRow.Konto.Trim();
        var kartica = FormirajKarticu(_sviRedovi, konto);
        Prikazi(kartica, $"Kartica materijala: {konto}");
    }

    [RelayCommand]
    private void SaldoMaterijala()
    {
        var saldo = FormirajSaldo(_sviRedovi, false);
        Prikazi(saldo, "Saldo materijala");
    }

    [RelayCommand]
    private void LagerLista()
    {
        var lager = FormirajSaldo(_sviRedovi, true);
        Prikazi(lager, "Lager lista materijala");
    }

    [RelayCommand]
    private void PrikaziOdstupanja()
    {
        var odstupanja = _sviRedovi.Where(r => r.ImaOdstupanje).ToList();
        Prikazi(odstupanja, $"Odstupanja: {odstupanja.Count}");
    }

    [RelayCommand]
    private void PrikaziSve() => Prikazi(_sviRedovi, "Svi redovi");

    [RelayCommand]
    private void NapraviNalogOdstupanja()
    {
        var novi = FormirajNalogOdstupanja(_sviRedovi, new DateTime(_godina, 12, 31));
        if (novi.Count == 0)
        {
            MessageBox.Show("Nema odstupanja za knjiženje.", "NALOG ODSTUPANJA",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var nalpPath = Path.Combine(_firmPath, "nalp.dbf");
        if (!File.Exists(nalpPath))
        {
            MessageBox.Show("nalp.dbf ne postoji.", "NALOG ODSTUPANJA",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show(
                $"U nalp.dbf biće dodat nalog 888888 sa {novi.Count} stavki. Nastaviti?",
                "NALOG ODSTUPANJA", MessageBoxButton.YesNo, MessageBoxImage.Warning) !=
            MessageBoxResult.Yes)
            return;

        try
        {
            var schema = DbfTableWriter.LoadSchema(nalpPath);
            var postojeci = new SimpleDbfReader(nalpPath).Zapisi()
                .Select(Nalp2ViewModel.NalpRowFromRecord)
                .ToList();
            postojeci.AddRange(novi);
            DbfTableWriter.WriteTable(
                nalpPath, schema, postojeci, Nalp2ViewModel.NalpRowFieldMapper);
            MessageBox.Show($"Dodat je nalog 888888 sa {novi.Count} stavki.",
                "NALOG ODSTUPANJA", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri formiranju naloga:\n{ex.Message}",
                "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Brisanje()
    {
        if (MessageBox.Show("Obrisati celu tabelu nalmat.dbf?", "BRISANJE",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        _sviRedovi.Clear();
        Prikazi(_sviRedovi, "Tabela je prazna");
        SnimiNalmat();
    }

    [RelayCommand]
    private void Izlaz()
    {
        SnimiNalmat();
        ZatvoriFormu?.Invoke();
    }

    public void SnimiIzmene()
    {
        if (Status.StartsWith("Svi redovi", StringComparison.Ordinal) ||
            Status.StartsWith("Primljena", StringComparison.Ordinal) ||
            Status.StartsWith("Prosečne", StringComparison.Ordinal))
        {
            _sviRedovi = Redovi.ToList();
            SnimiNalmat();
        }
    }

    internal static List<NalmatRow> FormirajPrijem(IEnumerable<NalpRow> nalRows)
    {
        var rows = nalRows.ToList();
        var materijalnaKonta = rows
            .Where(r => r.Ulaz != 0 || r.Izlaz != 0)
            .Select(r => r.Konto.Trim())
            .Where(k => !string.IsNullOrEmpty(k))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return rows
            .Where(r => materijalnaKonta.Contains(r.Konto.Trim()))
            .OrderBy(r => r.Konto.Trim(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Datdok)
            .ThenBy(r => r.Idbr)
            .Select(r => new NalmatRow
            {
                Konto = r.Konto,
                Cena = r.Cena,
                Ulaz = r.Ulaz,
                Izlaz = r.Izlaz,
                UkupnoD = r.UkupnoD,
                UkupnoP = r.UkupnoP,
                Dug = r.Dug,
                Pot = r.Pot,
                Stanje = r.Stanje,
                Saldo = r.Saldo,
                Datdok = r.Datdok,
                Brnal = r.Brnal,
                Opis = r.Opis,
                Preneto = r.Preneto,
                Idbr = r.Idbr,
            })
            .ToList();
    }

    internal static void SrediCene(IList<NalmatRow> rows)
    {
        foreach (var row in rows)
        {
            row.UkupnoD = Math.Round(row.Ulaz * row.Cena, 2);
            row.UkupnoP = Math.Round(row.Izlaz * row.Cena, 2);
        }

        foreach (var grupa in rows
                     .GroupBy(r => r.Konto.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            decimal ukupniUlaz = 0;
            decimal ukupniIzlaz = 0;
            decimal ukupnoDuguje = 0;
            decimal ukupnoPotrazuje = 0;

            foreach (var row in grupa.OrderBy(r => r.Datdok).ThenBy(r => r.Idbr))
            {
                ukupniUlaz += row.Ulaz;
                ukupnoDuguje += row.UkupnoD;

                if (row.Izlaz != 0)
                {
                    var stanjePreIzlaza = ukupniUlaz - ukupniIzlaz;
                    var saldoPreIzlaza = ukupnoDuguje - ukupnoPotrazuje;
                    if (stanjePreIzlaza != 0)
                    {
                        row.Cena = Math.Round(
                            saldoPreIzlaza / stanjePreIzlaza, 3,
                            MidpointRounding.AwayFromZero);
                        row.UkupnoP = Math.Round(
                            row.Izlaz * row.Cena, 2,
                            MidpointRounding.AwayFromZero);
                    }
                }

                ukupniIzlaz += row.Izlaz;
                ukupnoPotrazuje += row.UkupnoP;
            }
        }

        foreach (var row in rows)
        {
            if (row.Dug != 0) row.Saldo = row.Dug - row.UkupnoD;
            if (row.Pot != 0) row.Saldo = row.Pot - row.UkupnoP;
        }
    }

    internal static List<NalmatRow> FormirajKarticu(
        IEnumerable<NalmatRow> rows, string konto)
    {
        decimal stanje = 0;
        decimal saldo = 0;
        var rezultat = new List<NalmatRow>();

        foreach (var izvor in rows
                     .Where(r => r.Konto.Trim().Equals(
                         konto.Trim(), StringComparison.OrdinalIgnoreCase))
                     .OrderBy(r => r.Datdok)
                     .ThenBy(r => r.Idbr))
        {
            var row = izvor.Clone();
            stanje += row.Ulaz - row.Izlaz;
            saldo += row.UkupnoD - row.UkupnoP;
            row.Stanje = stanje;
            row.Saldo = saldo;
            rezultat.Add(row);
        }

        return rezultat;
    }

    internal static List<NalmatRow> FormirajSaldo(
        IEnumerable<NalmatRow> rows, bool izracunajCenu)
    {
        return rows
            .Where(r => r.Ulaz != 0 || r.Izlaz != 0)
            .GroupBy(r => r.Konto.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Select(g =>
            {
                var stanje = g.Sum(r => r.Ulaz) - g.Sum(r => r.Izlaz);
                var saldo = g.Sum(r => r.UkupnoD) - g.Sum(r => r.UkupnoP);
                return new NalmatRow
                {
                    Konto = g.Key,
                    NazivKonta = g.First().NazivKonta,
                    Ulaz = g.Sum(r => r.Ulaz),
                    Izlaz = g.Sum(r => r.Izlaz),
                    UkupnoD = g.Sum(r => r.UkupnoD),
                    UkupnoP = g.Sum(r => r.UkupnoP),
                    Stanje = stanje,
                    Saldo = saldo,
                    Cena = izracunajCenu && stanje != 0
                        ? Math.Round(saldo / stanje, 3, MidpointRounding.AwayFromZero)
                        : 0,
                };
            })
            .OrderBy(r => r.Konto, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static List<NalpRow> FormirajNalogOdstupanja(
        IEnumerable<NalmatRow> rows, DateTime datum)
    {
        var rezultat = new List<NalpRow>();
        var odstupanja = rows
            .Where(r => r.ImaOdstupanje)
            .GroupBy(r => r.Konto.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Select(g => new
            {
                Konto = g.Key,
                Dug = g.Sum(r => r.Dug),
                Pot = g.Sum(r => r.Pot),
                UkupnoD = g.Sum(r => r.UkupnoD),
                UkupnoP = g.Sum(r => r.UkupnoP),
            });

        foreach (var stavka in odstupanja)
        {
            var odstupanjeDug = stavka.Dug - stavka.UkupnoD;
            var odstupanjePot = stavka.Pot - stavka.UkupnoP;

            if (odstupanjeDug != 0)
                rezultat.Add(NoviNalogRed(
                    stavka.Konto, -odstupanjeDug, 0, datum));
            if (odstupanjePot != 0)
                rezultat.Add(NoviNalogRed(
                    stavka.Konto, 0, -odstupanjePot, datum));
        }

        return rezultat;
    }

    private static NalpRow NoviNalogRed(
        string konto, decimal dug, decimal pot, DateTime datum) => new()
    {
        Konto = konto,
        Dug = dug,
        Pot = pot,
        Datdok = datum,
        Brnal = "888888",
        Opis = "SLAGANJE MATERIJALA",
        Datum = DateTime.Today,
        Vreme = DateTime.Now.ToString("HH:mm:ss"),
    };

    private void Ucitaj()
    {
        _sviRedovi = [];
        if (File.Exists(_nalmatPath))
        {
            try
            {
                _sviRedovi = new SimpleDbfReader(_nalmatPath).Zapisi()
                    .Select(CitajRec)
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri čitanju nalmat.dbf:\n{ex.Message}");
            }
        }

        UcitajNaziveKonta(_sviRedovi);
        Prikazi(_sviRedovi, "Svi redovi");
    }

    private void UcitajNaziveKonta(IEnumerable<NalmatRow> rows)
    {
        var kontoPath = Path.Combine(_firmPath, "konto.dbf");
        if (!File.Exists(kontoPath)) return;

        try
        {
            var nazivi = new SimpleDbfReader(kontoPath).Zapisi()
                .Select(r => new
                {
                    Konto = r.DajString("KONTO").Trim(),
                    Naziv = r.DajString("NAZIV").Trim(),
                })
                .Where(r => !string.IsNullOrEmpty(r.Konto))
                .GroupBy(r => r.Konto, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Naziv,
                    StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
                if (nazivi.TryGetValue(row.Konto.Trim(), out var naziv))
                    row.NazivKonta = naziv;
        }
        catch { }
    }

    private void Prikazi(IEnumerable<NalmatRow> rows, string status)
    {
        Redovi = new ObservableCollection<NalmatRow>(rows);
        SelectedRow = Redovi.FirstOrDefault();
        Status = $"{status}   |   Redova: {Redovi.Count}";
    }

    private void SnimiNalmat()
    {
        if (!File.Exists(_nalmatPath)) return;
        try
        {
            var schema = DbfTableWriter.LoadSchema(_nalmatPath);
            DbfTableWriter.WriteTable(
                _nalmatPath, schema, _sviRedovi, NalmatFieldMapper);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri snimanju nalmat.dbf:\n{ex.Message}");
        }
    }

    private static NalmatRow CitajRec(DbfRecord rec) => new()
    {
        Konto = rec.DajString("KONTO"),
        Cena = rec.DajDecimal("CENA"),
        Ulaz = rec.DajDecimal("ULAZ"),
        Izlaz = rec.DajDecimal("IZLAZ"),
        UkupnoD = rec.DajDecimal("UKUPNO_D"),
        UkupnoP = rec.DajDecimal("UKUPNO_P"),
        Dug = rec.DajDecimal("DUG"),
        Pot = rec.DajDecimal("POT"),
        Stanje = rec.DajDecimal("STANJE"),
        Saldo = rec.DajDecimal("SALDO"),
        Datdok = rec.DajDate("DATDOK"),
        Brnal = rec.DajString("BRNAL"),
        Opis = rec.DajString("OPIS"),
        Preneto = rec.DajString("PRENETO"),
        Idbr = rec.DajDecimal("IDBR"),
    };

    private static object? NalmatFieldMapper(NalmatRow r, string f) =>
        f.ToUpperInvariant() switch
        {
            "KONTO" => r.Konto,
            "CENA" => r.Cena,
            "ULAZ" => r.Ulaz,
            "IZLAZ" => r.Izlaz,
            "UKUPNO_D" => r.UkupnoD,
            "UKUPNO_P" => r.UkupnoP,
            "DUG" => r.Dug,
            "POT" => r.Pot,
            "STANJE" => r.Stanje,
            "SALDO" => r.Saldo,
            "DATDOK" => r.Datdok,
            "BRNAL" => r.Brnal,
            "OPIS" => r.Opis,
            "PRENETO" => r.Preneto,
            "IDBR" => r.Idbr,
            _ => null,
        };
}
