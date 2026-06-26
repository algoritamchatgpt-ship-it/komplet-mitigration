using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>
/// Transcripcija NALPN.SCX — "NEDOVRŠENI NALOZI".
/// Pregled svih redova iz nalpn.dbf sa operativnim dugmadima:
/// PRAZNI NALOG, BRISANJE NALOGA, PRAZNI SVE, PRENOS U IZRADU NALOGA, SORT.
/// </summary>
public partial class NalpNViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext<NalpNViewModel>();

    private readonly string _firmPath;
    private readonly string _nalpNPath;
    private readonly string _nalpPath;
    private Dictionary<string, string> _kontoNaz = [];

    [ObservableProperty] private ObservableCollection<NalpRow> _redovi = [];
    [ObservableProperty] private NalpRow? _selectedRow;
    [ObservableProperty] private string   _lblRec    = string.Empty;
    // header strip (iz trenutnog reda)
    [ObservableProperty] private string   _brNal     = string.Empty;
    [ObservableProperty] private DateTime? _datDok;
    // info panel (konto iz trenutnog reda)
    [ObservableProperty] private string   _kontoInfo = string.Empty;
    [ObservableProperty] private string   _nazIvInfo = string.Empty;

    private enum SortMode { Original, ByBrnal, ByKonto, ByDatdok }
    private SortMode _sortMode = SortMode.Original;

    public event Action? ZatvoriFormu;

    public NalpNViewModel(string firmPath)
    {
        _firmPath  = firmPath;
        _nalpNPath = Path.Combine(firmPath, "nalpn.dbf");
        _nalpPath  = Path.Combine(firmPath, "nalp.dbf");
        UcitajKonto();
        UcitajNalpN();
    }

    private void UcitajKonto()
    {
        var path = Path.Combine(_firmPath, "konto.dbf");
        if (!File.Exists(path)) return;
        try
        {
            var r = new SimpleDbfReader(path);
            _kontoNaz = r.Zapisi()
                         .ToDictionary(
                             rec => rec.DajString("KONTO").Trim(),
                             rec => rec.DajString("NAZIV"),
                             StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) { _log.Warning(ex, "konto.dbf nije učitan"); }
    }

    public void UcitajNalpN()
    {
        if (!File.Exists(_nalpNPath)) { Redovi = []; return; }
        try
        {
            var r = new SimpleDbfReader(_nalpNPath);
            var lista = r.Zapisi().Select(NalpRowFromRecord).ToList();
            Redovi = new ObservableCollection<NalpRow>(lista);
            if (Redovi.Count > 0) SelectedRow = Redovi[0];
            AzuriraLblRec();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "nalpn.dbf — greška pri učitavanju");
            Redovi = [];
        }
    }

    partial void OnSelectedRowChanged(NalpRow? value)
    {
        if (value == null) return;
        BrNal    = value.Brnal.Trim();
        DatDok   = value.Datdok;
        var k    = value.Konto.Trim();
        KontoInfo = k;
        NazIvInfo = _kontoNaz.TryGetValue(k, out var n) ? n : string.Empty;
        AzuriraLblRec();
    }

    private void AzuriraLblRec()
    {
        var idx = SelectedRow != null ? Redovi.IndexOf(SelectedRow) + 1 : 0;
        LblRec = $"{idx,6}/{Redovi.Count,6}";
    }

    // ── PRAZNI NALOG S+F7 — briše redove DUG=POT=0 za tekući BRNAL ──
    [RelayCommand]
    private void PrazniNalog()
    {
        if (SelectedRow == null) return;
        var brnal = SelectedRow.Brnal.Trim();
        if (MessageBox.Show(
            $"Brisanje praznina za nalog {brnal}?",
            "Potvrda", MessageBoxButton.YesNo, MessageBoxImage.Question)
            != MessageBoxResult.Yes) return;

        var praznine = Redovi
            .Where(r => r.Brnal.Trim() == brnal && r.Dug == 0 && r.Pot == 0)
            .ToList();
        foreach (var r in praznine) Redovi.Remove(r);
        SnimiNalpN();
        AzuriraLblRec();
    }

    // ── BRISANJE NALOGA — briše sve redove za tekući BRNAL + cleanup ──
    [RelayCommand]
    private void BrisanjeNaloga()
    {
        if (SelectedRow == null) return;
        var brnal = SelectedRow.Brnal.Trim();
        if (MessageBox.Show(
            $"Brisanje celog naloga {brnal} iz nedovršenih?",
            "Potvrda", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        // legacy: DELETE ALL FOR BRNAL=MBRNAL, then DELETE ALL FOR DUG=0 AND POT=0
        var zaDelete = Redovi.Where(r => r.Brnal.Trim() == brnal).ToList();
        foreach (var r in zaDelete) Redovi.Remove(r);
        var prazni = Redovi.Where(r => r.Dug == 0 && r.Pot == 0).ToList();
        foreach (var r in prazni) Redovi.Remove(r);
        SnimiNalpN();
        AzuriraLblRec();
    }

    // ── PRAZNI SVE NALOGE — briše sve redove DUG=POT=0 ──────────
    [RelayCommand]
    private void PrazniSve()
    {
        if (MessageBox.Show(
            "Brisanje svih praznina (DUG=POT=0) iz nedovršenih naloga?",
            "Potvrda", MessageBoxButton.YesNo, MessageBoxImage.Question)
            != MessageBoxResult.Yes) return;

        var prazni = Redovi.Where(r => r.Dug == 0 && r.Pot == 0).ToList();
        foreach (var r in prazni) Redovi.Remove(r);
        SnimiNalpN();
        AzuriraLblRec();
    }

    // ── PRENOS U IZRADU NALOGA — kopira BRNAL iz nalpn → nalp.dbf ──
    [RelayCommand]
    private void PrenosUNalp()
    {
        if (SelectedRow == null) return;
        var brnal = SelectedRow.Brnal.Trim();

        if (MessageBox.Show(
            $" PRENOS U IZRADU NALOGA {brnal}",
            "Potvrda", MessageBoxButton.YesNo, MessageBoxImage.Question)
            != MessageBoxResult.Yes) return;

        if (!File.Exists(_nalpPath))
        {
            MessageBox.Show("nalp.dbf nije pronađen!", "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            // učitaj postojeće nalp.dbf redove
            var nalReader = new SimpleDbfReader(_nalpPath);
            var nalRows   = nalReader.Zapisi().Select(NalpRowFromRecord).ToList();

            // dodaj nalpn redove koji nisu prazni
            var prenosni = Redovi
                .Where(r => r.Brnal.Trim() == brnal && (r.Dug != 0 || r.Pot != 0))
                .Select(r => r.Clone())
                .ToList();
            nalRows.AddRange(prenosni);

            // snimi nalp.dbf
            var schema = DbfTableWriter.LoadSchema(_nalpPath);
            DbfTableWriter.WriteTable(_nalpPath, schema, nalRows, NalpRowFieldMapper);

            // obrisi iz nalpn i cleanup
            var zaDelete = Redovi.Where(r => r.Brnal.Trim() == brnal).ToList();
            foreach (var r in zaDelete) Redovi.Remove(r);
            SnimiNalpN();
            AzuriraLblRec();

            MessageBox.Show($"Nalog {brnal} prenesen u izradu naloga.", "OK",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri prenosu: {ex.Message}", "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
            _log.Error(ex, "PrenosUNalp — greška");
        }
    }

    // ── SORT — ciklično sortiranje ────────────────────────────────
    [RelayCommand]
    private void Sort()
    {
        _sortMode = _sortMode switch
        {
            SortMode.Original  => SortMode.ByBrnal,
            SortMode.ByBrnal   => SortMode.ByKonto,
            SortMode.ByKonto   => SortMode.ByDatdok,
            SortMode.ByDatdok  => SortMode.Original,
            _                  => SortMode.Original,
        };

        var sortirani = _sortMode switch
        {
            SortMode.ByBrnal  => Redovi.OrderBy(r => r.Brnal).ToList(),
            SortMode.ByKonto  => Redovi.OrderBy(r => r.Konto).ToList(),
            SortMode.ByDatdok => Redovi.OrderBy(r => r.Datdok).ToList(),
            _                 => Redovi.OrderBy(r => r.Idbr).ToList(),
        };

        Redovi = new ObservableCollection<NalpRow>(sortirani);
        if (Redovi.Count > 0) SelectedRow = Redovi[0];
        AzuriraLblRec();
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    // ── Snimanje nalpn.dbf ────────────────────────────────────────
    private void SnimiNalpN()
    {
        if (!File.Exists(_nalpNPath)) return;
        try
        {
            var schema = DbfTableWriter.LoadSchema(_nalpNPath);
            DbfTableWriter.WriteTable(_nalpNPath, schema, Redovi.ToList(), NalpRowFieldMapper);
        }
        catch (Exception ex) { _log.Error(ex, "SnimiNalpN — greška"); }
    }

    private static NalpRow NalpRowFromRecord(DbfRecord rec) => new()
    {
        Konto     = rec.DajString("KONTO"),     Dug       = rec.DajDecimal("DUG"),
        Pot       = rec.DajDecimal("POT"),      Opis      = rec.DajString("OPIS"),
        Datdok    = rec.DajDate("DATDOK"),      Brnal     = rec.DajString("BRNAL"),
        Dok       = rec.DajString("DOK"),       Mp        = rec.DajDecimal("MP"),
        Mtr       = rec.DajDecimal("MTR"),      Dev       = rec.DajString("DEV"),
        Devkurs   = rec.DajDecimal("DEVKURS"),  Devdug    = rec.DajDecimal("DEVDUG"),
        Devpot    = rec.DajDecimal("DEVPOT"),   Brdok     = rec.DajString("BRDOK"),
        Napomena1 = rec.DajString("NAPOMENA1"), Napomena2 = rec.DajString("NAPOMENA2"),
        Kurs      = rec.DajDecimal("KURS"),     Kursdug   = rec.DajDecimal("KURSDUG"),
        Kurspot   = rec.DajDecimal("KURSPOT"),  Dpsaldo   = rec.DajDecimal("DPSALDO"),
        Cena      = rec.DajDecimal("CENA"),     Ulaz      = rec.DajDecimal("ULAZ"),
        Izlaz     = rec.DajDecimal("IZLAZ"),    UkupnoD   = rec.DajDecimal("UKUPNO_D"),
        UkupnoP   = rec.DajDecimal("UKUPNO_P"), Sifra     = rec.DajString("SIFRA"),
        Brrac     = rec.DajString("BRRAC"),     Valuta    = rec.DajString("VALUTA"),
        Datpri    = rec.DajDate("DATPRI"),      Datpdv    = rec.DajDate("DATPDV"),
        Stanje    = rec.DajDecimal("STANJE"),   Saldo     = rec.DajDecimal("SALDO"),
        Oznaka    = rec.DajString("OZNAKA"),    Datum     = rec.DajDate("DATUM"),
        Vreme     = rec.DajString("VREME"),     Skonto    = rec.DajDecimal("SKONTO"),
        Automnal  = rec.DajString("AUTOMNAL"),  Oper      = rec.DajString("OPER"),
        Probni    = rec.DajString("PROBNI"),    Gkonto    = rec.DajString("GKONTO"),
        Arhiva    = rec.DajString("ARHIVA"),    Arhiva2   = rec.DajString("ARHIVA2"),
        Devizno   = rec.DajString("DEVIZNO"),   Vrsta     = rec.DajString("VRSTA"),
        Imetabele = rec.DajString("IMETABELE"), Datrazduz = rec.DajDate("DATRAZDUZ"),
        Opisu     = rec.DajString("OPISU"),     Dinrazduz = rec.DajDecimal("DINRAZDUZ"),
        Sifprod   = rec.DajDecimal("SIFPROD"),  Dp        = rec.DajString("DP"),
        Doddug    = rec.DajDecimal("DODDUG"),   Dodpot    = rec.DajDecimal("DODPOT"),
        Preneto   = rec.DajString("PRENETO"),   Numred    = rec.DajDecimal("NUMRED"),
        Idbr      = rec.DajDecimal("IDBR"),
    };

    private static object? NalpRowFieldMapper(NalpRow r, string f) => f.ToUpperInvariant() switch
    {
        "KONTO"     => r.Konto,    "DUG"       => r.Dug,       "POT"       => r.Pot,
        "OPIS"      => r.Opis,     "DATDOK"    => r.Datdok,    "BRNAL"     => r.Brnal,
        "DOK"       => r.Dok,      "MP"        => r.Mp,        "MTR"       => r.Mtr,
        "DEV"       => r.Dev,      "DEVKURS"   => r.Devkurs,   "DEVDUG"    => r.Devdug,
        "DEVPOT"    => r.Devpot,   "BRDOK"     => r.Brdok,     "NAPOMENA1" => r.Napomena1,
        "NAPOMENA2" => r.Napomena2,"KURS"      => r.Kurs,      "KURSDUG"   => r.Kursdug,
        "KURSPOT"   => r.Kurspot,  "DPSALDO"   => r.Dpsaldo,   "CENA"      => r.Cena,
        "ULAZ"      => r.Ulaz,     "IZLAZ"     => r.Izlaz,     "UKUPNO_D"  => r.UkupnoD,
        "UKUPNO_P"  => r.UkupnoP,  "SIFRA"     => r.Sifra,     "BRRAC"     => r.Brrac,
        "VALUTA"    => r.Valuta,   "DATPRI"    => r.Datpri,    "DATPDV"    => r.Datpdv,
        "STANJE"    => r.Stanje,   "SALDO"     => r.Saldo,     "OZNAKA"    => r.Oznaka,
        "DATUM"     => r.Datum,    "VREME"     => r.Vreme,     "SKONTO"    => r.Skonto,
        "AUTOMNAL"  => r.Automnal, "OPER"      => r.Oper,      "PROBNI"    => r.Probni,
        "GKONTO"    => r.Gkonto,   "ARHIVA"    => r.Arhiva,    "ARHIVA2"   => r.Arhiva2,
        "DEVIZNO"   => r.Devizno,  "VRSTA"     => r.Vrsta,     "IMETABELE" => r.Imetabele,
        "DATRAZDUZ" => r.Datrazduz,"OPISU"     => r.Opisu,     "DINRAZDUZ" => r.Dinrazduz,
        "SIFPROD"   => r.Sifprod,  "DP"        => r.Dp,        "DODDUG"    => r.Doddug,
        "DODPOT"    => r.Dodpot,   "PRENETO"   => r.Preneto,   "NUMRED"    => r.Numred,
        "IDBR"      => r.Idbr,     _           => null,
    };
}
