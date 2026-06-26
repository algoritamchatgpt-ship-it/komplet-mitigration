using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class GkIdentViewModel : ObservableObject
{
    private readonly string _folderPath;
    private List<Dictionary<string, object?>> _sviIden = [];

    public string Naslov => "GK IDENTIFIKATORI POSLOVNIH PROMENA";

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _identifikatori = [];
    [ObservableProperty] private string _filterTekst = "";
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;

    public event Action? ZatvaranjeZahtevano;

    public GkIdentViewModel(string folderPath)
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
            var dbfPath = NadjiDbf(_folderPath, "gkid.dbf");
            if (dbfPath is null)
            {
                StatusPoruka = "Tabela gkid.dbf nije pronađena.";
                return;
            }
            _sviIden = await Task.Run(() => DbfReader.CitajSveZapise(dbfPath));
            PrimenjiFilter();
            StatusPoruka = $"Identifikatora: {_sviIden.Count}.";
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
            ? _sviIden
            : _sviIden.Where(s =>
                s.Values.Any(v => v?.ToString()?.ToLowerInvariant().Contains(txt) == true)).ToList();

        Identifikatori = new ObservableCollection<Dictionary<string, object?>>(q);
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
