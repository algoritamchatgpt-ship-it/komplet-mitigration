using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>
/// Transcripcija NALPDEFK.SCX — CRUD grid za nalpdefk.dbf.
/// Caption = 'DEFINICIJA KONTA ANALITIKE' + operater + firma.
/// Ako je tabela prazna, puni je iz AAAN/AATV/AATM kao originalni NALPPUNIDEFK.
/// </summary>
public partial class NalpDefkViewModel : ObservableObject
{
    internal sealed record IzvorniRed(
        int RedniBroj,
        decimal Sifprod,
        string Pnaziv,
        string Prikaz,
        string Konto,
        string Sifarnik,
        string Dp = "",
        string Devizno = "");

    private readonly string _dbfPath;
    private readonly string _firmPath;

    public string Caption { get; }

    [ObservableProperty] private ObservableCollection<NalpDefkRow> _redovi = [];
    [ObservableProperty] private NalpDefkRow? _selectedRow;

    public event Action? ZatvoriFormu;
    public event Action<NalpDefkRow>? DodatRed;

    public NalpDefkViewModel(string firmPath, string korisnik, string firma)
    {
        _firmPath = firmPath;
        _dbfPath = Path.Combine(firmPath, "nalpdefk.dbf");
        Caption  = $" DEFINICIJA KONTA ANALITIKE  {korisnik}  {firma}";
        Ucitaj();
    }

    private void Ucitaj()
    {
        if (!File.Exists(_dbfPath)) return;
        try
        {
            var r = new SimpleDbfReader(_dbfPath);
            var ucitani = new ObservableCollection<NalpDefkRow>(
                r.Zapisi().Select(rec => new NalpDefkRow
                {
                    Sifprod   = rec.DajDecimal("SIFPROD"),
                    Konto     = rec.DajString("KONTO"),
                    Pnaziv    = rec.DajString("PNAZIV"),
                    Devizno   = rec.DajString("DEVIZNO"),
                    Sifarnik  = rec.DajString("SIFARNIK"),
                    Dok       = rec.DajString("DOK"),
                    Vrsta     = rec.DajString("VRSTA"),
                    Imetabele = rec.DajString("IMETABELE"),
                    Dp        = rec.DajString("DP"),
                    Preneto   = rec.DajString("PRENETO"),
                    Numred    = rec.DajDecimal("NUMRED"),
                    Idbr      = rec.DajDecimal("IDBR"),
                }));
            if (ucitani.Count == 0)
            {
                Redovi = new ObservableCollection<NalpDefkRow>(FormirajPocetneRedove(
                    UcitajIzvor("aaan.dbf", "KONTO", ukljuciDpIDevizno: true),
                    UcitajIzvor("aatv.dbf", "KONTOPAZ"),
                    UcitajIzvor("aatm.dbf", "KONTOPAZ")));
                SnimiNalpDefk();
            }
            else
            {
                Redovi = ucitani;
            }

            if (Redovi.Count > 0) SelectedRow = Redovi[0];
        }
        catch (Exception ex) { MessageBox.Show($"Greška pri čitanju nalpdefk.dbf: {ex.Message}"); }
    }

    // ── DODAJ + — APPEND BLANK → idi na kraj → fokus ─────────────
    private IReadOnlyList<IzvorniRed> UcitajIzvor(
        string imeFajla,
        string poljeKonta,
        bool ukljuciDpIDevizno = false)
    {
        var path = Path.Combine(_firmPath, imeFajla);
        if (!File.Exists(path))
            return [];

        var reader = new SimpleDbfReader(path);
        return reader.Zapisi()
            .Select((rec, index) => new IzvorniRed(
                index + 1,
                rec.DajDecimal("SIFPROD"),
                rec.DajString("PNAZIV"),
                rec.DajString("PRIKAZ"),
                rec.DajString(poljeKonta),
                rec.DajString("SIFARNIK"),
                ukljuciDpIDevizno ? rec.DajString("DP") : string.Empty,
                ukljuciDpIDevizno ? rec.DajString("DEVIZNO") : string.Empty))
            .ToList();
    }

    internal static IReadOnlyList<NalpDefkRow> FormirajPocetneRedove(
        IReadOnlyList<IzvorniRed> aaan,
        IReadOnlyList<IzvorniRed> aatv,
        IReadOnlyList<IzvorniRed> aatm)
    {
        var rezultat = new List<NalpDefkRow>();

        rezultat.AddRange(aaan
            .Where(UkljuciIzvorniRed)
            .Select(r => new NalpDefkRow
            {
                Sifprod = r.Sifprod,
                Pnaziv = r.Pnaziv,
                Konto = r.Konto,
                Imetabele = $"anal{r.RedniBroj}",
                Vrsta = "AN",
                Sifarnik = r.Sifarnik,
                Dp = r.Dp,
                Devizno = r.Devizno,
            }));

        rezultat.AddRange(FormirajPazarskeRedove(aatv, "VP", "tvtm", "V"));
        rezultat.AddRange(FormirajPazarskeRedove(aatm, "MP", "tm", "M"));

        return rezultat;
    }

    private static IEnumerable<NalpDefkRow> FormirajPazarskeRedove(
        IEnumerable<IzvorniRed> izvor,
        string vrsta,
        string prefiksTabele,
        string prefiksDokumenta) =>
        izvor
            .Where(UkljuciIzvorniRed)
            .Select(r => new NalpDefkRow
            {
                Sifprod = r.Sifprod,
                Pnaziv = r.Pnaziv,
                Konto = r.Konto,
                Imetabele = $"{prefiksTabele}{r.RedniBroj}",
                Vrsta = vrsta,
                Sifarnik = r.Sifarnik,
                Dok = $"{prefiksDokumenta}{r.RedniBroj}",
            });

    private static bool UkljuciIzvorniRed(IzvorniRed red) =>
        !string.Equals(red.Prikaz.Trim(), "N", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(red.Konto);

    [RelayCommand]
    private void Dodaj()
    {
        var nov = new NalpDefkRow();
        Redovi.Add(nov);
        SelectedRow = nov;
        DodatRed?.Invoke(nov);
    }

    // ── BRISANJE — DELETE NEXT 1, PACK, snimi ────────────────────
    [RelayCommand]
    private void Brisi()
    {
        if (SelectedRow == null) return;
        if (MessageBox.Show(
            $"Brisanje reda?\n\n{SelectedRow.Konto.Trim()}  —  {SelectedRow.Pnaziv.Trim()}",
            "Potvrda brisanja",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var idx = Redovi.IndexOf(SelectedRow);
        Redovi.Remove(SelectedRow);
        SelectedRow = Redovi.Count > 0 ? Redovi[Math.Min(idx, Redovi.Count - 1)] : null;
        SnimiNalpDefk();
    }

    // ── Eksplicitno snimanje (dugme SNIMI) ───────────────────────
    [RelayCommand]
    private void Snimi()
    {
        SnimiNalpDefk();
        MessageBox.Show("Snimljeno.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SnimiNalpDefk()
    {
        if (!File.Exists(_dbfPath)) return;
        try
        {
            var schema = DbfTableWriter.LoadSchema(_dbfPath);
            DbfTableWriter.WriteTable(_dbfPath, schema, Redovi.ToList(), FieldMapper);
        }
        catch (Exception ex) { MessageBox.Show($"Greška pri snimanju nalpdefk.dbf: {ex.Message}"); }
    }

    private static object? FieldMapper(NalpDefkRow r, string f) => f.ToUpperInvariant() switch
    {
        "SIFPROD"   => (object?)r.Sifprod,
        "KONTO"     => r.Konto,
        "PNAZIV"    => r.Pnaziv,
        "DEVIZNO"   => r.Devizno,
        "SIFARNIK"  => r.Sifarnik,
        "DOK"       => r.Dok,
        "VRSTA"     => r.Vrsta,
        "IMETABELE" => r.Imetabele,
        "DP"        => r.Dp,
        "PRENETO"   => r.Preneto,
        "NUMRED"    => r.Numred,
        "IDBR"      => r.Idbr,
        _           => null,
    };

    // ── Navigacija ───────────────────────────────────────────────
    [RelayCommand]
    private void IdiGore()
    {
        if (SelectedRow == null) return;
        var idx = Redovi.IndexOf(SelectedRow);
        if (idx > 0) SelectedRow = Redovi[idx - 1];
    }

    [RelayCommand]
    private void IdiDole()
    {
        if (SelectedRow == null && Redovi.Count > 0) { SelectedRow = Redovi[0]; return; }
        var idx = Redovi.IndexOf(SelectedRow!);
        if (idx >= 0 && idx < Redovi.Count - 1) SelectedRow = Redovi[idx + 1];
    }

    [RelayCommand] private void IdiNaVrh() { if (Redovi.Count > 0) SelectedRow = Redovi[0]; }
    [RelayCommand] private void IdiNaDno()  { if (Redovi.Count > 0) SelectedRow = Redovi.Last(); }

    // ── Izlaz — auto-snimi pa zatvori ────────────────────────────
    [RelayCommand]
    private void Izlaz()
    {
        SnimiNalpDefk();
        ZatvoriFormu?.Invoke();
    }
}
