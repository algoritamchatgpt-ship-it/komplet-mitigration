using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALPLOBR — ANALIZA OBRTNIH SREDSTAVA POTRAŽIVANJA I OBAVEZA</summary>
public partial class NalplobrViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly string _dbfPath;

    public event Action?  ZatvoriFormu;
    public event Action?  OtvoriNalPlDat;

    public ObservableCollection<NalplobrRow> Rows { get; } = new();
    [ObservableProperty] private NalplobrRow? _selectedRow;

    public NalplobrViewModel(string firmPath)
    {
        _firmPath = firmPath;
        _dbfPath  = Path.Combine(firmPath, "nalplobr.dbf");
        Ucitaj();
    }

    // ── Učitavanje ───────────────────────────────────────────
    private void Ucitaj()
    {
        Rows.Clear();
        if (!File.Exists(_dbfPath)) return;
        foreach (var rec in new SimpleDbfReader(_dbfPath).Zapisi())
            Rows.Add(MapRecord(rec));
        if (Rows.Count > 0) SelectedRow = Rows[0];
    }

    private static NalplobrRow MapRecord(DbfRecord rec) => new()
    {
        K2      = rec.DajString("K2"),
        Dugpot  = rec.DajString("DUGPOT"),
        Gnaz    = rec.DajString("GNAZ"),
        Dug     = rec.DajDecimal("DUG"),
        Pot     = rec.DajDecimal("POT"),
        Saldo   = rec.DajDecimal("SALDO"),
        Dat0    = rec.DajDate("DAT0"),
        Dat1    = rec.DajDate("DAT1"),
        Preneto = rec.DajString("PRENETO"),
        Idbr    = rec.DajDecimal("IDBR"),
    };

    private static object? FieldMapper(NalplobrRow r, string f) => f switch
    {
        "K2"      => r.K2,
        "DUGPOT"  => r.Dugpot,
        "GNAZ"    => r.Gnaz,
        "DUG"     => r.Dug,
        "POT"     => r.Pot,
        "SALDO"   => r.Saldo,
        "DAT0"    => r.Dat0,
        "DAT1"    => r.Dat1,
        "PRENETO" => r.Preneto,
        "IDBR"    => r.Idbr,
        _         => null,
    };

    // ── Navigacija ───────────────────────────────────────────
    [RelayCommand]
    private void Dole()
    {
        if (SelectedRow == null || Rows.Count == 0) return;
        int idx = Rows.IndexOf(SelectedRow);
        if (idx < Rows.Count - 1) SelectedRow = Rows[idx + 1];
    }

    [RelayCommand]
    private void Gore()
    {
        if (SelectedRow == null || Rows.Count == 0) return;
        int idx = Rows.IndexOf(SelectedRow);
        if (idx > 0) SelectedRow = Rows[idx - 1];
    }

    [RelayCommand]
    private void Zadnji()
    {
        if (Rows.Count > 0) SelectedRow = Rows[^1];
    }

    [RelayCommand]
    private void Prvi()
    {
        if (Rows.Count > 0) SelectedRow = Rows[0];
    }

    // ── Dodaj (APPEND BLANK) ──────────────────────────────────
    [RelayCommand]
    private void Dodaj()
    {
        var row = new NalplobrRow();
        Rows.Add(row);
        SelectedRow = row;
    }

    // ── Brisanje reda (DELETE + PACK + Release) ───────────────
    [RelayCommand]
    private void BrisanjeReda()
    {
        if (SelectedRow == null || Rows.Count == 0) return;
        Rows.Remove(SelectedRow);
        SelectedRow = Rows.Count > 0 ? Rows[^1] : null;
        Snimi();
        ZatvoriFormu?.Invoke();
    }

    // ── Datumi i putanje (DO FORM NALPLDAT) ──────────────────
    [RelayCommand]
    private void DatumiIPutanje() => OtvoriNalPlDat?.Invoke();

    // ── Pregled analize (printer_bullzip → stub) ──────────────
    [RelayCommand]
    private void PregledAnalize()
        => MessageBox.Show("Štampa analize nije implementirana.", "Pregled analize");

    // ── Učitaj konta iz Glavne knjige (Command16) ─────────────
    [RelayCommand]
    private void UcitajKontaIzGlavneKnjige()
    {
        try
        {
            var nalPath  = Path.Combine(_firmPath, "nal.dbf");
            var kon2Path = Path.Combine(_firmPath, "kon2.dbf");
            if (!File.Exists(nalPath)) return;

            // kon2 lookup: k2 → naziv
            var kon2Map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(kon2Path))
            {
                foreach (var r in new SimpleDbfReader(kon2Path).Zapisi())
                {
                    var k = r.DajString("K2").Trim();
                    if (k.Length > 0 && !kon2Map.ContainsKey(k))
                        kon2Map[k] = r.DajString("NAZIV").Trim();
                }
            }

            // build set of existing K2 values
            var existing = new HashSet<string>(
                Rows.Select(r => r.K2.Trim()), StringComparer.OrdinalIgnoreCase);

            foreach (var rec in new SimpleDbfReader(nalPath).Zapisi())
            {
                var konto  = rec.DajString("KONTO").Trim();
                if (konto.Length < 2) continue;
                var mmkonto  = konto[0].ToString();
                var mmkonto2 = konto[..2];

                if (mmkonto != "1" && mmkonto != "2" && mmkonto != "4") continue;
                if (existing.Contains(mmkonto2)) continue;

                kon2Map.TryGetValue(mmkonto2, out string? gnaz);
                var newRow = new NalplobrRow
                {
                    K2     = mmkonto2,
                    Dugpot = (mmkonto == "1" || mmkonto == "2") ? "D" : "P",
                    Gnaz   = gnaz ?? "NEMA GRUPE",
                };
                Rows.Add(newRow);
                existing.Add(mmkonto2);
            }

            if (Rows.Count > 0) SelectedRow = Rows[0];
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška: {ex.Message}", "Učitaj konta");
        }
    }

    // ── Prenos iz Glavne knjige (Command9) ────────────────────
    [RelayCommand]
    private void PrenosIzGlavneKnjige()
    {
        try
        {
            var nalPath     = Path.Combine(_firmPath, "nal.dbf");
            var nalpldatPath = Path.Combine(_firmPath, "nalpldat.dbf");
            if (!File.Exists(nalPath)) return;

            // read date range from nalpldat
            DateTime dat0 = new DateTime(DateTime.Today.Year, 1, 1);
            DateTime dat1 = new DateTime(DateTime.Today.Year, 12, 31);
            if (File.Exists(nalpldatPath))
            {
                foreach (var r in new SimpleDbfReader(nalpldatPath).Zapisi())
                {
                    dat0 = r.DajDate("DAT0") ?? dat0;
                    dat1 = r.DajDate("DAT1") ?? dat1;
                    break;
                }
            }

            // load all nal records
            var nalRecs = new SimpleDbfReader(nalPath).Zapisi().ToList();

            foreach (var row in Rows)
            {
                var k2 = row.K2.Trim();
                decimal dug = 0, pot = 0;
                foreach (var r in nalRecs)
                {
                    var konto = r.DajString("KONTO").Trim();
                    if (!konto.StartsWith(k2, StringComparison.OrdinalIgnoreCase)) continue;
                    var datdok = r.DajDate("DATDOK");
                    if (datdok == null || datdok < dat0 || datdok > dat1) continue;
                    dug += r.DajDecimal("DUG");
                    pot += r.DajDecimal("POT");
                }
                row.Dug    = dug;
                row.Pot    = pot;
                row.Saldo  = dug - pot;
                row.Dat0   = dat0;
                row.Dat1   = dat1;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška: {ex.Message}", "Prenos iz GK");
        }
    }

    // ── Brisanje tabele (ZAP + Release) ──────────────────────
    [RelayCommand]
    private void BrisanjeTabele()
    {
        if (Rows.Count == 0) return;
        Rows.Clear();
        SelectedRow = null;
        Snimi();
        ZatvoriFormu?.Invoke();
    }

    // ── Izlaz ─────────────────────────────────────────────────
    [RelayCommand]
    private void Izlaz()
    {
        Snimi();
        ZatvoriFormu?.Invoke();
    }

    // ── Snimanje ──────────────────────────────────────────────
    private void Snimi()
    {
        if (!File.Exists(_dbfPath)) return;
        try
        {
            var schema = DbfTableWriter.LoadSchema(_dbfPath);
            DbfTableWriter.WriteTable(_dbfPath, schema, Rows, FieldMapper);
        }
        catch { }
    }
}
