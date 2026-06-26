using Algoritam.Core.Services;
using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class Nalp2ViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext<Nalp2ViewModel>();

    private readonly AppState _appState;
    private readonly string   _firmPath;

    // ═══ GLOBALKE (iz legacy ANPAR tabele) ═══
    public string PkPlans    { get; private set; } = string.Empty;
    public string PkOndopu   { get; private set; } = string.Empty;
    public string PkTkr      { get; private set; } = string.Empty;
    public string PkAnAutoz  { get; private set; } = string.Empty;
    public string PkNjan     { get; private set; } = string.Empty;
    // ═══ KOLONE FORME ═══
    [ObservableProperty] private ObservableCollection<NalpRow> _nalpRows = [];
    [ObservableProperty] private NalpRow?   _selectedRow;
    [ObservableProperty] private string     _brnal       = string.Empty;
    [ObservableProperty] private DateTime?  _datdok;
    [ObservableProperty] private string     _lblRec      = string.Empty;

    // info panel (ispod grida, vezan na SEL red)
    [ObservableProperty] private string _txtKonto2  = string.Empty;
    [ObservableProperty] private string _txtNaziv   = string.Empty;
    [ObservableProperty] private string _txtMp      = string.Empty;
    [ObservableProperty] private string _txtMesto   = string.Empty;
    [ObservableProperty] private string _txtMtr     = string.Empty;
    [ObservableProperty] private string _txtNaziv2  = string.Empty;

    // AN0 header (top)
    [ObservableProperty] private string _txtSifra  = string.Empty;
    [ObservableProperty] private string _txtNaziv3 = string.Empty;

    // totali
    [ObservableProperty] private string  _txtDug    = string.Empty;
    [ObservableProperty] private string  _txtPot    = string.Empty;
    [ObservableProperty] private string  _txtSaldo  = string.Empty;
    [ObservableProperty] private bool    _saldoJeNula = true;

    public string Caption { get; private set; } = string.Empty;

    // lookup tabele (in-memory rečnici)
    private Dictionary<string, (string Naziv, string unused)> _konto   = [];
    private Dictionary<string, (string Mesto, string Mp)>     _mesta   = [];
    private Dictionary<string, string>                        _mtr     = [];
    private Dictionary<string, string>                        _an0Naz  = [];
    private Dictionary<string, NalpDefkRow>                   _nalpdefk = [];

    // Event koji View hvata da zatvori prozor
    public event Action? ZatvoriFormu;

    public Nalp2ViewModel(AppState appState, string firmPath, string brnal)
    {
        _appState = appState;
        _firmPath = firmPath;
        Brnal     = brnal.PadLeft(6);

        Caption = $" IZRADA NALOGA ZA KNJIŽENJE DRUGI NAČIN  " +
                  $"{appState.TrenutniKorisnik?.KorisnikIme ?? ""} " +
                  $"{appState.AktivnaFirma?.Naziv ?? ""}";

        UcitajAnpar();
        UcitajLookups();
        UcitajNalpRows();
        AzuriraTotale();
    }

    // ═══════════════════════════════════════════════════════
    // UČITAVANJE
    // ═══════════════════════════════════════════════════════

    private void UcitajAnpar()
    {
        var path = Dbf("anpar.dbf");
        if (path == null) return;
        try
        {
            var r = new SimpleDbfReader(path);
            foreach (var rec in r.Zapisi())
            {
                PkPlans   = rec.DajString("KPLANS");
                PkOndopu  = rec.DajString("KONDOPU");
                PkTkr     = rec.DajString("KTKR");
                PkAnAutoz = rec.DajString("KANAUTOZ");
                PkNjan    = rec.DajString("KNJAN");
                break;
            }
        }
        catch (Exception ex) { _log.Warning(ex, "anpar.dbf nije učitan"); }
    }

    private void UcitajLookups()
    {
        UcitajKonto();
        UcitajMesta();
        UcitajMtr();
        UcitajAn0();
        UcitajNalpdefk();
    }

    private void UcitajKonto()
    {
        var path = Dbf("konto.dbf");
        if (path == null) return;
        try
        {
            var r = new SimpleDbfReader(path);
            _konto = r.Zapisi()
                      .ToDictionary(
                          rec => rec.DajString("KONTO").Trim(),
                          rec => (rec.DajString("NAZIV"), ""),
                          StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) { _log.Warning(ex, "konto.dbf nije učitan"); }
    }

    private void UcitajMesta()
    {
        var path = Dbf("mesta.dbf");
        if (path == null) return;
        try
        {
            var r = new SimpleDbfReader(path);
            _mesta = r.Zapisi()
                      .ToDictionary(
                          rec => rec.DajString("MP").Trim(),
                          rec => (rec.DajString("MESTO"), rec.DajString("MP")),
                          StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) { _log.Warning(ex, "mesta.dbf nije učitan"); }
    }

    private void UcitajMtr()
    {
        var path = Dbf("mtr.dbf");
        if (path == null) return;
        try
        {
            var r = new SimpleDbfReader(path);
            _mtr = r.Zapisi()
                    .ToDictionary(
                        rec => rec.DajString("MTR").Trim(),
                        rec => rec.DajString("NAZIV"),
                        StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) { _log.Warning(ex, "mtr.dbf nije učitan"); }
    }

    private void UcitajAn0()
    {
        var path = Dbf("an0.dbf");
        if (path == null) return;
        try
        {
            var r = new SimpleDbfReader(path);
            _an0Naz = r.Zapisi()
                       .ToDictionary(
                           rec => rec.DajString("SIFRA").Trim(),
                           rec => rec.DajString("NAZIV"),
                           StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) { _log.Warning(ex, "an0.dbf nije učitan"); }
    }

    private void UcitajNalpdefk()
    {
        var path = Dbf("nalpdefk.dbf");
        if (path == null) return;
        try
        {
            var r = new SimpleDbfReader(path);
            _nalpdefk = r.Zapisi()
                         .Select(rec => new NalpDefkRow
                         {
                             Sifprod   = rec.DajDecimal("SIFPROD"),
                             Konto     = rec.DajString("KONTO").Trim(),
                             Pnaziv    = rec.DajString("PNAZIV"),
                             Devizno   = rec.DajString("DEVIZNO"),
                             Sifarnik  = rec.DajString("SIFARNIK"),
                             Dok       = rec.DajString("DOK"),
                             Vrsta     = rec.DajString("VRSTA").Trim(),
                             Imetabele = rec.DajString("IMETABELE"),
                             Dp        = rec.DajString("DP"),
                             Preneto   = rec.DajString("PRENETO"),
                             Numred    = rec.DajDecimal("NUMRED"),
                             Idbr      = rec.DajDecimal("IDBR"),
                         })
                         .GroupBy(d => d.Konto, StringComparer.OrdinalIgnoreCase)
                         .ToDictionary(g => g.Key, g => g.First(),
                                       StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) { _log.Warning(ex, "nalpdefk.dbf nije učitan"); }
    }

    public void UcitajNalpRows()
    {
        var path = Dbf("nalp.dbf");
        if (path == null)
        {
            NalpRows = [];
            return;
        }
        try
        {
            var r = new SimpleDbfReader(path);
            var rows = r.Zapisi()
                .Where(rec => rec.DajString("BRNAL").Trim() == Brnal.Trim())
                .Select(rec => NalpRowFromRecord(rec))
                .ToList();
            NalpRows = new ObservableCollection<NalpRow>(rows);
            if (NalpRows.Count > 0)
                SelectedRow = NalpRows[0];
        }
        catch (Exception ex)
        {
            _log.Error(ex, "nalp.dbf — greška pri učitavanju");
            NalpRows = [];
        }
    }

    internal static NalpRow NalpRowFromRecord(DbfRecord rec) => new()
    {
        Konto     = rec.DajString("KONTO"),
        Dug       = rec.DajDecimal("DUG"),
        Pot       = rec.DajDecimal("POT"),
        Opis      = rec.DajString("OPIS"),
        Datdok    = rec.DajDate("DATDOK"),
        Brnal     = rec.DajString("BRNAL"),
        Dok       = rec.DajString("DOK"),
        Mp        = rec.DajDecimal("MP"),
        Mtr       = rec.DajDecimal("MTR"),
        Dev       = rec.DajString("DEV"),
        Devkurs   = rec.DajDecimal("DEVKURS"),
        Devdug    = rec.DajDecimal("DEVDUG"),
        Devpot    = rec.DajDecimal("DEVPOT"),
        Brdok     = rec.DajString("BRDOK"),
        Napomena1 = rec.DajString("NAPOMENA1"),
        Napomena2 = rec.DajString("NAPOMENA2"),
        Kurs      = rec.DajDecimal("KURS"),
        Kursdug   = rec.DajDecimal("KURSDUG"),
        Kurspot   = rec.DajDecimal("KURSPOT"),
        Dpsaldo   = rec.DajDecimal("DPSALDO"),
        Cena      = rec.DajDecimal("CENA"),
        Ulaz      = rec.DajDecimal("ULAZ"),
        Izlaz     = rec.DajDecimal("IZLAZ"),
        UkupnoD   = rec.DajDecimal("UKUPNO_D"),
        UkupnoP   = rec.DajDecimal("UKUPNO_P"),
        Sifra     = rec.DajString("SIFRA"),
        Brrac     = rec.DajString("BRRAC"),
        Valuta    = rec.DajString("VALUTA"),
        Datpri    = rec.DajDate("DATPRI"),
        Datpdv    = rec.DajDate("DATPDV"),
        Stanje    = rec.DajDecimal("STANJE"),
        Saldo     = rec.DajDecimal("SALDO"),
        Oznaka    = rec.DajString("OZNAKA"),
        Datum     = rec.DajDate("DATUM"),
        Vreme     = rec.DajString("VREME"),
        Skonto    = rec.DajDecimal("SKONTO"),
        Automnal  = rec.DajString("AUTOMNAL"),
        Oper      = rec.DajString("OPER"),
        Probni    = rec.DajString("PROBNI"),
        Gkonto    = rec.DajString("GKONTO"),
        Arhiva    = rec.DajString("ARHIVA"),
        Arhiva2   = rec.DajString("ARHIVA2"),
        Devizno   = rec.DajString("DEVIZNO"),
        Vrsta     = rec.DajString("VRSTA"),
        Imetabele = rec.DajString("IMETABELE"),
        Datrazduz = rec.DajDate("DATRAZDUZ"),
        Opisu     = rec.DajString("OPISU"),
        Dinrazduz = rec.DajDecimal("DINRAZDUZ"),
        Sifprod   = rec.DajDecimal("SIFPROD"),
        Dp        = rec.DajString("DP"),
        Doddug    = rec.DajDecimal("DODDUG"),
        Dodpot    = rec.DajDecimal("DODPOT"),
        Preneto   = rec.DajString("PRENETO"),
        Numred    = rec.DajDecimal("NUMRED"),
        Idbr      = rec.DajDecimal("IDBR"),
    };

    // ═══════════════════════════════════════════════════════
    // AfterRowColChange — osvežava info panel i totale
    // (poziva se iz View kada se promeni SelectedRow)
    // ═══════════════════════════════════════════════════════

    partial void OnSelectedRowChanged(NalpRow? value)
    {
        if (value == null) return;

        var kontoKey = value.Konto.Trim();
        TxtKonto2 = kontoKey;
        TxtNaziv  = _konto.TryGetValue(kontoKey, out var kt) ? kt.Naziv : string.Empty;

        var mpKey = ((int)value.Mp).ToString().PadLeft(2);
        TxtMp    = mpKey.Trim();
        TxtMesto = _mesta.TryGetValue(mpKey.Trim(), out var m) ? m.Mesto : string.Empty;

        var mtrKey = ((int)value.Mtr).ToString().PadLeft(5);
        TxtMtr   = mtrKey.Trim();
        TxtNaziv2 = _mtr.TryGetValue(mtrKey.Trim(), out var mtrNaz) ? mtrNaz : string.Empty;

        LblRec = $"{NalpRows.IndexOf(value) + 1,6}/{NalpRows.Count,6}";
        AzuriraTotale();

        // AN0 header
        var sifra = value.Sifra.Trim();
        TxtSifra  = sifra;
        TxtNaziv3 = _an0Naz.TryGetValue(sifra, out var an0n) ? an0n : string.Empty;

        // Datdok iz prvog reda
        Datdok = value.Datdok;
    }

    private void AzuriraTotale()
    {
        var brnal = Brnal.Trim();
        var rows  = NalpRows.Where(r => r.Brnal.Trim() == brnal).ToList();
        var mdug  = rows.Sum(r => r.Dug);
        var mpot  = rows.Sum(r => r.Pot);
        var sal   = mdug - mpot;

        TxtDug    = sal == 0
            ? FormatBroj(mdug)
            : FormatBroj(mdug);
        TxtPot    = FormatBroj(mpot);
        TxtSaldo  = FormatBroj(sal);
        SaldoJeNula = sal == 0;
    }

    private static string FormatBroj(decimal v) =>
        v.ToString("### ### ### ###.##").Trim().PadLeft(18);

    // ═══════════════════════════════════════════════════════
    // POSLOVNI DOGAĐAJI (pozivaju se iz View na cell eventi)
    // ═══════════════════════════════════════════════════════

    /// <summary>Column1 (Konto) LostFocus — odgovara Legacy Column1.LostFocus</summary>
    public void OnKontoLostFocus(NalpRow row)
    {
        if (PkPlans != "D" && PkOndopu == "D")
        {
            var k = row.Konto.TrimEnd();
            row.Konto = k.PadRight(10, '0');
        }

        if (row.Dug == 0 && row.Pot == 0 && !string.IsNullOrWhiteSpace(row.Konto))
            OtvoriNalp2Kart(row);

        AzuriraTotale();
    }

    /// <summary>Column2 (Dug) LostFocus</summary>
    public void OnDugLostFocus(NalpRow row)
    {
        if (row.Dug != 0 && row.Pot != 0)
            row.Pot = 0;
        if (row.Konto.Trim() == "9999999999")
        { row.Dug = 0; row.Pot = 0; }
        AzuriraTotale();
    }

    /// <summary>Column3 (Pot) LostFocus</summary>
    public void OnPotLostFocus(NalpRow row)
    {
        if (row.Dug != 0 && row.Pot != 0)
            row.Dug = 0;
        if (row.Konto.Trim() == "9999999999")
        { row.Dug = 0; row.Pot = 0; }
        AzuriraTotale();
        OnPropertyChanged(nameof(NalpRows));
    }

    /// <summary>Column11 (Dev) LostFocus — traži kurs iz dev.dbf</summary>
    public void OnDevLostFocus(NalpRow row)
    {
        var devVal = row.Dev.Trim();
        if (string.IsNullOrEmpty(devVal)) return;

        var devPath = Dbf("dev.dbf");
        if (devPath == null) return;
        try
        {
            var r = new SimpleDbfReader(devPath);
            var datKey = row.Datdok.HasValue
                ? row.Datdok.Value.ToString("yyyyMMdd")
                : DateTime.Today.ToString("yyyyMMdd");
            var searchKey = devVal + datKey;
            foreach (var rec in r.Zapisi())
            {
                var k2 = rec.DajString("DEV").Trim() + rec.DajString("DATDOK").Trim();
                if (string.Equals(k2, searchKey, StringComparison.OrdinalIgnoreCase))
                {
                    row.Dev     = rec.DajString("DEV");
                    row.Devkurs = rec.DajDecimal("KURS");
                    return;
                }
            }
        }
        catch (Exception ex) { _log.Warning(ex, "dev.dbf kurs nije nađen"); }
    }

    private void OtvoriNalp2Kart(NalpRow row)
    {
        var kontoKey = row.Konto.Trim();
        if (!_nalpdefk.TryGetValue(kontoKey, out var defk)) return;

        if (defk.Vrsta.Trim() == "AN")
        {
            row.Devizno   = defk.Devizno;
            row.Imetabele = defk.Imetabele;
            row.Vrsta     = defk.Vrsta;
            row.Sifprod   = defk.Sifprod;
            row.Dp        = defk.Dp;

            var vm  = new Nalp2KartViewModel(row, _firmPath, _an0Naz, _mtr, _mesta);
            var win = new Views.Nalp2KartWindow(vm);
            win.ShowDialog();
        }
        else if (defk.Vrsta.Trim() == "MP")
        {
            row.Devizno   = defk.Devizno;
            row.Imetabele = defk.Imetabele;
            row.Vrsta     = defk.Vrsta;
            row.Sifprod   = defk.Sifprod;
            row.Dp        = defk.Dp;

            var vm  = new Nalp2KartMpViewModel(row, _nalpdefk, _firmPath);
            var win = new Views.Nalp2KartMpWindow(vm);
            win.ShowDialog();
        }
    }

    // ═══════════════════════════════════════════════════════
    // KOMANDE
    // ═══════════════════════════════════════════════════════

    /// <summary>Command23 — PRAZNI NALOG S+F7</summary>
    [RelayCommand]
    private void PrazniNalog()
    {
        var brnal = Brnal.Trim();
        foreach (var r in NalpRows.Where(r => r.Brnal.Trim() == brnal))
        {
            r.Dug    = 0; r.Pot    = 0;
            r.Devdug = 0; r.Devpot = 0;
        }
        AzuriraTotale();
        SnimiNalpDbf();
    }

    /// <summary>CMDDODAJ — dodaje novi red</summary>
    [RelayCommand]
    private void DodajRed()
    {
        if (NalpRows.Count == 0) return;
        var curr = SelectedRow ?? NalpRows.Last();
        var nova = new NalpRow
        {
            Konto   = curr.Konto,
            Opis    = curr.Opis,
            Brnal   = Brnal.PadLeft(6),
            Datdok  = curr.Datdok,
            Oper    = _appState.TrenutniKorisnik?.KorisnikIme ?? string.Empty,
            Datum   = DateTime.Today,
            Vreme   = DateTime.Now.ToString("HH:mm:ss"),
            Dok     = curr.Dok,
            Mtr     = curr.Mtr,
            Mp      = curr.Mp,
        };
        NalpRows.Add(nova);
        SelectedRow = nova;
        SnimiNalpDbf();
    }

    /// <summary>Command12 — BRISANJE PRAZNINA</summary>
    [RelayCommand]
    private void BrisiPraznine()
    {
        var brnal = Brnal.Trim();
        var prazni = NalpRows
            .Where(r => r.Brnal.Trim() == brnal && r.Dug == 0 && r.Pot == 0)
            .ToList();
        foreach (var r in prazni)
            NalpRows.Remove(r);

        SnimiNalpDbf();
        ZatvoriFormu?.Invoke();
    }

    /// <summary>Command24 — KOPIRAJ RED S+F5</summary>
    [RelayCommand]
    private void KopirajRed()
    {
        if (SelectedRow == null) return;
        var klon = SelectedRow.Clone();
        NalpRows.Add(klon);
        SelectedRow = klon;
        SnimiNalpDbf();
    }

    /// <summary>Command20 — BRISANJE NALOGA</summary>
    [RelayCommand]
    private void BrisiNalog()
    {
        var brnal = Brnal.Trim();
        if (MessageBox.Show(
            $"Brisanje celog naloga {brnal}?\n\nDa li ste sigurni?",
            "Potvrda brisanja",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var zaDelete = NalpRows.Where(r => r.Brnal.Trim() == brnal).ToList();
        foreach (var r in zaDelete)
            NalpRows.Remove(r);
        SnimiNalpDbf();
        ZatvoriFormu?.Invoke();
    }

    /// <summary>Command7 — KNJIŽI F5</summary>
    [RelayCommand]
    private void Knjizi()
    {
        var brnal  = Brnal.Trim();
        var redovi = NalpRows.Where(r => r.Brnal.Trim() == brnal).ToList();
        var mdug   = redovi.Sum(r => r.Dug);
        var mpot   = redovi.Sum(r => r.Pot);
        bool ispravan = redovi.All(r =>
            !string.IsNullOrWhiteSpace(r.Konto) || (r.Dug == 0 && r.Pot == 0));

        if (mdug != mpot || !ispravan)
        {
            MessageBox.Show(" NALOG NIJE U RAVNOTEŽI ILI NEMA KONTA ",
                "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show($" KNJIŽENJE NALOGA {brnal}",
            "Potvrda", MessageBoxButton.YesNo, MessageBoxImage.Question)
            != MessageBoxResult.Yes)
            return;

        // scatter → NAL
        var nalPath = Dbf("nal.dbf");
        if (nalPath == null)
        {
            MessageBox.Show("nal.dbf nije pronađen!", "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        try
        {
            // učitaj sve postojeće NAL redove
            var nalReader = new SimpleDbfReader(nalPath);
            var nalRows   = nalReader.Zapisi()
                                     .Select(r => NalpRowFromRecord(r))
                                     .ToList();

            // dodaj NALP redove koji imaju DUG ili POT
            foreach (var r in redovi.Where(r => r.Dug != 0 || r.Pot != 0))
            {
                var nov = r.Clone();
                nov.Datum = DateTime.Today;
                nov.Vreme = DateTime.Now.ToString("HH:mm:ss");
                nalRows.Add(nov);
            }

            // snimi NAL
            var schema = DbfTableWriter.LoadSchema(nalPath);
            DbfTableWriter.WriteTable(nalPath, schema, nalRows, NalpRowFieldMapper);

            // obeleži NALBROJ.DATKNJI
            AzuriraNalbrojDatknji(brnal);

            // obriši NALP redove
            foreach (var r in redovi)
                NalpRows.Remove(r);
            SnimiNalpDbf();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju: {ex.Message}", "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
            _log.Error(ex, "Knjizi — greška");
            return;
        }

        ZatvoriFormu?.Invoke();
    }

    private void AzuriraNalbrojDatknji(string brnal)
    {
        var path = Dbf("nalbroj.dbf");
        if (path == null) return;
        try
        {
            var r    = new SimpleDbfReader(path);
            var rows = r.Zapisi().Select(rec => new Dictionary<string, object?>
            {
                ["BRNAL"]   = rec.DajString("BRNAL"),
                ["VRNAL"]   = rec.DajString("VRNAL"),
                ["DATNAL"]  = rec.DajDate("DATNAL"),
                ["DATKNJI"] = rec.DajString("BRNAL").Trim() == brnal.Trim()
                                ? (object?)DateTime.Today
                                : rec.DajDate("DATKNJI"),
                ["OPER"]    = rec.DajString("OPER"),
                ["IDBR"]    = rec.DajDecimal("IDBR"),
            }).ToList();

            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, rows,
                (d, f) => d.TryGetValue(f.ToUpperInvariant(), out var v) ? v : null);
        }
        catch (Exception ex) { _log.Warning(ex, "nalbroj DATKNJI nije ažuriran"); }
    }

    /// <summary>Command6 — DODAJ ZBIR F6</summary>
    [RelayCommand]
    private void DodajZbir()
    {
        var brnal = Brnal.Trim();
        // traži konto iz NALVRSTA
        var nalvPath = Dbf("nalvrsta.dbf");
        string kontoI = string.Empty;
        if (nalvPath != null)
        {
            try
            {
                // nalbroj → vrnal
                var nbPath = Dbf("nalbroj.dbf");
                string vrnal = string.Empty;
                if (nbPath != null)
                {
                    var nbr = new SimpleDbfReader(nbPath);
                    foreach (var rec in nbr.Zapisi())
                        if (rec.DajString("BRNAL").Trim() == brnal)
                        { vrnal = rec.DajString("VRNAL").Trim(); break; }
                }

                if (!string.IsNullOrEmpty(vrnal))
                {
                    var nvr = new SimpleDbfReader(nalvPath);
                    foreach (var rec in nvr.Zapisi())
                        if (rec.DajString("VRNAL").Trim() == vrnal)
                        { kontoI = rec.DajString("KONTO").Trim(); break; }
                }
            }
            catch { }
        }

        if (string.IsNullOrEmpty(kontoI)) return;

        var redovi = NalpRows.Where(r => r.Brnal.Trim() == brnal).ToList();
        bool postoji = redovi.Any(r => r.Konto.Trim() == kontoI);
        if (postoji) return;

        var mdug  = redovi.Sum(r => r.Dug);
        var mpot  = redovi.Sum(r => r.Pot);
        var opis  = redovi.LastOrDefault()?.Opis ?? string.Empty;
        var datd  = redovi.LastOrDefault()?.Datdok;

        if (mpot != 0)
        {
            var nr = new NalpRow { Konto = kontoI, Opis = opis, Datdok = datd,
                Dug = mpot, Brnal = Brnal, Datum = DateTime.Today,
                Vreme = DateTime.Now.ToString("HH:mm:ss") };
            NalpRows.Add(nr);
        }
        if (mdug != 0)
        {
            var nr = new NalpRow { Konto = kontoI, Opis = opis, Datdok = datd,
                Pot = mdug, Brnal = Brnal, Datum = DateTime.Today,
                Vreme = DateTime.Now.ToString("HH:mm:ss") };
            NalpRows.Add(nr);
        }

        if (NalpRows.Count > 0) SelectedRow = NalpRows.Last();
        AzuriraTotale();
        SnimiNalpDbf();
    }

    /// <summary>CMDKON — Kontni plan F4 (otvara browse formu)</summary>
    [RelayCommand]
    private void KontniPlan()
    {
        var vm = new KontoPlanViewModel(_firmPath, "konto.dbf", "KONTNI PLAN");
        new Views.KontoPlanWindow(vm).ShowDialog();
    }

    /// <summary>Command13 — NALOG F10</summary>
    [RelayCommand]
    private void NalogF10()
    {
        OtvoriPregled("PREGLED NALOGA", NalpRows);
    }

    /// <summary>Command19 — DEVIZNI NALOG A+F10</summary>
    [RelayCommand]
    private void DevizniNalog()
    {
        OtvoriPregled("DEVIZNI PREGLED NALOGA", NalpRows);
    }

    /// <summary>Command32 — NALOG EKO</summary>
    [RelayCommand]
    private void NalogEko()
    {
        OtvoriPregled("EKO PREGLED NALOGA", NalpRows);
    }

    /// <summary>Command25 — PARAMETRI A+F9</summary>
    [RelayCommand]
    private void KepuKnjige()
    {
        var datum = SelectedRow?.Datdok ?? Datdok ?? DateTime.Today;
        var vm = new NalptkViewModel(_firmPath, Brnal, datum);
        vm.StavkeFormirane += DodajKepuStavke;
        new Views.NalptkWindow(vm).ShowDialog();
    }

    private void DodajKepuStavke(IReadOnlyList<NalpRow> stavke)
    {
        foreach (var stavka in stavke)
            NalpRows.Add(stavka);

        SelectedRow = NalpRows.LastOrDefault();
        AzuriraTotale();
        SnimiNalpDbf();
    }

    [RelayCommand]
    private void Parametri()
    {
        var vm = new NalanparViewModel(_firmPath);
        new Views.NalanparWindow(vm).ShowDialog();
        UcitajAnpar();
    }

    private static void OtvoriPregled(string naslov, IEnumerable<NalpRow> rows)
    {
        var vm = new NalogPregledViewModel(
            naslov, rows.Select(NalogPregledViewModel.IzNalp));
        new Views.NalogPregledWindow(vm).ShowDialog();
    }

    /// <summary>ALT+F2 — EVIDENCIJA ULAZA MATERIJALA</summary>
    [RelayCommand]
    private void NalpMatul()
    {
        if (SelectedRow == null) return;
        var vm  = new NalpMatulViewModel(SelectedRow, _firmPath);
        var win = new Views.NalpMatulWindow(vm);
        win.ShowDialog();
    }

    /// <summary>ALT+F3 — EVIDENCIJA IZLAZA MATERIJALA</summary>
    [RelayCommand]
    private void NalpMatiz()
    {
        if (SelectedRow == null) return;
        var vm  = new NalpMatizViewModel(SelectedRow, _firmPath);
        var win = new Views.NalpMatizWindow(vm);
        win.ShowDialog();
    }

    /// <summary>ALT+F6 — DOPUNSKI PODACI ANALITIKE</summary>
    [RelayCommand]
    private void NalpDopan()
    {
        if (SelectedRow == null) return;
        var vm  = new NalpDopanViewModel(SelectedRow, _firmPath);
        var win = new Views.NalpDopanWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    // ═══════════════════════════════════════════════════════
    // NAVIGACIJA (Command1/2/3/4)
    // ═══════════════════════════════════════════════════════

    [RelayCommand]
    private void IdiDole()
    {
        if (SelectedRow == null && NalpRows.Count > 0) { SelectedRow = NalpRows[0]; return; }
        var idx = NalpRows.IndexOf(SelectedRow!);
        if (idx >= 0 && idx < NalpRows.Count - 1)
            SelectedRow = NalpRows[idx + 1];
    }

    [RelayCommand]
    private void IdiGore()
    {
        var idx = NalpRows.IndexOf(SelectedRow!);
        if (idx > 0)
            SelectedRow = NalpRows[idx - 1];
    }

    [RelayCommand]
    private void IdiNaDno()
    {
        if (NalpRows.Count > 0)
            SelectedRow = NalpRows.Last();
    }

    [RelayCommand]
    private void IdiNaVrh()
    {
        if (NalpRows.Count > 0)
            SelectedRow = NalpRows[0];
    }

    // ═══════════════════════════════════════════════════════
    // SNIMANJE
    // ═══════════════════════════════════════════════════════

    public void SnimiNalpDbf()
    {
        var path = Dbf("nalp.dbf");
        if (path == null) return;
        try
        {
            // učitaj SVE redove (ostali nalozi + naši ažurirani)
            var schema   = DbfTableWriter.LoadSchema(path);
            var reader   = new SimpleDbfReader(path);
            var ostali   = reader.Zapisi()
                                 .Where(rec => rec.DajString("BRNAL").Trim() != Brnal.Trim())
                                 .Select(rec => NalpRowFromRecord(rec))
                                 .ToList();
            var svi      = ostali.Concat(NalpRows).ToList();
            DbfTableWriter.WriteTable(path, schema, svi, NalpRowFieldMapper);
        }
        catch (Exception ex) { _log.Error(ex, "SnimiNalpDbf — greška"); }
    }

    internal static object? NalpRowFieldMapper(NalpRow r, string f) => f.ToUpperInvariant() switch
    {
        "KONTO"     => r.Konto,
        "DUG"       => r.Dug,
        "POT"       => r.Pot,
        "OPIS"      => r.Opis,
        "DATDOK"    => r.Datdok,
        "BRNAL"     => r.Brnal,
        "DOK"       => r.Dok,
        "MP"        => r.Mp,
        "MTR"       => r.Mtr,
        "DEV"       => r.Dev,
        "DEVKURS"   => r.Devkurs,
        "DEVDUG"    => r.Devdug,
        "DEVPOT"    => r.Devpot,
        "BRDOK"     => r.Brdok,
        "NAPOMENA1" => r.Napomena1,
        "NAPOMENA2" => r.Napomena2,
        "KURS"      => r.Kurs,
        "KURSDUG"   => r.Kursdug,
        "KURSPOT"   => r.Kurspot,
        "DPSALDO"   => r.Dpsaldo,
        "CENA"      => r.Cena,
        "ULAZ"      => r.Ulaz,
        "IZLAZ"     => r.Izlaz,
        "UKUPNO_D"  => r.UkupnoD,
        "UKUPNO_P"  => r.UkupnoP,
        "SIFRA"     => r.Sifra,
        "BRRAC"     => r.Brrac,
        "VALUTA"    => r.Valuta,
        "DATPRI"    => r.Datpri,
        "DATPDV"    => r.Datpdv,
        "STANJE"    => r.Stanje,
        "SALDO"     => r.Saldo,
        "OZNAKA"    => r.Oznaka,
        "DATUM"     => r.Datum,
        "VREME"     => r.Vreme,
        "SKONTO"    => r.Skonto,
        "AUTOMNAL"  => r.Automnal,
        "OPER"      => r.Oper,
        "PROBNI"    => r.Probni,
        "GKONTO"    => r.Gkonto,
        "ARHIVA"    => r.Arhiva,
        "ARHIVA2"   => r.Arhiva2,
        "DEVIZNO"   => r.Devizno,
        "VRSTA"     => r.Vrsta,
        "IMETABELE" => r.Imetabele,
        "DATRAZDUZ" => r.Datrazduz,
        "OPISU"     => r.Opisu,
        "DINRAZDUZ" => r.Dinrazduz,
        "SIFPROD"   => r.Sifprod,
        "DP"        => r.Dp,
        "DODDUG"    => r.Doddug,
        "DODPOT"    => r.Dodpot,
        "PRENETO"   => r.Preneto,
        "NUMRED"    => r.Numred,
        "IDBR"      => r.Idbr,
        _           => null,
    };

    // ═══════════════════════════════════════════════════════
    // HELP
    // ═══════════════════════════════════════════════════════

    private string? Dbf(string ime)
    {
        var p = Path.Combine(_firmPath, ime);
        return File.Exists(p) ? p : null;
    }
}
