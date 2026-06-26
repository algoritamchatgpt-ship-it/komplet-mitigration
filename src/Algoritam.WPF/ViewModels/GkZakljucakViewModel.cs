using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class GkZakljucakViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly string _dbfName;

    public string Naslov { get; }

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _stavke = [];
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;
    [ObservableProperty] private string _filterKonto = "";
    [ObservableProperty] private string _filterNaziv = "";

    private List<Dictionary<string, object?>> _sveStavke = [];

    public event Action? ZatvaranjeZahtevano;

    public GkZakljucakViewModel(string folderPath,
        string dbfName = "nalzaklj.dbf",
        string naslov = "ZAKLJUČAK NALOGA — ZAKLJUČNI LIST")
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
            StatusPoruka = $"Ukupno: {_sveStavke.Count} konta.";
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

    partial void OnFilterKontoChanged(string value) => PrimenjiFilter();
    partial void OnFilterNazivChanged(string value) => PrimenjiFilter();

    private void PrimenjiFilter()
    {
        var konto = FilterKonto.Trim().ToLowerInvariant();
        var naziv = FilterNaziv.Trim().ToLowerInvariant();

        var filtrirani = _sveStavke.AsEnumerable();

        if (!string.IsNullOrEmpty(konto))
            filtrirani = filtrirani.Where(s =>
                s.TryGetValue("KONTO", out var v) && v?.ToString()?.ToLowerInvariant().StartsWith(konto) == true);

        if (!string.IsNullOrEmpty(naziv))
            filtrirani = filtrirani.Where(s =>
                s.TryGetValue("NAZIV", out var v) && v?.ToString()?.ToLowerInvariant().Contains(naziv) == true);

        Stavke = new ObservableCollection<Dictionary<string, object?>>(filtrirani.ToList());
    }

    [RelayCommand]
    private void Osvezi() => _ = UcitajAsync();

    [RelayCommand]
    private void OcistiFilter()
    {
        FilterKonto = "";
        FilterNaziv = "";
    }

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
                    .FirstOrDefault(x => Path.GetFileName(x).Equals(fileName, StringComparison.OrdinalIgnoreCase));
                if (ci is not null) return ci;
            }
            catch { }
        }
        return null;
    }
}
