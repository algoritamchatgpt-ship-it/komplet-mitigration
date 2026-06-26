using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class BlKasViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly string _dbfName;
    private readonly string _datumPolje;
    private List<Dictionary<string, object?>> _sviNalozi = [];

    public string Naslov { get; }

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _nalozi = [];
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;
    [ObservableProperty] private DateTime _datumOd = new(DateTime.Today.Year, 1, 1);
    [ObservableProperty] private DateTime _datumDo = DateTime.Today;
    [ObservableProperty] private string _filterTekst = "";

    public event Action? ZatvaranjeZahtevano;

    public BlKasViewModel(string folderPath, string dbfName = "kas.dbf",
        string naslov = "BLAGAJNA — PREGLED", string datumPolje = "DATDOK")
    {
        _folderPath = folderPath;
        _dbfName = dbfName;
        _datumPolje = datumPolje;
        Naslov = naslov;
        _ = UcitajAsync();
    }

    private async Task UcitajAsync()
    {
        Ucitava = true;
        StatusPoruka = "Učitavanje...";
        try
        {
            var dbfPath = NadjiDbf(_folderPath, _dbfName)
                       ?? NadjiDbf(_folderPath, "blp.dbf")
                       ?? NadjiDbf(_folderPath, "bl1.dbf");
            if (dbfPath is null)
            {
                StatusPoruka = $"Tabela {_dbfName} nije pronađena.";
                return;
            }
            _sviNalozi = await Task.Run(() => DbfReader.CitajSveZapise(dbfPath));
            PrimenjiFilter();
            StatusPoruka = $"Ukupno: {_sviNalozi.Count} zapisa.";
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

    partial void OnDatumOdChanged(DateTime value)    => PrimenjiFilter();
    partial void OnDatumDoChanged(DateTime value)    => PrimenjiFilter();
    partial void OnFilterTekstChanged(string value)  => PrimenjiFilter();

    private void PrimenjiFilter()
    {
        var tekst = FilterTekst.Trim().ToLowerInvariant();
        var od = DatumOd.Date;
        var do_ = DatumDo.Date;

        var filtrirani = _sviNalozi.AsEnumerable();

        filtrirani = filtrirani.Where(n =>
        {
            if (!n.TryGetValue(_datumPolje, out var dv)) return true;
            var d = dv is DateTime dt ? dt :
                    DateTime.TryParse(dv?.ToString(), out var dt2) ? dt2 : (DateTime?)null;
            return d == null || (d.Value.Date >= od && d.Value.Date <= do_);
        });

        if (!string.IsNullOrEmpty(tekst))
            filtrirani = filtrirani.Where(n =>
                n.Values.Any(v => v?.ToString()?.ToLowerInvariant().Contains(tekst) == true));

        Nalozi = new ObservableCollection<Dictionary<string, object?>>(filtrirani.ToList());
    }

    [RelayCommand]
    private void Osvezi() => _ = UcitajAsync();

    [RelayCommand]
    private void OcistiFilter()
    {
        DatumOd = new DateTime(DateTime.Today.Year, 1, 1);
        DatumDo = DateTime.Today;
        FilterTekst = "";
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
