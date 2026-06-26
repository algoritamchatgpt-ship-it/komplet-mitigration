using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalraspViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly Dictionary<string, string> _kontoNaziv = [];
    private readonly Dictionary<string, string> _mtrNaziv   = [];

    public event Action? ZatvoriFormu;

    [ObservableProperty] private ObservableCollection<NalraspRow> _rows = [];
    [ObservableProperty] private NalraspRow?  _selectedRow;

    [ObservableProperty] private string _lblRec    = string.Empty;
    [ObservableProperty] private string _lblNalog  = string.Empty;
    [ObservableProperty] private string _lblKonto  = string.Empty;
    [ObservableProperty] private string _lblMtr    = string.Empty;

    public NalraspViewModel(string firmPath)
    {
        _firmPath = firmPath;
        UcitajLookup();
        UcitajRows();
    }

    private void UcitajLookup()
    {
        // konto10.dbf — for naziv
        var kp = Path.Combine(_firmPath, "konto10.dbf");
        if (File.Exists(kp))
        {
            try
            {
                var r = new SimpleDbfReader(kp);
                foreach (var rec in r.Zapisi())
                {
                    var k = rec.DajString("KONTO").Trim();
                    var n = rec.DajString("NAZIV").Trim();
                    if (!string.IsNullOrEmpty(k)) _kontoNaziv[k] = n;
                }
            }
            catch { }
        }
        // mtr.dbf — for naziv
        var mp = Path.Combine(_firmPath, "mtr.dbf");
        if (File.Exists(mp))
        {
            try
            {
                var r = new SimpleDbfReader(mp);
                foreach (var rec in r.Zapisi())
                {
                    var k = rec.DajString("MTR").Trim();
                    var n = rec.DajString("NAZIV").Trim();
                    if (!string.IsNullOrEmpty(k)) _mtrNaziv[k] = n;
                }
            }
            catch { }
        }
    }

    private void UcitajRows()
    {
        var path = Dbf("nalrasp.dbf");
        if (path == null) { Rows = []; return; }
        try
        {
            var r = new SimpleDbfReader(path);
            Rows = new ObservableCollection<NalraspRow>(r.Zapisi().Select(CitajRec));
            SelectedRow = Rows.Count > 0 ? Rows.Last() : null;
        }
        catch { Rows = []; }
    }

    partial void OnSelectedRowChanged(NalraspRow? value)
    {
        if (value == null) { LblRec = ""; LblNalog = ""; LblKonto = ""; LblMtr = ""; return; }
        var idx = Rows.IndexOf(value);
        LblRec   = $"{idx + 1,6}/{Rows.Count,6}";
        LblNalog = $"nalog za knjizenje {value.Brnal.Trim()}   datum {value.Datdok:dd.MM.yyyy}";
        var kn   = _kontoNaziv.TryGetValue(value.Konto.Trim(), out var knv) ? knv : string.Empty;
        LblKonto = $"{value.Konto.Trim()}  {kn}";
        var mn   = _mtrNaziv.TryGetValue(((int)value.Mtr).ToString(), out var mnv) ? mnv : string.Empty;
        LblMtr   = $"mesto troškova {(int)value.Mtr,5}  {mn}";
    }

    // ═══════════════════════════════════════════════════════
    // NAVIGACIJA
    // ═══════════════════════════════════════════════════════

    [RelayCommand] private void IdiDole()  { var i = Rows.IndexOf(SelectedRow!); if (i >= 0 && i < Rows.Count - 1) SelectedRow = Rows[i + 1]; }
    [RelayCommand] private void IdiGore()  { var i = Rows.IndexOf(SelectedRow!); if (i > 0) SelectedRow = Rows[i - 1]; }
    [RelayCommand] private void IdiNaDno() { if (Rows.Count > 0) SelectedRow = Rows.Last(); }
    [RelayCommand] private void IdiNaVrh() { if (Rows.Count > 0) SelectedRow = Rows[0]; }

    // ═══════════════════════════════════════════════════════
    // KOMANDE
    // ═══════════════════════════════════════════════════════

    [RelayCommand]
    private void Dodaj()
    {
        // NALRASPDODAJ: copy Opis/Brnal/Datdok/Konto from GO BOTTOM row (current last)
        var nova = new NalraspRow();
        if (Rows.Count > 0)
        {
            var last = Rows.Last();
            nova.Konto  = last.Konto;
            nova.Opis   = last.Opis;
            nova.Brnal  = last.Brnal;
            nova.Datdok = last.Datdok;
        }
        Rows.Add(nova);
        SelectedRow = nova;
        SnimiNalraspDbf();
    }

    [RelayCommand]
    private void BrisanjeReda()
    {
        if (SelectedRow == null) return;
        if (MessageBox.Show("Brisanje reda?", "Potvrda", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Rows.Remove(SelectedRow);
        SnimiNalraspDbf();
        ZatvoriFormu?.Invoke();
    }

    [RelayCommand]
    private void BrisanjeTabele()
    {
        if (MessageBox.Show("Brisanje čitave tabele?", "Potvrda", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Rows.Clear();
        SnimiNalraspDbf();
        ZatvoriFormu?.Invoke();
    }

    [RelayCommand]
    private void NapuniSvaKonta()
    {
        // NAPUNI SVA KONTA IZ GLAVNE KNJIGE
        // Read nal.dbf, filter KONTO[0]∈{'5','6'}, TOTAL ON KONTO → append to nalrasp
        var nalPath = Dbf("nal.dbf");
        if (nalPath == null) { MessageBox.Show("nal.dbf ne postoji."); return; }
        try
        {
            var r = new SimpleDbfReader(nalPath);
            var grouped = r.Zapisi()
                .Where(rec => { var k0 = rec.DajString("KONTO").FirstOrDefault(); return k0 == '5' || k0 == '6'; })
                .GroupBy(rec => rec.DajString("KONTO").Trim())
                .ToDictionary(g => g.Key, g => (
                    Dug: g.Sum(rec => rec.DajDecimal("DUG")),
                    Pot: g.Sum(rec => rec.DajDecimal("POT"))));

            foreach (var kvp in grouped)
            {
                var mkonto = kvp.Key;
                var k9 = mkonto.StartsWith("5")
                    ? "950" + (mkonto.Length >= 9 ? mkonto.Substring(2, 7) : mkonto.Substring(2).PadRight(7))
                    : "980" + (mkonto.Length >= 9 ? mkonto.Substring(2, 7) : mkonto.Substring(2).PadRight(7));
                var dp = mkonto.StartsWith("5") ? "D" : "P";
                Rows.Add(new NalraspRow
                {
                    Konto  = mkonto.PadRight(10),
                    K9     = k9.PadRight(10),
                    Ucesce = 100,
                    Dp     = dp,
                });
            }
            SnimiNalraspDbf();
            ZatvoriFormu?.Invoke();
        }
        catch (Exception ex) { MessageBox.Show($"Greška: {ex.Message}"); }
    }

    [RelayCommand]
    private void NapuniKonta5255()
    {
        // NAPUNI KONTA OD 52 DO 55 I 60 DO 62
        var nalPath = Dbf("nal.dbf");
        if (nalPath == null) { MessageBox.Show("nal.dbf ne postoji."); return; }
        try
        {
            var prefixes = new[] { "50","51","52","53","54","55","60","61","62" };
            var r = new SimpleDbfReader(nalPath);
            var grouped = r.Zapisi()
                .Where(rec => { var k = rec.DajString("KONTO").Trim(); return prefixes.Any(p => k.StartsWith(p)); })
                .GroupBy(rec => rec.DajString("KONTO").Trim())
                .ToDictionary(g => g.Key, g => (
                    Dug: g.Sum(rec => rec.DajDecimal("DUG")),
                    Pot: g.Sum(rec => rec.DajDecimal("POT"))));

            foreach (var kvp in grouped)
            {
                var mkonto = kvp.Key;
                var dp = mkonto.StartsWith("5") ? "D" : "P";
                Rows.Add(new NalraspRow
                {
                    Konto  = mkonto.PadRight(10),
                    K9     = (mkonto.StartsWith("5") ? "9500000000" : "9800000000"),
                    Ucesce = 100,
                    Dp     = dp,
                });
            }
            SnimiNalraspDbf();
            ZatvoriFormu?.Invoke();
        }
        catch (Exception ex) { MessageBox.Show($"Greška: {ex.Message}"); }
    }

    [RelayCommand]
    private void Preuzimanje()
    {
        // Opens NALRASPU dialog
        var win = new Views.NalraspuWindow(this);
        win.ShowDialog();
    }

    [RelayCommand]
    private void Knjizenje()
    {
        // NALRASPKNJIZI: copy K9/Datdok/Dug/Pot/Brnal from nalrasp → nalap.dbf
        var nalapPath = Dbf("nalap.dbf");
        if (nalapPath == null) { MessageBox.Show("nalap.dbf ne postoji."); return; }
        try
        {
            var schema   = DbfTableWriter.LoadSchema(nalapPath);
            var existing = new List<NalapMinRow>();
            var reader   = new SimpleDbfReader(nalapPath);
            foreach (var rec in reader.Zapisi())
                existing.Add(new NalapMinRow
                {
                    Konto  = rec.DajString("KONTO"),
                    Datdok = rec.DajDate("DATDOK"),
                    Dug    = rec.DajDecimal("DUG"),
                    Pot    = rec.DajDecimal("POT"),
                    Brnal  = rec.DajString("BRNAL"),
                    Opis   = rec.DajString("OPIS"),
                    Dp     = rec.DajString("DP"),
                });

            foreach (var row in Rows)
                existing.Add(new NalapMinRow
                {
                    Konto  = row.K9,
                    Datdok = row.Datdok,
                    Dug    = row.Dug,
                    Pot    = row.Pot,
                    Brnal  = row.Brnal,
                    Opis   = row.Opis,
                    Dp     = row.Dp,
                });

            DbfTableWriter.WriteTable(nalapPath, schema, existing, NalapMinRowFieldMapper);
            MessageBox.Show($"Proknjiženo {Rows.Count} redova u nalap.dbf.", "KNJIŽENJE",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show($"Greška: {ex.Message}"); }
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    // ═══════════════════════════════════════════════════════
    // NALRASPU (PREUZIMANJE) — called from NalraspuWindow
    // ═══════════════════════════════════════════════════════

    public void IzvrsiPreuzimanje(DateTime dat0, DateTime dat1, decimal mtr, string brnal)
    {
        var nalPath = Dbf("nal.dbf");
        if (nalPath == null) { MessageBox.Show("nal.dbf ne postoji."); return; }
        try
        {
            // build konto→(dug,pot) from nal.dbf filtered by date + mtr
            var nalReader = new SimpleDbfReader(nalPath);
            var kontoSums = new Dictionary<string, (decimal Dug, decimal Pot)>();
            foreach (var rec in nalReader.Zapisi())
            {
                var k    = rec.DajString("KONTO").Trim();
                var d    = rec.DajDate("DATDOK");
                var rmtr = rec.DajDecimal("MTR");
                if (d == null) continue;
                if (d < dat0 || d > dat1) continue;
                if (mtr != 0 && rmtr != mtr) continue;
                var dug  = rec.DajDecimal("DUG");
                var pot  = rec.DajDecimal("POT");
                if (kontoSums.TryGetValue(k, out var existing))
                    kontoSums[k] = (existing.Dug + dug, existing.Pot + pot);
                else
                    kontoSums[k] = (dug, pot);
            }

            // update nalrasp rows
            foreach (var row in Rows)
            {
                var mk = row.Konto.Trim();
                row.Datdok = dat1;
                row.Brnal  = brnal.PadRight(6);
                if (kontoSums.TryGetValue(mk, out var sums))
                {
                    if (row.Dp.Trim() == "D")
                        row.Dug = Math.Round((sums.Dug - sums.Pot) * row.Ucesce / 100, 2);
                    else
                        row.Pot = Math.Round((sums.Pot - sums.Dug) * row.Ucesce / 100, 2);
                }
                else
                {
                    row.Dug = 0;
                    row.Pot = 0;
                }
            }

            SnimiNalraspDbf();
            var cur = SelectedRow;
            SelectedRow = null;
            SelectedRow = cur;
        }
        catch (Exception ex) { MessageBox.Show($"Greška pri preuzimanju: {ex.Message}"); }
    }

    // ═══════════════════════════════════════════════════════
    // DBF I/O
    // ═══════════════════════════════════════════════════════

    public void SnimiNalraspDbf()
    {
        var path = Dbf("nalrasp.dbf");
        if (path == null) return;
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, Rows.ToList(), NalraspRowFieldMapper);
        }
        catch (Exception ex) { MessageBox.Show($"Greška pri snimanju: {ex.Message}"); }
    }

    private static NalraspRow CitajRec(DbfRecord rec) => new()
    {
        Konto   = rec.DajString("KONTO"),
        K9      = rec.DajString("K9"),
        Ucesce  = rec.DajDecimal("UCESCE"),
        Dp      = rec.DajString("DP"),
        Dug     = rec.DajDecimal("DUG"),
        Pot     = rec.DajDecimal("POT"),
        Opis    = rec.DajString("OPIS"),
        Mtr     = rec.DajDecimal("MTR"),
        Datdok  = rec.DajDate("DATDOK"),
        Brnal   = rec.DajString("BRNAL"),
        Dok     = rec.DajString("DOK"),
        K1      = rec.DajString("K1"),
        K2      = rec.DajString("K2"),
        K3      = rec.DajString("K3"),
        K4      = rec.DajString("K4"),
        K5      = rec.DajString("K5"),
        K6      = rec.DajString("K6"),
        Preneto = rec.DajString("PRENETO"),
        Idbr    = rec.DajDecimal("IDBR"),
    };

    private static object? NalraspRowFieldMapper(NalraspRow r, string f) => f.ToUpperInvariant() switch
    {
        "KONTO"   => r.Konto,
        "K9"      => r.K9,
        "UCESCE"  => r.Ucesce,
        "DP"      => r.Dp,
        "DUG"     => r.Dug,
        "POT"     => r.Pot,
        "OPIS"    => r.Opis,
        "MTR"     => r.Mtr,
        "DATDOK"  => r.Datdok,
        "BRNAL"   => r.Brnal,
        "DOK"     => r.Dok,
        "K1"      => r.K1,
        "K2"      => r.K2,
        "K3"      => r.K3,
        "K4"      => r.K4,
        "K5"      => r.K5,
        "K6"      => r.K6,
        "PRENETO" => r.Preneto,
        "IDBR"    => r.Idbr,
        _         => null,
    };

    private static object? NalapMinRowFieldMapper(NalapMinRow r, string f) => f.ToUpperInvariant() switch
    {
        "KONTO"  => r.Konto,
        "DATDOK" => r.Datdok,
        "DUG"    => r.Dug,
        "POT"    => r.Pot,
        "BRNAL"  => r.Brnal,
        "OPIS"   => r.Opis,
        "DP"     => r.Dp,
        _        => null,
    };

    private string? Dbf(string name)
    {
        var p = Path.Combine(_firmPath, name);
        return File.Exists(p) ? p : null;
    }

    private sealed class NalapMinRow
    {
        public string   Konto  { get; init; } = string.Empty;
        public DateTime? Datdok { get; init; }
        public decimal  Dug    { get; init; }
        public decimal  Pot    { get; init; }
        public string   Brnal  { get; init; } = string.Empty;
        public string   Opis   { get; init; } = string.Empty;
        public string   Dp     { get; init; } = string.Empty;
    }
}
