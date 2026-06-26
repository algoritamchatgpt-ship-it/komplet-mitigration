using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public record KontoPlanStavka(
    string Konto, string Naziv,
    string K1, string K2, string K3, string K4, string K5, string K6,
    string Kn1, string Kn2, string Kn3, string Kn4, string Kn5, string Kn6);

public partial class GkKontniPlanViewModel : ObservableObject
{
    private readonly string _folderPath;
    private List<KontoPlanStavka> _svaKonta = [];

    public string Naslov => "KONTNI PLAN";

    [ObservableProperty] private ObservableCollection<KontoPlanStavka> _konta = [];
    [ObservableProperty] private KontoPlanStavka? _izabranoKonto;
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;
    [ObservableProperty] private string _filterKonto = "";
    [ObservableProperty] private string _filterNaziv = "";

    public event Action? ZatvaranjeZahtevano;

    public GkKontniPlanViewModel(string folderPath)
    {
        _folderPath = folderPath;
        _ = UcitajAsync();
    }

    private async Task UcitajAsync()
    {
        Ucitava = true;
        StatusPoruka = "Učitavanje kontnog plana...";
        try
        {
            var dbfPath = NadjiDbf(_folderPath, "konplan.dbf")
                       ?? NadjiDbf(_folderPath, "konto.dbf");
            if (dbfPath is null)
            {
                StatusPoruka = "Tabela kontnog plana nije pronađena.";
                return;
            }

            var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(dbfPath));
            _svaKonta = zapisi.Select(MapirajStavku).ToList();
            PrimenjiFilter();
            StatusPoruka = $"Kontni plan: {_svaKonta.Count} konta.";
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

    private static KontoPlanStavka MapirajStavku(Dictionary<string, object?> r)
    {
        static string G(Dictionary<string, object?> d, string k) =>
            d.TryGetValue(k, out var v) ? v?.ToString()?.Trim() ?? "" : "";
        return new KontoPlanStavka(
            G(r, "KONTO"), G(r, "NAZIV"),
            G(r, "K1"), G(r, "K2"), G(r, "K3"), G(r, "K4"), G(r, "K5"), G(r, "K6"),
            G(r, "KN1"), G(r, "KN2"), G(r, "KN3"), G(r, "KN4"), G(r, "KN5"), G(r, "KN6"));
    }

    partial void OnFilterKontoChanged(string value) => PrimenjiFilter();
    partial void OnFilterNazivChanged(string value) => PrimenjiFilter();

    private void PrimenjiFilter()
    {
        var kFil = FilterKonto.Trim().ToLowerInvariant();
        var nFil = FilterNaziv.Trim().ToLowerInvariant();
        var result = _svaKonta.AsEnumerable();
        if (kFil.Length > 0)
            result = result.Where(s => s.Konto.ToLowerInvariant().StartsWith(kFil));
        if (nFil.Length > 0)
            result = result.Where(s => s.Naziv.ToLowerInvariant().Contains(nFil)
                                    || s.Kn1.ToLowerInvariant().Contains(nFil)
                                    || s.Kn2.ToLowerInvariant().Contains(nFil)
                                    || s.Kn3.ToLowerInvariant().Contains(nFil));
        Konta = new ObservableCollection<KontoPlanStavka>(result);
    }

    [RelayCommand]
    private void Prvo()
    {
        if (Konta.Count > 0) IzabranoKonto = Konta[0];
    }

    [RelayCommand]
    private void Gore()
    {
        var idx = IzabranoKonto is null ? -1 : Konta.IndexOf(IzabranoKonto);
        if (idx > 0) IzabranoKonto = Konta[idx - 1];
    }

    [RelayCommand]
    private void Dole()
    {
        var idx = IzabranoKonto is null ? -1 : Konta.IndexOf(IzabranoKonto);
        if (idx >= 0 && idx < Konta.Count - 1) IzabranoKonto = Konta[idx + 1];
    }

    [RelayCommand]
    private void Zadnje()
    {
        if (Konta.Count > 0) IzabranoKonto = Konta[^1];
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
