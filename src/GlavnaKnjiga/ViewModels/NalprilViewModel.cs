using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALPRIL — prilog uz nalog (nalpril.dbf grid editor)</summary>
public partial class NalprilViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly string _dbfPath;
    public event Action? ZatvoriFormu;
    public event Action? OtvoriNalpril0;
    public event Action? OtvoriNalpril1;
    public event Action<string, IReadOnlyList<NalprilPregledRow>>? OtvoriPregled;

    public ObservableCollection<NalprilRow> Rows { get; } = new();

    [ObservableProperty] private NalprilRow? _selectedRow;
    [ObservableProperty] private string      _lblKonto = string.Empty;

    public NalprilViewModel(string firmPath)
    {
        _firmPath = firmPath;
        _dbfPath  = Path.Combine(firmPath, "nalpril.dbf");
        Osvezi();
    }

    public void Osvezi()
    {
        Rows.Clear();
        if (!File.Exists(_dbfPath)) return;
        var r = new SimpleDbfReader(_dbfPath);
        foreach (var rec in r.Zapisi())
            Rows.Add(MapRecord(rec));
        if (Rows.Count > 0) SelectedRow = Rows[0];
    }

    internal static NalprilRow MapRecord(DbfRecord rec) => new()
    {
        Konto   = rec.DajString("KONTO"),
        Sifra   = rec.DajString("SIFRA"),
        Dug     = rec.DajDecimal("DUG"),
        Pot     = rec.DajDecimal("POT"),
        Dugpre  = rec.DajDecimal("DUGPRE"),
        Potpre  = rec.DajDecimal("POTPRE"),
        Naziv   = rec.DajString("NAZIV"),
        Brrac   = rec.DajString("BRRAC"),
        Pauto   = rec.DajString("PAUTO"),
        Opis    = rec.DajString("OPIS"),
        Dat0    = rec.DajDate("DAT0"),
        Dat1    = rec.DajDate("DAT1"),
        K1      = rec.DajString("K1"),
        K2      = rec.DajString("K2"),
        K3      = rec.DajString("K3"),
        Grupa   = rec.DajString("GRUPA"),
        Preneto = rec.DajString("PRENETO"),
        Idbr    = rec.DajDecimal("IDBR"),
    };

    internal static object? FieldMapper(NalprilRow r, string field) => field switch
    {
        "KONTO"   => r.Konto,   "SIFRA"   => r.Sifra,  "DUG"    => r.Dug,
        "POT"     => r.Pot,     "DUGPRE"  => r.Dugpre, "POTPRE" => r.Potpre,
        "NAZIV"   => r.Naziv,   "BRRAC"   => r.Brrac,  "PAUTO"  => r.Pauto,
        "OPIS"    => r.Opis,    "DAT0"    => r.Dat0,   "DAT1"   => r.Dat1,
        "K1"      => r.K1,      "K2"      => r.K2,     "K3"     => r.K3,
        "GRUPA"   => r.Grupa,   "PRENETO" => r.Preneto,"IDBR"   => r.Idbr,
        _         => null,
    };

    partial void OnSelectedRowChanged(NalprilRow? value)
    {
        LblKonto = value?.Konto.Trim() ?? string.Empty;
    }

    [RelayCommand] private void Dodaj()
    {
        var row = new NalprilRow();
        Rows.Add(row);
        SelectedRow = row;
    }

    [RelayCommand]
    private void BrisiPautoRedove()
    {
        // SELECT NALPRIL; DELETE ALL FOR PAUTO='*'; PACK
        var toRemove = Rows.Where(r => r.Pauto.Trim() == "*").ToList();
        foreach (var r in toRemove) Rows.Remove(r);
        Snimi();
        ZatvoriFormu?.Invoke();
    }

    [RelayCommand] private void OtvoriKontaAnalitike() => OtvoriNalpril0?.Invoke();

    [RelayCommand]
    private void PreuzmiIzAnalitike()
    {
        Snimi();
        OtvoriNalpril1?.Invoke();
    }

    [RelayCommand]
    private void TekucaLikvidnost() =>
        OtvoriPregled?.Invoke(
            "TEKUĆA LIKVIDNOST",
            FormirajTekucuLikvidnost(Rows));

    [RelayCommand]
    private void UkupnaLikvidnost() =>
        OtvoriPregled?.Invoke(
            "UKUPNA LIKVIDNOST",
            FormirajUkupnuLikvidnost(Rows));

    [RelayCommand] private void Dole()   { if (SelectedRow != null) { int i = Rows.IndexOf(SelectedRow); if (i < Rows.Count - 1) SelectedRow = Rows[i + 1]; } }
    [RelayCommand] private void Gore()   { if (SelectedRow != null) { int i = Rows.IndexOf(SelectedRow); if (i > 0) SelectedRow = Rows[i - 1]; } }
    [RelayCommand] private void Zadnji() { if (Rows.Count > 0) SelectedRow = Rows[^1]; }
    [RelayCommand] private void Prvi()   { if (Rows.Count > 0) SelectedRow = Rows[0]; }

    [RelayCommand]
    private void Izlaz()
    {
        Snimi();
        ZatvoriFormu?.Invoke();
    }

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

    internal static List<NalprilPregledRow> FormirajTekucuLikvidnost(
        IEnumerable<NalprilRow> redovi) =>
        redovi
            .Where(r => r.Dug != 0 || r.Pot != 0)
            .GroupBy(r => r.Konto.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new NalprilPregledRow
            {
                Grupa = "2",
                Konto = g.Key,
                Dug = g.Sum(r => r.Dug),
                Pot = g.Sum(r => r.Pot),
            })
            .ToList();

    internal static List<NalprilPregledRow> FormirajUkupnuLikvidnost(
        IEnumerable<NalprilRow> redovi)
    {
        var lista = redovi.ToList();
        var prethodno = lista
            .Where(r => r.Dugpre != 0 || r.Potpre != 0)
            .GroupBy(r => r.Konto.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new NalprilPregledRow
            {
                Grupa = "1",
                Konto = g.Key,
                Dugpre = g.Sum(r => r.Dugpre),
                Potpre = g.Sum(r => r.Potpre),
            });

        return prethodno.Concat(FormirajTekucuLikvidnost(lista)).ToList();
    }
}
