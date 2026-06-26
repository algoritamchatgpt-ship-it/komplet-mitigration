using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class GkZnBilansViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly string _dbfName;

    public string Naslov { get; }

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _pozicije = [];
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;
    [ObservableProperty] private string _filterTekst = "";

    private List<Dictionary<string, object?>> _svePozicije = [];

    public event Action? ZatvaranjeZahtevano;

    public GkZnBilansViewModel(string folderPath,
        string dbfName = "znbil.dbf",
        string naslov = "ZAVRŠNI BILANS")
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
            _svePozicije = await Task.Run(() => DbfReader.CitajSveZapise(dbfPath));
            PrimenjiFilter();
            StatusPoruka = $"Ukupno: {_svePozicije.Count} pozicija.";
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
        var tekst = FilterTekst.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(tekst))
        {
            Pozicije = new ObservableCollection<Dictionary<string, object?>>(_svePozicije);
            return;
        }

        var filtrirane = _svePozicije.Where(p =>
            p.Values.Any(v => v?.ToString()?.ToLowerInvariant().Contains(tekst) == true));
        Pozicije = new ObservableCollection<Dictionary<string, object?>>(filtrirane.ToList());
    }

    [RelayCommand]
    private void Osvezi() => _ = UcitajAsync();

    [RelayCommand]
    private void OcistiFilter() => FilterTekst = "";

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
