using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class Nalgk10ViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly Dictionary<string, string> _kontoNaziv = [];

    public event Action? ZatvoriFormu;

    [ObservableProperty] private ObservableCollection<Nalgk10Row> _rows = [];
    [ObservableProperty] private Nalgk10Row?  _selectedRow;

    [ObservableProperty] private string _lblRec      = string.Empty;
    [ObservableProperty] private string _lblBrnal    = string.Empty;
    [ObservableProperty] private string _lblDatdok   = string.Empty;
    [ObservableProperty] private string _kontoInfo   = string.Empty;
    [ObservableProperty] private string _nazIvInfo   = string.Empty;
    [ObservableProperty] private string _txtDug      = string.Empty;
    [ObservableProperty] private string _txtPot      = string.Empty;
    [ObservableProperty] private string _txtSaldo    = string.Empty;
    [ObservableProperty] private bool   _isArchived;

    public Nalgk10ViewModel(string firmPath)
    {
        _firmPath = firmPath;
        UcitajKonto10();
        UcitajRows();
    }

    private void UcitajKonto10()
    {
        var path = Path.Combine(_firmPath, "konto10.dbf");
        if (!File.Exists(path)) return;
        try
        {
            var r = new SimpleDbfReader(path);
            foreach (var rec in r.Zapisi())
            {
                var k = rec.DajString("KONTO").Trim();
                var n = rec.DajString("NAZIV").Trim();
                if (!string.IsNullOrEmpty(k))
                    _kontoNaziv[k] = n;
            }
        }
        catch { }
    }

    private void UcitajRows()
    {
        var path = Path.Combine(_firmPath, "nalgk10.dbf");
        if (!File.Exists(path)) { Rows = []; return; }
        try
        {
            var r = new SimpleDbfReader(path);
            Rows = new ObservableCollection<Nalgk10Row>(
                r.Zapisi().Select(CitajRec));
            if (Rows.Count > 0) SelectedRow = Rows[0];
        }
        catch { Rows = []; }
    }

    partial void OnSelectedRowChanged(Nalgk10Row? value)
    {
        if (value == null) { LblRec = ""; LblBrnal = ""; LblDatdok = ""; return; }

        var idx = Rows.IndexOf(value);
        LblRec    = $"{idx + 1,6}/{Rows.Count,6}";
        LblBrnal  = value.Brnal.Trim();
        LblDatdok = value.Datdok?.ToString("dd.MM.yyyy") ?? "";

        var k = value.Konto.Trim();
        KontoInfo = k;
        NazIvInfo = _kontoNaziv.TryGetValue(k, out var n) ? n : string.Empty;

        IsArchived = value.Arhiva.Trim() != string.Empty;

        if (!IsArchived) AzurirajTotale(value.Brnal.Trim());
        else { TxtDug = ""; TxtPot = ""; TxtSaldo = ""; }
    }

    private void AzurirajTotale(string brnal)
    {
        var mdug = Rows.Where(r => r.Brnal.Trim() == brnal).Sum(r => r.Dug);
        var mpot = Rows.Where(r => r.Brnal.Trim() == brnal).Sum(r => r.Pot);
        TxtDug   = mdug.ToString("N2");
        TxtPot   = mpot.ToString("N2");
        TxtSaldo = (mdug - mpot).ToString("N2");
    }

    // ═══════════════════════════════════════════════════════
    // NAVIGACIJA
    // ═══════════════════════════════════════════════════════

    [RelayCommand]
    private void IdiDole()
    {
        if (SelectedRow == null && Rows.Count > 0) { SelectedRow = Rows[0]; return; }
        var idx = Rows.IndexOf(SelectedRow!);
        if (idx >= 0 && idx < Rows.Count - 1) SelectedRow = Rows[idx + 1];
    }

    [RelayCommand]
    private void IdiGore()
    {
        var idx = Rows.IndexOf(SelectedRow!);
        if (idx > 0) SelectedRow = Rows[idx - 1];
    }

    [RelayCommand]
    private void IdiNaDno()  { if (Rows.Count > 0) SelectedRow = Rows.Last(); }

    [RelayCommand]
    private void IdiNaVrh()  { if (Rows.Count > 0) SelectedRow = Rows[0]; }

    // ═══════════════════════════════════════════════════════
    // KOMANDE
    // ═══════════════════════════════════════════════════════

    [RelayCommand]
    private void Dodaj()
    {
        // NALGK10DODAJ: copy Opis/Brnal/Datdok/Konto from GO BOTTOM row
        var nova = new Nalgk10Row { Datum = DateTime.Today, Vreme = DateTime.Now.ToString("HH:mm:ss") };
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
        SnimiNalgk10Dbf();
    }

    [RelayCommand]
    private void KnjiziF5()
    {
        // NALGK10KNJIZI: toggle ARHIVA ' '<->'*' for all rows of current BRNAL
        if (SelectedRow == null) return;
        var brnal = SelectedRow.Brnal.Trim();
        var toToggle = Rows.Where(r => r.Brnal.Trim() == brnal).ToList();
        if (toToggle.Count == 0) return;
        var newArh = toToggle[0].Arhiva.Trim() == string.Empty ? "*" : " ";
        foreach (var r in toToggle)
            r.Arhiva = newArh;
        SnimiNalgk10Dbf();
        // refresh selection to update IsArchived
        var cur = SelectedRow;
        SelectedRow = null;
        SelectedRow = cur;
    }

    [RelayCommand]
    private void PrazniNalog()
    {
        // NALGK10PRAZNI: set DUG=0, POT=0 for current BRNAL only if ARHIVA=' '
        if (SelectedRow == null) return;
        if (SelectedRow.Arhiva.Trim() != string.Empty) return;
        var brnal = SelectedRow.Brnal.Trim();
        foreach (var r in Rows.Where(r => r.Brnal.Trim() == brnal))
        {
            r.Dug = 0;
            r.Pot = 0;
        }
        SnimiNalgk10Dbf();
        AzurirajTotale(brnal);
    }

    [RelayCommand]
    private void BrisanjeNaloga()
    {
        // Command20: delete rows for current BRNAL (if ARHIVA=' '), then remove DUG=POT=0 rows
        if (SelectedRow == null) return;
        var brnal = SelectedRow.Brnal.Trim();
        if (SelectedRow.Arhiva.Trim() == string.Empty)
        {
            var toDelete = Rows.Where(r => r.Brnal.Trim() == brnal).ToList();
            foreach (var r in toDelete) Rows.Remove(r);
        }
        // PACK: remove DUG=0 AND POT=0 rows
        var prazni = Rows.Where(r => r.Dug == 0 && r.Pot == 0).ToList();
        foreach (var r in prazni) Rows.Remove(r);
        SnimiNalgk10Dbf();
        ZatvoriFormu?.Invoke();
    }

    [RelayCommand]
    private void TraziNalogF9()
    {
        var input = Interaction.InputBox("Unesite broj naloga:", "TRAŽENJE NALOGA", "").Trim();
        if (string.IsNullOrEmpty(input)) return;
        var found = Rows.FirstOrDefault(r => r.Brnal.Trim().Equals(input, StringComparison.OrdinalIgnoreCase));
        if (found != null) SelectedRow = found;
        else MessageBox.Show($"Nalog '{input}' nije pronađen.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void ZakljucniList()
    {
        var win = new Views.Nalzaklj10Window();
        win.ShowDialog();
    }

    [RelayCommand]
    private void KontniPlan()
    {
        var vm = new KontoPlanViewModel(_firmPath, "konto10.dbf", "KONTNI PLAN — 10 CIFARA");
        new Views.KontoPlanWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void NalogF10()
    {
        if (SelectedRow == null) return;
        var brnal = SelectedRow.Brnal.Trim();
        OtvoriPregled($"PREGLED NALOGA {brnal}",
            Rows.Where(r => r.Brnal.Trim().Equals(
                brnal, StringComparison.OrdinalIgnoreCase)));
    }

    [RelayCommand]
    private void TraziKontoF7()
    {
        var input = Interaction.InputBox("Unesite konto za pretragu:", "TRAŽENJE KONTA", "").Trim();
        if (string.IsNullOrEmpty(input)) return;
        var found = Rows.FirstOrDefault(r => r.Konto.Trim().StartsWith(input, StringComparison.OrdinalIgnoreCase));
        if (found != null) SelectedRow = found;
        else MessageBox.Show($"Konto '{input}' nije pronađen.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void KontoF6()
    {
        if (SelectedRow == null) return;
        var konto = SelectedRow.Konto.Trim();
        OtvoriPregled($"PREGLED KONTA {konto}",
            Rows.Where(r => r.Konto.Trim().Equals(
                konto, StringComparison.OrdinalIgnoreCase)));
    }

    private static void OtvoriPregled(
        string naslov, IEnumerable<Nalgk10Row> rows)
    {
        var vm = new NalogPregledViewModel(
            naslov, rows.Select(NalogPregledViewModel.IzNalgk10));
        new Views.NalogPregledWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void Izlaz()
    {
        SnimiNalgk10Dbf();
        ZatvoriFormu?.Invoke();
    }

    // ═══════════════════════════════════════════════════════
    // DBF I/O
    // ═══════════════════════════════════════════════════════

    public void SnimiNalgk10Dbf()
    {
        var path = Path.Combine(_firmPath, "nalgk10.dbf");
        if (!File.Exists(path)) return;
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, Rows.ToList(), Nalgk10RowFieldMapper);
        }
        catch (Exception ex) { MessageBox.Show($"Greška pri snimanju: {ex.Message}"); }
    }

    private static Nalgk10Row CitajRec(DbfRecord rec) => new()
    {
        Konto   = rec.DajString("KONTO"),
        Dug     = rec.DajDecimal("DUG"),
        Pot     = rec.DajDecimal("POT"),
        Opis    = rec.DajString("OPIS"),
        Datdok  = rec.DajDate("DATDOK"),
        Brnal   = rec.DajString("BRNAL"),
        Arhiva  = rec.DajString("ARHIVA"),
        Datum   = rec.DajDate("DATUM"),
        Vreme   = rec.DajString("VREME"),
        Preneto = rec.DajString("PRENETO"),
        Idbr    = rec.DajDecimal("IDBR"),
    };

    private static object? Nalgk10RowFieldMapper(Nalgk10Row r, string f) => f.ToUpperInvariant() switch
    {
        "KONTO"   => r.Konto,
        "DUG"     => r.Dug,
        "POT"     => r.Pot,
        "OPIS"    => r.Opis,
        "DATDOK"  => r.Datdok,
        "BRNAL"   => r.Brnal,
        "ARHIVA"  => r.Arhiva,
        "DATUM"   => r.Datum,
        "VREME"   => r.Vreme,
        "PRENETO" => r.Preneto,
        "IDBR"    => r.Idbr,
        _         => null,
    };
}
