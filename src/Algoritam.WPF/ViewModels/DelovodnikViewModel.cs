using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class DelovodnikViewModel : ObservableObject
{
    private readonly string _folderPath;
    private List<Dictionary<string, object?>> _sveStavke = [];

    public string Naslov => "DELOVODNIK";

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _stavke = [];
    [ObservableProperty] private string _filterTekst = "";
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;

    public event Action? ZatvaranjeZahtevano;

    public DelovodnikViewModel(string folderPath)
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
            var dbfPath = NadjiDbf(_folderPath, "delov.dbf");
            if (dbfPath is null)
            {
                StatusPoruka = "Tabela delov.dbf nije pronađena.";
                return;
            }

            var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(dbfPath));

            var nazivVrsta  = await UcitajSifarnikAsync("delvrsta.dbf", "VRSTA");
            var nazivDok    = await UcitajSifarnikAsync("deldok.dbf", "DOK");
            var nazivLokac  = await UcitajSifarnikAsync("dellokac.dbf", "SIFLOKAC");
            var nazivStatus = await UcitajSifarnikAsync("delstat.dbf", "SIFSTATUS");
            var nazivPrim   = await UcitajSifarnikAsync("delprim.dbf", "SIFPRIM");
            var nazivOrgan  = await UcitajSifarnikAsync("delorgan.dbf", "SIFORGAN");

            foreach (var z in zapisi)
            {
                z["VRSTA_NAZIV"]  = NadjiNaziv(nazivVrsta, z, "VRSTA");
                z["DOK_NAZIV"]    = NadjiNaziv(nazivDok, z, "DOK");
                z["LOKAC_NAZIV"]  = NadjiNaziv(nazivLokac, z, "SIFLOKAC");
                z["STATUS_NAZIV"] = NadjiNaziv(nazivStatus, z, "SIFSTATUS");
                z["PRIM_NAZIV"]   = NadjiNaziv(nazivPrim, z, "SIFPRIM");
                z["ORGAN_NAZIV"]  = NadjiNaziv(nazivOrgan, z, "SIFORGAN");
            }

            _sveStavke = zapisi;
            PrimenjiFilter();
            StatusPoruka = $"Ukupno zapisa: {_sveStavke.Count}.";
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

    private async Task<Dictionary<string, string>> UcitajSifarnikAsync(string dbfName, string sifraPolje)
    {
        var rezultat = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var path = NadjiDbf(_folderPath, dbfName);
            if (path is null) return rezultat;

            var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(path));
            foreach (var z in zapisi)
            {
                var sifra = z.TryGetValue(sifraPolje, out var s) ? s?.ToString()?.Trim() : null;
                var naziv = z.TryGetValue("NAZIV", out var n) ? n?.ToString()?.Trim() : null;
                if (!string.IsNullOrEmpty(sifra))
                    rezultat[sifra] = naziv ?? "";
            }
        }
        catch { }
        return rezultat;
    }

    private static string NadjiNaziv(Dictionary<string, string> sifarnik, Dictionary<string, object?> zapis, string polje)
    {
        var sifra = zapis.TryGetValue(polje, out var s) ? s?.ToString()?.Trim() : null;
        if (string.IsNullOrEmpty(sifra)) return "";
        return sifarnik.TryGetValue(sifra, out var naziv) ? naziv : sifra;
    }

    partial void OnFilterTekstChanged(string value) => PrimenjiFilter();

    private void PrimenjiFilter()
    {
        var txt = FilterTekst.Trim().ToLowerInvariant();
        var q = string.IsNullOrEmpty(txt)
            ? _sveStavke
            : _sveStavke.Where(s =>
                s.Values.Any(v => v?.ToString()?.ToLowerInvariant().Contains(txt) == true)).ToList();

        Stavke = new ObservableCollection<Dictionary<string, object?>>(q);
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
