using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class TvFaktureViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly string _zaglavljeDbf;
    private readonly string _stavkeDbf;
    private readonly string _linkPolje;

    private List<Dictionary<string, object?>> _svaZaglavlja = [];
    private List<Dictionary<string, object?>> _sveStavke = [];

    public string Naslov { get; }
    public string NaslovZaglavlja { get; }
    public string NaslovStavki { get; }

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _zaglavlja = [];
    [ObservableProperty] private Dictionary<string, object?>? _izabranoZaglavlje;
    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _stavke = [];
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;
    [ObservableProperty] private DateTime _datumOd = new(DateTime.Today.Year, 1, 1);
    [ObservableProperty] private DateTime _datumDo = DateTime.Today;
    [ObservableProperty] private string _filterSifra = "";
    [ObservableProperty] private string _filterTekst = "";

    public event Action? ZatvaranjeZahtevano;

    public TvFaktureViewModel(
        string folderPath,
        string zaglavljeDbf = "fak.dbf",
        string stavkeDbf = "fakp.dbf",
        string linkPolje = "BRFAK",
        string naslov = "FAKTURE — VELEPRODAJA",
        string naslovZaglavlja = "ZAGLAVLJA FAKTURA",
        string naslovStavki = "STAVKE FAKTURE")
    {
        _folderPath = folderPath;
        _zaglavljeDbf = zaglavljeDbf;
        _stavkeDbf = stavkeDbf;
        _linkPolje = linkPolje;
        Naslov = naslov;
        NaslovZaglavlja = naslovZaglavlja;
        NaslovStavki = naslovStavki;
        _ = UcitajAsync();
    }

    private async Task UcitajAsync()
    {
        Ucitava = true;
        StatusPoruka = "Učitavanje...";
        try
        {
            var zaglavljePath = NadjiDbf(_folderPath, _zaglavljeDbf);
            if (zaglavljePath is null)
            {
                StatusPoruka = $"{_zaglavljeDbf} nije pronađena.";
                return;
            }

            var stavkePath = NadjiDbf(_folderPath, _stavkeDbf);

            _svaZaglavlja = await Task.Run(() => DbfReader.CitajSveZapise(zaglavljePath));
            _sveStavke = stavkePath is not null
                ? await Task.Run(() => DbfReader.CitajSveZapise(stavkePath))
                : [];

            PrimenjiFilter();
            StatusPoruka = $"Zaglavlja: {_svaZaglavlja.Count}  |  Stavke: {_sveStavke.Count}";
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

    partial void OnDatumOdChanged(DateTime value)   => PrimenjiFilter();
    partial void OnDatumDoChanged(DateTime value)   => PrimenjiFilter();
    partial void OnFilterSifraChanged(string value) => PrimenjiFilter();
    partial void OnFilterTekstChanged(string value) => PrimenjiFilter();

    private void PrimenjiFilter()
    {
        var od = DatumOd.Date;
        var do_ = DatumDo.Date;
        var sifra = FilterSifra.Trim().ToLowerInvariant();
        var tekst = FilterTekst.Trim().ToLowerInvariant();

        var q = _svaZaglavlja.AsEnumerable();

        q = q.Where(z =>
        {
            if (!z.TryGetValue("DATDOK", out var dv)) return true;
            var d = dv is DateTime dt ? dt :
                    DateTime.TryParse(dv?.ToString(), out var dt2) ? dt2 : (DateTime?)null;
            return d == null || (d.Value.Date >= od && d.Value.Date <= do_);
        });

        if (!string.IsNullOrEmpty(sifra))
            q = q.Where(z => z.TryGetValue("SIFRA", out var v) &&
                             v?.ToString()?.ToLowerInvariant().StartsWith(sifra) == true);

        if (!string.IsNullOrEmpty(tekst))
            q = q.Where(z => z.Values.Any(v => v?.ToString()?.ToLowerInvariant().Contains(tekst) == true));

        Zaglavlja = new ObservableCollection<Dictionary<string, object?>>(q.ToList());

        // If current selection still visible keep it, otherwise clear detail
        if (IzabranoZaglavlje is not null && !Zaglavlja.Contains(IzabranoZaglavlje))
            IzabranoZaglavlje = null;
    }

    partial void OnIzabranoZaglavljeChanged(Dictionary<string, object?>? value)
    {
        if (value is null) { Stavke.Clear(); return; }

        // Find link field value
        var linkKey = value.Keys.FirstOrDefault(k =>
            k.Equals(_linkPolje, StringComparison.OrdinalIgnoreCase));
        var linkVal = linkKey is not null ? value[linkKey]?.ToString()?.Trim() : null;

        if (string.IsNullOrEmpty(linkVal)) { Stavke.Clear(); return; }

        var filtered = _sveStavke.Where(s =>
        {
            var sk = s.Keys.FirstOrDefault(k => k.Equals(_linkPolje, StringComparison.OrdinalIgnoreCase));
            return sk is not null && s[sk]?.ToString()?.Trim() == linkVal;
        }).ToList();

        Stavke = new ObservableCollection<Dictionary<string, object?>>(filtered);
    }

    [RelayCommand]
    private void Osvezi() => _ = UcitajAsync();

    [RelayCommand]
    private void OcistiFilter()
    {
        DatumOd = new DateTime(DateTime.Today.Year, 1, 1);
        DatumDo = DateTime.Today;
        FilterSifra = "";
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
