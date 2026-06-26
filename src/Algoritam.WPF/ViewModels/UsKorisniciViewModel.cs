using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Pregled korisnika komunalnih usluga — uskor.dbf sa filterom po adresi/imenu.
/// </summary>
public partial class UsKorisniciViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly string _dbfName;

    public string Naslov { get; }

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _korisnici = [];
    [ObservableProperty] private Dictionary<string, object?>? _izabraniKorisnik;
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;
    [ObservableProperty] private string _filterTekst = "";

    private List<Dictionary<string, object?>> _sviKorisnici = [];

    public event Action? ZatvaranjeZahtevano;

    public UsKorisniciViewModel(string folderPath, string dbfName = "uskor.dbf",
        string naslov = "KORISNICI KOMUNALNIH USLUGA")
    {
        _folderPath = folderPath;
        _dbfName = dbfName;
        Naslov = naslov;
        _ = UcitajAsync();
    }

    private async Task UcitajAsync()
    {
        Ucitava = true;
        StatusPoruka = "Učitavanje korisnika...";
        try
        {
            var dbfPath = NadjiDbf(_folderPath, _dbfName);
            if (dbfPath is null)
            {
                StatusPoruka = $"Tabela {_dbfName} nije pronađena.";
                return;
            }
            _sviKorisnici = await Task.Run(() => DbfReader.CitajSveZapise(dbfPath));
            PrimenjiFilter();
            StatusPoruka = $"Ukupno: {_sviKorisnici.Count} korisnika.";
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
        if (string.IsNullOrWhiteSpace(FilterTekst))
        {
            Korisnici = new ObservableCollection<Dictionary<string, object?>>(_sviKorisnici);
            return;
        }
        var upit = FilterTekst.ToLowerInvariant();
        var filtrirani = _sviKorisnici.Where(k =>
            k.Values.Any(v => v?.ToString()?.ToLowerInvariant().Contains(upit) == true)).ToList();
        Korisnici = new ObservableCollection<Dictionary<string, object?>>(filtrirani);
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
                    .FirstOrDefault(x => Path.GetFileName(x).Equals(fileName, StringComparison.OrdinalIgnoreCase));
                if (ci is not null) return ci;
            }
            catch { }
        }
        return null;
    }
}
