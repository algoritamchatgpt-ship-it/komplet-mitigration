using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class GkPdvViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly string _pdvTip;
    private readonly string? _dbfNameOverride;
    private readonly string? _naslovOverride;
    private List<Dictionary<string, object?>> _sveStavke = [];

    public string Naslov => _naslovOverride ?? (_pdvTip == "I"
        ? "EVIDENCIJA IZLAZNIH RAČUNA ZA OBRAČUN PDV"
        : "EVIDENCIJA ULAZNIH RAČUNA ZA OBRAČUN PDV");

    public bool JeIzlazni => _pdvTip == "I";

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _stavke = [];
    [ObservableProperty] private Dictionary<string, object?>? _izabranaStavka;
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;
    [ObservableProperty] private DateTime _datumOd = new(DateTime.Today.Year, 1, 1);
    [ObservableProperty] private DateTime _datumDo = DateTime.Today;
    [ObservableProperty] private string _filterSifra = "";
    [ObservableProperty] private string _filterDok = "";
    [ObservableProperty] private string _filterVpdv = "";

    private string DbfName => _dbfNameOverride ?? (_pdvTip == "I" ? "pdvi.dbf" : "pdvu.dbf");

    public event Action? ZatvaranjeZahtevano;

    public GkPdvViewModel(string folderPath, string pdvTip = "I",
        string? dbfNameOverride = null, string? naslovOverride = null)
    {
        _folderPath = folderPath;
        _pdvTip = pdvTip.ToUpperInvariant();
        _dbfNameOverride = dbfNameOverride;
        _naslovOverride = naslovOverride;
        _ = UcitajAsync();
    }

    private async Task UcitajAsync()
    {
        Ucitava = true;
        StatusPoruka = "Učitavanje...";
        try
        {
            var dbfPath = NadjiDbf(_folderPath, DbfName);
            if (dbfPath is null)
            {
                StatusPoruka = $"Tabela {DbfName} nije pronađena.";
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
    partial void OnDatumDoChanged(DateTime value)  => PrimenjiFilter();
    partial void OnFilterSifraChanged(string value) => PrimenjiFilter();
    partial void OnFilterDokChanged(string value)   => PrimenjiFilter();
    partial void OnFilterVpdvChanged(string value)  => PrimenjiFilter();

    private void PrimenjiFilter()
    {
        var od = DatumOd.Date;
        var do_ = DatumDo.Date;
        var sifra = FilterSifra.Trim().ToLowerInvariant();
        var dok = FilterDok.Trim().ToLowerInvariant();
        var vpdv = FilterVpdv.Trim().ToLowerInvariant();

        var q = _sveStavke.AsEnumerable();

        q = q.Where(s =>
        {
            if (!s.TryGetValue("DATDOK", out var dv)) return true;
            var d = dv is DateTime dt ? dt :
                    DateTime.TryParse(dv?.ToString(), out var dt2) ? dt2 : (DateTime?)null;
            return d == null || (d.Value.Date >= od && d.Value.Date <= do_);
        });

        if (!string.IsNullOrEmpty(sifra))
            q = q.Where(s => s.TryGetValue("SIFRA", out var v) &&
                             v?.ToString()?.ToLowerInvariant().StartsWith(sifra) == true);

        if (!string.IsNullOrEmpty(dok))
            q = q.Where(s => s.TryGetValue("DOK", out var v) &&
                             v?.ToString()?.ToLowerInvariant().Contains(dok) == true);

        if (!string.IsNullOrEmpty(vpdv))
            q = q.Where(s => s.TryGetValue("VPDV", out var v) &&
                             v?.ToString()?.ToLowerInvariant().Contains(vpdv) == true);

        Stavke = new ObservableCollection<Dictionary<string, object?>>(q.ToList());
    }

    [RelayCommand]
    private void Osvezi() => _ = UcitajAsync();

    [RelayCommand]
    private void OcistiFilter()
    {
        DatumOd = new DateTime(DateTime.Today.Year, 1, 1);
        DatumDo = DateTime.Today;
        FilterSifra = "";
        FilterDok = "";
        FilterVpdv = "";
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
