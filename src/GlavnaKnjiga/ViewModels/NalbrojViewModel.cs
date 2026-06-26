using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using GlavnaKnjiga.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>
/// Transcripcija NALBROJ.SCX — EVIDENCIJA NALOGA.
/// NALB='99' branch (standalone): čita nalbroj.dbf, prikazuje u gridu (read-only).
/// Buttons: PRVI/GORE/DOLE/ZADNJI + DODAJ+/TRAŽENJE F6/KARTICA F7/BRISANJE/IZLAZ.
/// </summary>
public partial class NalbrojViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly string _dbfPath;
    private readonly string _nalvrstePath;

    [ObservableProperty] private ObservableCollection<NalbrojRow> _redovi = [];
    [ObservableProperty] private NalbrojRow? _selectedRow;
    [ObservableProperty] private string _labelInfo = string.Empty;

    public event Action? ZatvoriFormu;

    public NalbrojViewModel(string firmPath)
    {
        _firmPath     = firmPath;
        _dbfPath      = Path.Combine(firmPath, "nalbroj.dbf");
        _nalvrstePath = Path.Combine(firmPath, "nalvrsta.dbf");
        Ucitaj();
    }

    // ── UČITAVANJE ──────────────────────────────────────────────────
    private void Ucitaj()
    {
        if (!File.Exists(_dbfPath)) return;
        try
        {
            var r = new SimpleDbfReader(_dbfPath);
            Redovi = new ObservableCollection<NalbrojRow>(
                r.Zapisi().Select(rec => new NalbrojRow
                {
                    Brnal   = rec.DajString("BRNAL"),
                    Datum   = rec.DajDate("DATUM"),
                    Vrnal   = rec.DajString("VRNAL"),
                    Opis    = rec.DajString("OPIS"),
                    Datod   = rec.DajDate("DATOD"),
                    Datdo   = rec.DajDate("DATDO"),
                    Dug     = rec.DajDecimal("DUG"),
                    Pot     = rec.DajDecimal("POT"),
                    Datknji = rec.DajDate("DATKNJI"),
                    Oper    = rec.DajString("OPER"),
                    Preneto = rec.DajString("PRENETO"),
                    Idbr    = rec.DajDecimal("IDBR"),
                }));
            if (Redovi.Count > 0) SelectedRow = Redovi[0];
        }
        catch (Exception ex) { MessageBox.Show($"Greška pri čitanju nalbroj.dbf: {ex.Message}"); }
        AzuriraLabel();
    }

    partial void OnSelectedRowChanged(NalbrojRow? value) => AzuriraLabel();

    private void AzuriraLabel()
    {
        var r = SelectedRow;
        LabelInfo = r == null
            ? string.Empty
            : $" NALOG ZA KNJIŽENJE {r.Brnal.Trim()} {r.Opis.Trim()}";
    }

    // ── NAVIGACIJA ───────────────────────────────────────────────────
    [RelayCommand]
    private void IdiGore()
    {
        if (SelectedRow == null || Redovi.Count == 0) return;
        var idx = Redovi.IndexOf(SelectedRow);
        if (idx > 0) SelectedRow = Redovi[idx - 1];
    }

    [RelayCommand]
    private void IdiDole()
    {
        if (SelectedRow == null || Redovi.Count == 0) return;
        var idx = Redovi.IndexOf(SelectedRow);
        if (idx < Redovi.Count - 1) SelectedRow = Redovi[idx + 1];
    }

    [RelayCommand]
    private void IdiNaVrh()
    {
        if (Redovi.Count > 0) SelectedRow = Redovi[0];
    }

    [RelayCommand]
    private void IdiNaDno()
    {
        if (Redovi.Count > 0) SelectedRow = Redovi[^1];
    }

    // ── DODAJ + (NALBROJK — ODREDJIVANJE NALOGA) ───────────────────
    [RelayCommand]
    private void Dodaj()
    {
        var nalvrste = UcitajNalvrste();
        var novRed   = new NalbrojRow();
        Redovi.Add(novRed);
        SelectedRow = novRed;

        var vm  = new NalbrojKViewModel(_firmPath, Redovi, novRed, nalvrste);
        var win = new NalbrojKWindow(vm);
        if (win.ShowDialog() != true)
        {
            Redovi.Remove(novRed);
            if (Redovi.Count > 0) SelectedRow = Redovi[^1];
        }
        else
        {
            SnimiNalbroj();
            SelectedRow = novRed;
        }
        AzuriraLabel();
    }

    // ── KARTICA F7 (NALBROJK2 — edit existing) ─────────────────────
    [RelayCommand]
    private void KarticaF7()
    {
        if (SelectedRow == null) return;
        var nalvrste = UcitajNalvrste();
        var vm  = new NalbrojK2ViewModel(SelectedRow, Redovi, nalvrste);
        var win = new NalbrojK2Window(vm);
        if (win.ShowDialog() == true)
            SnimiNalbroj();
        AzuriraLabel();
    }

    // ── TRAŽENJE F6 ──────────────────────────────────────────────────
    [RelayCommand]
    private void TraziF6()
    {
        var unos = Microsoft.VisualBasic.Interaction.InputBox(
            "Unesite broj naloga (BRNAL):", "Traženje", string.Empty);
        if (string.IsNullOrWhiteSpace(unos)) return;

        if (int.TryParse(unos.Trim(), out var n))
            unos = n.ToString().PadLeft(6);
        else
            unos = unos.PadLeft(6);

        var nadjeni = Redovi.FirstOrDefault(r => r.Brnal == unos);
        if (nadjeni != null)
            SelectedRow = nadjeni;
    }

    // ── BRISANJE (NALBROJBRISI) ──────────────────────────────────────
    [RelayCommand]
    private void Brisanje()
    {
        var win = new NalbrojBrisiWindow(_firmPath);
        win.ShowDialog();
        Ucitaj();
    }

    // ── IZLAZ ────────────────────────────────────────────────────────
    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    // ── SNIMANJE nalbroj.dbf ─────────────────────────────────────────
    internal void SnimiNalbroj()
    {
        if (!File.Exists(_dbfPath)) return;
        try
        {
            var schema = DbfTableWriter.LoadSchema(_dbfPath);
            DbfTableWriter.WriteTable(_dbfPath, schema, Redovi.ToList(), NalbrojFieldMapper);
        }
        catch (Exception ex) { MessageBox.Show($"Greška pri snimanju nalbroj.dbf: {ex.Message}"); }
    }

    private static object? NalbrojFieldMapper(NalbrojRow r, string field) => field switch
    {
        "BRNAL"   => r.Brnal,
        "DATUM"   => r.Datum,
        "VRNAL"   => r.Vrnal,
        "OPIS"    => r.Opis,
        "DATOD"   => r.Datod,
        "DATDO"   => r.Datdo,
        "DUG"     => r.Dug,
        "POT"     => r.Pot,
        "DATKNJI" => r.Datknji,
        "OPER"    => r.Oper,
        "PRENETO" => r.Preneto,
        "IDBR"    => r.Idbr,
        _         => null
    };

    // ── POMOĆNA — učitava nalvrsta.dbf u dict ──────────────────────
    internal Dictionary<string, NalvrstaRow> UcitajNalvrste()
    {
        var dict = new Dictionary<string, NalvrstaRow>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(_nalvrstePath)) return dict;
        try
        {
            var r = new SimpleDbfReader(_nalvrstePath);
            foreach (var rec in r.Zapisi())
            {
                var row = new NalvrstaRow
                {
                    Vrnal   = rec.DajString("VRNAL"),
                    Naziv   = rec.DajString("NAZIV"),
                    Dok     = rec.DajString("DOK"),
                    Mp      = rec.DajString("MP"),
                    Obl     = rec.DajString("OBL"),
                    Period  = rec.DajDecimal("PERIOD"),
                    Naldok  = rec.DajString("NALDOK"),
                    Znakovi = rec.DajDecimal("ZNAKOVI"),
                    Pocsif  = rec.DajString("POCSIF"),
                    Nauto   = rec.DajString("NAUTO"),
                    Konto   = rec.DajString("KONTO"),
                    Preneto = rec.DajString("PRENETO"),
                    Idbr    = rec.DajDecimal("IDBR"),
                };
                dict[row.Vrnal.Trim()] = row;
            }
        }
        catch { }
        return dict;
    }
}
