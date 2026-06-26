using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class TmKalkulacijaViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly string _dbfName;
    private List<Dictionary<string, object?>> _sveStavke = [];

    public string Naslov { get; }

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _stavke = [];
    [ObservableProperty] private string _filterTekst = "";
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;

    public decimal UkupnoRazlika => Stavke.Sum(s => ExtractDecimal(s, "RAZLIKA"));

    public event Action? ZatvaranjeZahtevano;

    public TmKalkulacijaViewModel(string folderPath, string dbfName = "tmkal.dbf",
        string naslov = "KALKULACIJA — MALOPRODAJA")
    {
        _folderPath = folderPath;
        _dbfName = dbfName;
        Naslov = naslov;
        _ = UcitajAsync();
    }

    private async Task UcitajAsync()
    {
        Ucitava = true;
        StatusPoruka = "Učitavanje...";
        try
        {
            var dbfPath = NadjiDbf(_folderPath, _dbfName);
            if (dbfPath is null)
            {
                StatusPoruka = $"Tabela {_dbfName} nije pronađena.";
                return;
            }
            _sveStavke = await Task.Run(() => DbfReader.CitajSveZapise(dbfPath));
            PrimenjiFilter();
            StatusPoruka = $"Redova: {_sveStavke.Count}.";
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

    partial void OnFilterTekstChanged(string value) => PrimenjiFilter();

    private void PrimenjiFilter()
    {
        var txt = FilterTekst.Trim().ToLowerInvariant();
        var q = string.IsNullOrEmpty(txt)
            ? _sveStavke
            : _sveStavke.Where(s =>
                s.Values.Any(v => v?.ToString()?.ToLowerInvariant().Contains(txt) == true)).ToList();

        Stavke = new ObservableCollection<Dictionary<string, object?>>(q);
        OnPropertyChanged(nameof(UkupnoRazlika));
    }

    private static decimal ExtractDecimal(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var v) || v is null) return 0;
        if (v is decimal d) return d;
        return decimal.TryParse(v.ToString(), out var dp) ? dp : 0;
    }

    [RelayCommand]
    private void OcistiFilter() => FilterTekst = "";

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
