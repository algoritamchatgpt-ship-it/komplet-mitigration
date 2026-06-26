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
/// Transcripcija NALVRSTA.SCX — VRSTE NALOGA ZA KNJIŽENJE.
/// 11-col grid iz nalvrsta.dbf (VRNAL C3, NAZIV C30, DOK C3, MP C2, OBL C1,
/// PERIOD N2, NALDOK C1, ZNAKOVI N1, POCSIF C3, NAUTO C1, KONTO C10).
/// DODAJ: APPEND BLANK → otvori KARTICA. KARTICA F7: edit existing.
/// BRISANJE S+F10: briše selected. ŠABLONI F8: otvori NalvrstasSWindow.
/// DOPUNI KONTO: REPLACE ALL KONTO WITH ALLTRIM(KONTO)+'00'.
/// </summary>
public partial class NalvrstaViewModel : ObservableObject
{
    private readonly string _dbfPath;

    [ObservableProperty] private ObservableCollection<NalvrstaRow> _redovi = [];
    [ObservableProperty] private NalvrstaRow? _selectedRow;
    [ObservableProperty] private string _labelInfo = string.Empty;

    public event Action? ZatvoriFormu;
    public event Action<NalvrstaRow>? DodatRed;

    // Ako je otvoren kao picker — vraća izabrani VRNAL
    public bool JePiker { get; set; }
    public string? IzabraniVrnal { get; private set; }

    public NalvrstaViewModel(string dbfPath)
    {
        _dbfPath = dbfPath;
        Ucitaj();
    }

    private void Ucitaj()
    {
        if (!File.Exists(_dbfPath)) return;
        try
        {
            var r = new SimpleDbfReader(_dbfPath);
            Redovi = new ObservableCollection<NalvrstaRow>(
                r.Zapisi().Select(rec => new NalvrstaRow
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
                }));
            if (Redovi.Count > 0) SelectedRow = Redovi[0];
        }
        catch (Exception ex) { MessageBox.Show($"Greška pri čitanju nalvrsta.dbf: {ex.Message}"); }
        AzuriraLabel();
    }

    partial void OnSelectedRowChanged(NalvrstaRow? value) => AzuriraLabel();

    private void AzuriraLabel()
    {
        var r = SelectedRow;
        LabelInfo = r == null ? string.Empty : $"{r.Vrnal.Trim()} {r.Naziv.Trim()}";
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

    // ── DODAJ (NALVRSTADOD) — APPEND BLANK, otvori KARTICA ──────────
    [RelayCommand]
    private void Dodaj()
    {
        var nov = new NalvrstaRow();
        Redovi.Add(nov);
        SelectedRow = nov;
        DodatRed?.Invoke(nov);

        var vm  = new NalvrstaKViewModel(nov);
        var win = new NalvrstaKWindow(vm);
        if (win.ShowDialog() == true)
            SnimiNalvrsta();
        else
            Redovi.Remove(nov);
    }

    // ── KARTICA F7 (NALVRSTAK) — edit existing ───────────────────────
    [RelayCommand]
    private void KarticaF7()
    {
        if (SelectedRow == null) return;
        var vm  = new NalvrstaKViewModel(SelectedRow);
        var win = new NalvrstaKWindow(vm);
        if (win.ShowDialog() == true)
            SnimiNalvrsta();
        AzuriraLabel();
    }

    // Enter/double-click u picker modu
    public void IzaberiSelektovani()
    {
        if (SelectedRow == null) return;
        IzabraniVrnal = SelectedRow.Vrnal.Trim();
        ZatvoriFormu?.Invoke();
    }

    // ── BRISANJE S+F10 (NALVRSTABRISI) ──────────────────────────────
    [RelayCommand]
    private void BrisanjeF10()
    {
        if (SelectedRow == null) return;
        if (MessageBox.Show(
            $"Brisanje vrste naloga '{SelectedRow.Vrnal.Trim()}  {SelectedRow.Naziv.Trim()}'?",
            "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        Redovi.Remove(SelectedRow);
        SelectedRow = Redovi.Count > 0 ? Redovi[0] : null;
        SnimiNalvrsta();
        AzuriraLabel();
    }

    // ── ŠABLONI F8 (NALVRSTAS) ───────────────────────────────────────
    [RelayCommand]
    private void SabloniF8()
    {
        var win = new NalvrstasSWindow(_dbfPath, Redovi);
        win.ShowDialog();
        // NalvrstasSWindow snima direktno; osvježi prikaz
        Ucitaj();
    }

    // ── DOPUNI KONTO ─────────────────────────────────────────────────
    // GO TOP; DO WHILE .NOT. EOF(); IF LEN(ALLTRIM(KONTO))>0 → KONTO=KONTO+'00'; SKIP; ENDDO
    [RelayCommand]
    private void DopuniKonto()
    {
        foreach (var r in Redovi)
        {
            var k = r.Konto.Trim();
            if (k.Length > 0) r.Konto = k + "00";
        }
        SnimiNalvrsta();
    }

    // ── IZLAZ ────────────────────────────────────────────────────────
    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    // ── SNIMANJE nalvrsta.dbf ────────────────────────────────────────
    private void SnimiNalvrsta()
    {
        if (!File.Exists(_dbfPath)) return;
        try
        {
            var schema = DbfTableWriter.LoadSchema(_dbfPath);
            DbfTableWriter.WriteTable(_dbfPath, schema, Redovi.ToList(), (r, f) => f switch
            {
                "VRNAL"   => (object)r.Vrnal,
                "NAZIV"   => r.Naziv,
                "DOK"     => r.Dok,
                "MP"      => r.Mp,
                "OBL"     => r.Obl,
                "PERIOD"  => r.Period,
                "NALDOK"  => r.Naldok,
                "ZNAKOVI" => r.Znakovi,
                "POCSIF"  => r.Pocsif,
                "NAUTO"   => r.Nauto,
                "KONTO"   => r.Konto,
                "PRENETO" => r.Preneto,
                "IDBR"    => r.Idbr,
                _         => null
            });
        }
        catch (Exception ex) { MessageBox.Show($"Greška pri snimanju nalvrsta.dbf: {ex.Message}"); }
    }
}
