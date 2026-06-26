using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class GkNalBrojViewModel : ObservableObject
{
    private readonly string _folderPath;
    private List<Dictionary<string, object?>> _sviNalozi = [];

    public string Naslov => "BROJEVI NALOGA — REGISTAR";

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _nalozi = [];
    [ObservableProperty] private string _filterTip = "";
    [ObservableProperty] private string _filterOpis = "";
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;

    public event Action? ZatvaranjeZahtevano;

    public GkNalBrojViewModel(string folderPath)
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
            var dbfPath = NadjiDbf(_folderPath, "nalbroj.dbf");
            if (dbfPath is null)
            {
                StatusPoruka = "Tabela nalbroj.dbf nije pronađena.";
                return;
            }
            _sviNalozi = await Task.Run(() => DbfReader.CitajSveZapise(dbfPath));
            PrimenjiFilter();
            StatusPoruka = $"Naloga: {_sviNalozi.Count}.";
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

    partial void OnFilterTipChanged(string value)  => PrimenjiFilter();
    partial void OnFilterOpisChanged(string value) => PrimenjiFilter();

    private void PrimenjiFilter()
    {
        var tip   = FilterTip.Trim().ToUpperInvariant();
        var opis  = FilterOpis.Trim().ToLowerInvariant();

        var q = _sviNalozi.AsEnumerable();

        if (tip.Length > 0)
            q = q.Where(s => s.TryGetValue("TIP", out var v) &&
                v?.ToString()?.Trim().ToUpperInvariant().StartsWith(tip) == true);

        if (opis.Length > 0)
            q = q.Where(s => s.TryGetValue("OPIS", out var v) &&
                v?.ToString()?.ToLowerInvariant().Contains(opis) == true);

        Nalozi = new ObservableCollection<Dictionary<string, object?>>(q.ToList());
    }

    [RelayCommand]
    private void OcistiFilter()
    {
        FilterTip = "";
        FilterOpis = "";
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
