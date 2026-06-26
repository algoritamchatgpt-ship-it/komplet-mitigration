using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public record GkDnevnikStavka(
    string BrNal, DateTime Datum, string Konto,
    decimal Dug, decimal Pot,
    string Opis, DateTime DatDok, string Dok,
    string Mp, decimal Mtr, string Vrsta);

public partial class GkDnevnikViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly string _dbfName;
    private readonly string _naslov;
    private List<GkDnevnikStavka> _sveStavke = [];

    public string Naslov => _naslov;

    [ObservableProperty] private ObservableCollection<GkDnevnikStavka> _stavke = [];
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;

    // Filter fields
    [ObservableProperty] private DateTime _datumOd = new(DateTime.Today.Year, 1, 1);
    [ObservableProperty] private DateTime _datumDo = DateTime.Today;
    [ObservableProperty] private string _filterKonto = "";
    [ObservableProperty] private string _filterDok = "";
    [ObservableProperty] private string _filterMp = "";
    [ObservableProperty] private string _prikazRezim = "SVE"; // DUG / POT / SVE

    public string[] PrikazRezimi { get; } = ["SVE", "DUG", "POT"];

    public decimal UkupnoDug => Stavke.Sum(s => s.Dug);
    public decimal UkupnoPot => Stavke.Sum(s => s.Pot);

    public event Action? ZatvaranjeZahtevano;

    public GkDnevnikViewModel(string folderPath,
        string dbfName = "nal.dbf",
        string naslov = "DNEVNIK GLAVNE KNJIGE")
    {
        _folderPath = folderPath;
        _dbfName = dbfName;
        _naslov = naslov;
        _ = UcitajAsync();
    }

    private async Task UcitajAsync()
    {
        Ucitava = true;
        StatusPoruka = "Učitavanje...";
        try
        {
            // For nal.dbf fall back to nalp.dbf (drafts); other files use exact name
            var dbfPath = NadjiDbf(_folderPath, _dbfName)
                       ?? (_dbfName == "nal.dbf" ? NadjiDbf(_folderPath, "nalp.dbf") : null);

            if (dbfPath is null)
            {
                StatusPoruka = $"Tabela {_dbfName} nije pronađena.";
                return;
            }

            var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(dbfPath));
            _sveStavke = zapisi.Select(MapirajStavku).ToList();
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

    private static GkDnevnikStavka MapirajStavku(Dictionary<string, object?> r)
    {
        static string G(Dictionary<string, object?> d, string k) =>
            d.TryGetValue(k, out var v) ? v?.ToString()?.Trim() ?? "" : "";
        static decimal D(Dictionary<string, object?> d, string k) =>
            d.TryGetValue(k, out var v) && v is not null && decimal.TryParse(v.ToString(), out var n) ? n : 0;
        static DateTime Dt(Dictionary<string, object?> d, string k) =>
            d.TryGetValue(k, out var v) && v is DateTime dt ? dt :
            d.TryGetValue(k, out var v2) && DateTime.TryParse(v2?.ToString(), out var dt2) ? dt2 : DateTime.MinValue;

        return new GkDnevnikStavka(
            G(r, "BRNAL"), Dt(r, "DATUM"), G(r, "KONTO"),
            D(r, "DUG"), D(r, "POT"),
            G(r, "OPIS"), Dt(r, "DATDOK"), G(r, "DOK"),
            G(r, "MP"), D(r, "MTR"), G(r, "VRSTA"));
    }

    partial void OnDatumOdChanged(DateTime value)    => PrimenjiFilter();
    partial void OnDatumDoChanged(DateTime value)    => PrimenjiFilter();
    partial void OnFilterKontoChanged(string value)  => PrimenjiFilter();
    partial void OnFilterDokChanged(string value)    => PrimenjiFilter();
    partial void OnFilterMpChanged(string value)     => PrimenjiFilter();
    partial void OnPrikazRezimChanged(string value)  => PrimenjiFilter();

    private void PrimenjiFilter()
    {
        var kFil  = FilterKonto.Trim();
        var dFil  = FilterDok.Trim();
        var mpFil = FilterMp.Trim();
        var od = DatumOd.Date;
        var do_ = DatumDo.Date;

        var result = _sveStavke.AsEnumerable();

        if (kFil.Length > 0)
            result = result.Where(s => s.Konto.StartsWith(kFil, StringComparison.OrdinalIgnoreCase));
        if (dFil.Length > 0)
            result = result.Where(s => s.Dok.Equals(dFil, StringComparison.OrdinalIgnoreCase));
        if (mpFil.Length > 0)
            result = result.Where(s => s.Mp.Equals(mpFil, StringComparison.OrdinalIgnoreCase));

        result = result.Where(s => s.DatDok != DateTime.MinValue
            ? s.DatDok.Date >= od && s.DatDok.Date <= do_
            : s.Datum.Date >= od && s.Datum.Date <= do_);

        result = PrikazRezim switch
        {
            "DUG" => result.Where(s => s.Dug != 0),
            "POT" => result.Where(s => s.Pot != 0),
            _     => result
        };

        Stavke = new ObservableCollection<GkDnevnikStavka>(result);
        OnPropertyChanged(nameof(UkupnoDug));
        OnPropertyChanged(nameof(UkupnoPot));
        StatusPoruka = $"Prikazano: {Stavke.Count} / {_sveStavke.Count} stavki | Dug: {UkupnoDug:N2} | Pot: {UkupnoPot:N2}";
    }

    [RelayCommand]
    private void Osvezi() => _ = UcitajAsync();

    [RelayCommand]
    private void Zatvori() => ZatvaranjeZahtevano?.Invoke();

    private static string? NadjiDbf(string folderPath, string fileName)
    {
        foreach (var dir in new[] { folderPath,
            Path.Combine(folderPath, "data00"),
            Path.Combine(folderPath, "01") })
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
