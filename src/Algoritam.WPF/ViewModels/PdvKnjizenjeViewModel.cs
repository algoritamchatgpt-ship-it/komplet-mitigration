using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class PdvKnjizenjeViewModel : ObservableObject
{
    private readonly string _folderPath;
    private List<Dictionary<string, object?>> _sveStavke = [];

    public string Naslov => "PDV — KNJIŽENJA U GLAVNU KNJIGU";

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _stavke = [];
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;
    [ObservableProperty] private DateTime _datumOd = new(DateTime.Today.Year, 1, 1);
    [ObservableProperty] private DateTime _datumDo = DateTime.Today;
    [ObservableProperty] private string _filterKonto = "";
    [ObservableProperty] private string _filterVpdv = "";

    public decimal UkupnoDug => Stavke.Sum(s => ExtractDecimal(s, "DUG"));
    public decimal UkupnoPot => Stavke.Sum(s => ExtractDecimal(s, "POT"));

    public event Action? ZatvaranjeZahtevano;

    public PdvKnjizenjeViewModel(string folderPath)
    {
        _folderPath = folderPath;
        _ = UcitajAsync();
    }

    private async Task UcitajAsync()
    {
        Ucitava = true;
        StatusPoruka = "Učitavanje...";
        try
        {
            var dbfPath = NadjiDbf(_folderPath, "pdvsn.dbf");
            if (dbfPath is null)
            {
                StatusPoruka = "Tabela pdvsn.dbf nije pronađena.";
                return;
            }
            _sveStavke = await Task.Run(() => DbfReader.CitajSveZapise(dbfPath));
            PrimenjiFilter();
            StatusPoruka = $"Ukupno: {_sveStavke.Count} stavki.";
        }
        catch (Exception ex)
        {
            StatusPoruka = $"Greška: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    partial void OnDatumOdChanged(DateTime value)  => PrimenjiFilter();
    partial void OnDatumDoChanged(DateTime value)   => PrimenjiFilter();
    partial void OnFilterKontoChanged(string value) => PrimenjiFilter();
    partial void OnFilterVpdvChanged(string value)  => PrimenjiFilter();

    private void PrimenjiFilter()
    {
        var od    = DatumOd.Date;
        var do_   = DatumDo.Date;
        var konto = FilterKonto.Trim();
        var vpdv  = FilterVpdv.Trim().ToUpperInvariant();

        var q = _sveStavke.AsEnumerable();

        q = q.Where(s =>
        {
            var dat = ExtractDate(s, "DATPDV");
            if (dat == DateTime.MinValue) dat = ExtractDate(s, "DATDOK");
            return dat == DateTime.MinValue || (dat.Date >= od && dat.Date <= do_);
        });

        if (konto.Length > 0)
            q = q.Where(s => s.TryGetValue("KONTO", out var v) &&
                v?.ToString()?.StartsWith(konto, StringComparison.OrdinalIgnoreCase) == true);

        if (vpdv.Length > 0)
            q = q.Where(s => s.TryGetValue("VPDV", out var v) &&
                v?.ToString()?.Trim().Equals(vpdv, StringComparison.OrdinalIgnoreCase) == true);

        Stavke = new ObservableCollection<Dictionary<string, object?>>(q.ToList());
        OnPropertyChanged(nameof(UkupnoDug));
        OnPropertyChanged(nameof(UkupnoPot));
    }

    private static decimal ExtractDecimal(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var v) || v is null) return 0;
        if (v is decimal d) return d;
        return decimal.TryParse(v.ToString(), out var dp) ? dp : 0;
    }

    private static DateTime ExtractDate(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var v)) return DateTime.MinValue;
        if (v is DateTime dt) return dt;
        return DateTime.TryParse(v?.ToString(), out var dt2) ? dt2 : DateTime.MinValue;
    }

    [RelayCommand]
    private void OcistiFilter()
    {
        DatumOd = new DateTime(DateTime.Today.Year, 1, 1);
        DatumDo = DateTime.Today;
        FilterKonto = "";
        FilterVpdv = "";
    }

    [RelayCommand]
    private void Osvezi() => _ = UcitajAsync();

    [RelayCommand]
    private void Zatvori() => ZatvaranjeZahtevano?.Invoke();

    private static string? NadjiDbf(string folderPath, string fileName)
    {
        foreach (var dir in new[] { folderPath,
            Path.Combine(folderPath, "data00"),
            Path.Combine(folderPath, "01"),
            Path.Combine(folderPath, "..") })
        {
            if (!Directory.Exists(dir)) continue;
            var f = Path.Combine(dir, fileName);
            if (File.Exists(f)) return f;
            try
            {
                var ci = Directory.GetFiles(dir, "*.dbf", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(x => Path.GetFileName(x)
                        .Equals(fileName, StringComparison.OrdinalIgnoreCase));
                if (ci is not null) return ci;
            }
            catch { }
        }
        return null;
    }
}
