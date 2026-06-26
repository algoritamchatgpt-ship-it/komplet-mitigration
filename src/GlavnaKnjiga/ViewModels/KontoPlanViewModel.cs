using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;

namespace GlavnaKnjiga.ViewModels;

public partial class KontoPlanViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly string _fileName;
    private List<KontoPlanRow> _svaKonta = [];

    public event Action? ZatvoriFormu;

    public string Naslov { get; }

    [ObservableProperty] private ObservableCollection<KontoPlanRow> _konta = [];
    [ObservableProperty] private KontoPlanRow? _izabranoKonto;
    [ObservableProperty] private string _filterKonto = string.Empty;
    [ObservableProperty] private string _filterNaziv = string.Empty;
    [ObservableProperty] private string _status = string.Empty;

    public KontoPlanViewModel(string folderPath, string fileName, string naslov)
    {
        _folderPath = folderPath;
        _fileName = fileName;
        Naslov = naslov;
        Ucitaj();
    }

    partial void OnFilterKontoChanged(string value) => PrimenjiFilter();
    partial void OnFilterNazivChanged(string value) => PrimenjiFilter();

    [RelayCommand]
    private void Osvezi() => Ucitaj();

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    internal List<KontoPlanRow> Filtriraj(
        IEnumerable<KontoPlanRow> rows, string konto, string naziv)
    {
        var kontoFilter = konto.Trim();
        var nazivFilter = naziv.Trim();

        return rows
            .Where(r => string.IsNullOrEmpty(kontoFilter) ||
                        r.Konto.StartsWith(
                            kontoFilter, StringComparison.OrdinalIgnoreCase))
            .Where(r => string.IsNullOrEmpty(nazivFilter) ||
                        r.Naziv.Contains(
                            nazivFilter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.Konto, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void Ucitaj()
    {
        var path = NadjiDbf();
        if (path == null)
        {
            _svaKonta = [];
            Konta = [];
            Status = $"{_fileName} nije pronađen.";
            return;
        }

        try
        {
            _svaKonta = new SimpleDbfReader(path).Zapisi()
                .Select(r => new KontoPlanRow
                {
                    Konto = r.DajString("KONTO").Trim(),
                    Naziv = r.DajString("NAZIV").Trim(),
                    K1 = r.DajString("K1").Trim(),
                    K2 = r.DajString("K2").Trim(),
                    K3 = r.DajString("K3").Trim(),
                    K4 = r.DajString("K4").Trim(),
                    K5 = r.DajString("K5").Trim(),
                    K6 = r.DajString("K6").Trim(),
                    Kod = r.DajString("KOD").Trim(),
                    Skonto = r.DajString("SKONTO").Trim(),
                    Jed = r.DajString("JED").Trim(),
                    Konton = r.DajString("KONTON").Trim(),
                })
                .Where(r => !string.IsNullOrEmpty(r.Konto))
                .ToList();
            PrimenjiFilter();
        }
        catch (Exception ex)
        {
            _svaKonta = [];
            Konta = [];
            Status = $"Greška: {ex.Message}";
        }
    }

    private void PrimenjiFilter()
    {
        var rezultat = Filtriraj(_svaKonta, FilterKonto, FilterNaziv);
        Konta = new ObservableCollection<KontoPlanRow>(rezultat);
        IzabranoKonto = Konta.FirstOrDefault();
        Status = $"Prikazano: {Konta.Count} / {_svaKonta.Count}";
    }

    private string? NadjiDbf()
    {
        if (!Directory.Exists(_folderPath)) return null;
        var direct = Path.Combine(_folderPath, _fileName);
        if (File.Exists(direct)) return direct;

        return Directory.EnumerateFiles(_folderPath, "*.dbf", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(p => Path.GetFileName(p).Equals(
                _fileName, StringComparison.OrdinalIgnoreCase));
    }
}
