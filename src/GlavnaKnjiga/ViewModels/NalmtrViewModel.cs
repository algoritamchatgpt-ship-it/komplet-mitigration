using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using GlavnaKnjiga.Views;
using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalmtrViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly string _dbfPath;
    private readonly string _mtrKPath;
    private readonly int _godina;

    public ObservableCollection<NalmtrRow> Redovi { get; } = new();

    [ObservableProperty] private NalmtrRow? _selectedRow;

    // dynamic column headers for Iznos01-30 (loaded from nalmtrk.dbf)
    public IReadOnlyList<string> KoloneNazivi { get; private set; } = Enumerable.Range(1, 30).Select(i => $"Iznos{i:D2}").ToList();

    public event Action? ZatvoriFormu;

    public NalmtrViewModel(string firmPath, int godina = 0)
    {
        _firmPath = firmPath;
        _godina = godina > 0 ? godina : DateTime.Today.Year;
        _dbfPath  = Path.Combine(firmPath, "nalmtr.dbf");
        _mtrKPath = Path.Combine(firmPath, "nalmtrk.dbf");
        UcitajKolone();
        Ucitaj();
    }

    private void UcitajKolone()
    {
        if (!File.Exists(_mtrKPath)) return;
        try
        {
            var kolone = new List<string>(30);
            var r = new SimpleDbfReader(_mtrKPath);
            foreach (var rec in r.Zapisi())
            {
                var nazStr = $"{rec.DajString("KONTOTR").Trim()} {rec.DajString("NAZIV").Trim()}".Trim();
                kolone.Add(nazStr);
                if (kolone.Count == 30) break;
            }
            while (kolone.Count < 30) kolone.Add($"Iznos{(kolone.Count + 1):D2}");
            KoloneNazivi = kolone;
        }
        catch { }
    }

    private void Ucitaj()
    {
        Redovi.Clear();
        if (!File.Exists(_dbfPath)) return;
        try
        {
            var r = new SimpleDbfReader(_dbfPath);
            foreach (var rec in r.Zapisi())
                Redovi.Add(NalmtrPrenosViewModel.CitajNalmtrRec(rec));
        }
        catch (Exception ex) { MessageBox.Show($"Greška pri čitanju nalmtr.dbf: {ex.Message}"); }
    }

    // ── NAVIGACIJA ─────────────────────────────────────────────────────
    [RelayCommand]
    private void IdiGore()
    {
        if (SelectedRow == null && Redovi.Count > 0) { SelectedRow = Redovi[0]; return; }
        var idx = SelectedRow != null ? Redovi.IndexOf(SelectedRow) : -1;
        if (idx > 0) SelectedRow = Redovi[idx - 1];
    }

    [RelayCommand]
    private void IdiDole()
    {
        var idx = SelectedRow != null ? Redovi.IndexOf(SelectedRow) : -1;
        if (idx < Redovi.Count - 1) SelectedRow = Redovi[idx + 1];
    }

    [RelayCommand]
    private void IdiNaVrh() { if (Redovi.Count > 0) SelectedRow = Redovi[0]; }

    [RelayCommand]
    private void IdiNaDno() { if (Redovi.Count > 0) SelectedRow = Redovi[^1]; }

    // ── TRAŽENJE ────────────────────────────────────────────────────────
    [RelayCommand]
    private void TraziKontoF5()
    {
        var unos = Interaction.InputBox("Konto:", "TRAŽENJE KONTA", "").Trim();
        if (string.IsNullOrEmpty(unos)) return;
        var nadjeni = Redovi.FirstOrDefault(r => r.Konto.Trim().StartsWith(unos, StringComparison.OrdinalIgnoreCase));
        if (nadjeni != null) SelectedRow = nadjeni;
    }

    [RelayCommand]
    private void TraziNalogF9()
    {
        var unos = Interaction.InputBox("Nalog (BRNAL):", "TRAŽENJE NALOGA", "").Trim().PadLeft(6);
        if (string.IsNullOrEmpty(unos.Trim())) return;
        var nadjeni = Redovi.FirstOrDefault(r => r.Brnal.Trim() == unos.Trim());
        if (nadjeni != null) SelectedRow = nadjeni;
    }

    [RelayCommand]
    private void TraziDatumF8()
    {
        var unos = Interaction.InputBox("Datum (dd.MM.yyyy):", "TRAŽENJE DATUMA", "").Trim();
        if (string.IsNullOrEmpty(unos)) return;
        if (!DateTime.TryParse(unos, out var dat)) return;
        var nadjeni = Redovi.FirstOrDefault(r => r.Datdok?.Date == dat.Date);
        if (nadjeni != null) SelectedRow = nadjeni;
    }

    // ── PRENOS KNJIŽENJA ────────────────────────────────────────────────
    [RelayCommand]
    private void PrenosKnjizenja()
    {
        var vm  = new NalmtrPrenosViewModel(_firmPath);
        var win = new NalmtrPrenosWindow(vm);
        vm.ZatvoriFormu += win.Close;
        win.ShowDialog();
        UcitajKolone();
        Ucitaj();
        OnPropertyChanged(nameof(KoloneNazivi));
    }

    // ── BRISANJE TABELE ─────────────────────────────────────────────────
    [RelayCommand]
    private void BrisanjeTabele()
    {
        if (MessageBox.Show("Brisanje cele tabele NALMTR?", "BRISANJE TABELE",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        if (!File.Exists(_dbfPath)) return;
        try
        {
            var schema = DbfTableWriter.LoadSchema(_dbfPath);
            DbfTableWriter.WriteTable(_dbfPath, schema, new List<NalmtrRow>(), NalmtrPrenosViewModel.NalmtrRowFieldMapper);
            Redovi.Clear();
        }
        catch (Exception ex) { MessageBox.Show($"Greška: {ex.Message}"); }
    }

    // ── KONTA MESTA TROŠKOVA ────────────────────────────────────────────
    [RelayCommand]
    private void KontaMtr()
    {
        var vm  = new NalmtrKViewModel(_firmPath);
        var win = new NalmtrKWindow(vm);
        vm.ZatvoriFormu += win.Close;
        win.ShowDialog();
        UcitajKolone();
        OnPropertyChanged(nameof(KoloneNazivi));
        // caller (NalmtrWindow) must re-apply column headers
        KoloneAzuriraneEvent?.Invoke();
    }

    public event Action? KoloneAzuriraneEvent;

    // ── PREGLED ZA PERIOD ───────────────────────────────────────────────
    [RelayCommand]
    private void PregledZaPeriod()
    {
        var vm = new NalmtrPregledViewModel(_firmPath, _godina, KoloneNazivi);
        new NalmtrPregledWindow(vm).ShowDialog();
    }

    // ── BRISANJE NALOGA ─────────────────────────────────────────────────
    [RelayCommand]
    private void BrisanjeNaloga()
    {
        if (SelectedRow == null) return;
        var mbrnal = SelectedRow.Brnal.Trim();
        if (MessageBox.Show($"Brisanje naloga {mbrnal}?", "BRISANJE NALOGA",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        var brisi = Redovi.Where(r => r.Brnal.Trim() == mbrnal).ToList();
        foreach (var r in brisi) Redovi.Remove(r);
        SnimiNalmtr();
    }

    // ── SNIMANJE ────────────────────────────────────────────────────────
    internal void SnimiNalmtr()
    {
        if (!File.Exists(_dbfPath)) return;
        try
        {
            var schema = DbfTableWriter.LoadSchema(_dbfPath);
            DbfTableWriter.WriteTable(_dbfPath, schema, Redovi.ToList(), NalmtrPrenosViewModel.NalmtrRowFieldMapper);
        }
        catch (Exception ex) { MessageBox.Show($"Greška pri snimanju nalmtr.dbf: {ex.Message}"); }
    }

    [RelayCommand]
    private void Izlaz()
    {
        SnimiNalmtr();
        ZatvoriFormu?.Invoke();
    }
}
