using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class TvRobaViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly string _dbfName;

    public string Naslov { get; }

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _stavke = [];
    [ObservableProperty] private Dictionary<string, object?>? _izabranaStavka;
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;
    [ObservableProperty] private string _filterSifra = "";
    [ObservableProperty] private string _filterNaziv = "";

    private List<Dictionary<string, object?>> _sveStavke = [];
    private string _sifraPolje = "";
    private string _nazivPolje = "";
    private string? _dbfPath;
    private DbfTableWriter.DbfSchema? _schema;

    public event Action? ZatvaranjeZahtevano;

    public TvRobaViewModel(string folderPath, string dbfName = "roba.dbf",
        string naslov = "ŠIFARNIK ROBE")
    {
        _folderPath = folderPath;
        _dbfName = dbfName;
        Naslov = naslov;
        _ = UcitajAsync();
    }

    private async Task UcitajAsync()
    {
        Ucitava = true;
        StatusPoruka = "Učitavanje...";
        try
        {
            _dbfPath = NadjiDbf(_folderPath, _dbfName);
            if (_dbfPath is null)
            {
                StatusPoruka = $"Tabela {_dbfName} nije pronađena.";
                return;
            }
            _schema = DbfTableWriter.LoadSchema(_dbfPath);
            _sveStavke = await Task.Run(() => DbfReader.CitajSveZapise(_dbfPath));
            var kljucevi = _sveStavke.Count > 0
                ? _sveStavke[0].Keys.ToList()
                : _schema.Fields.Select(f => f.Name).ToList();
            _sifraPolje = NadjiPolje(kljucevi, "SIFRA", "SIF", "KOD", "BROBA", "SROBA");
            _nazivPolje = NadjiPolje(kljucevi, "NAZIV", "NAZ", "IME", "OPIS");
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

    private static string NadjiPolje(List<string> kljucevi, params string[] kandidati)
    {
        foreach (var k in kandidati)
        {
            var hit = kljucevi.FirstOrDefault(x => x.Equals(k, StringComparison.OrdinalIgnoreCase)
                                                || x.StartsWith(k, StringComparison.OrdinalIgnoreCase));
            if (hit is not null) return hit;
        }
        return "";
    }

    partial void OnFilterSifraChanged(string value) => PrimenjiFilter();
    partial void OnFilterNazivChanged(string value) => PrimenjiFilter();

    private void PrimenjiFilter()
    {
        var sifra = FilterSifra.Trim().ToLowerInvariant();
        var naziv = FilterNaziv.Trim().ToLowerInvariant();

        var filtrirane = _sveStavke.AsEnumerable();

        if (!string.IsNullOrEmpty(sifra))
            filtrirane = filtrirane.Where(s =>
                (_sifraPolje.Length > 0 &&
                 s.TryGetValue(_sifraPolje, out var sf) &&
                 sf?.ToString()?.ToLowerInvariant().StartsWith(sifra) == true) ||
                (_sifraPolje.Length == 0 &&
                 s.Values.Any(v => v?.ToString()?.ToLowerInvariant().Contains(sifra) == true)));

        if (!string.IsNullOrEmpty(naziv))
            filtrirane = filtrirane.Where(s =>
                (_nazivPolje.Length > 0 &&
                 s.TryGetValue(_nazivPolje, out var nv) &&
                 nv?.ToString()?.ToLowerInvariant().Contains(naziv) == true) ||
                (_nazivPolje.Length == 0 &&
                 s.Values.Any(v => v?.ToString()?.ToLowerInvariant().Contains(naziv) == true)));

        Stavke = new ObservableCollection<Dictionary<string, object?>>(filtrirane.ToList());
    }

    [RelayCommand]
    private void Osvezi() => _ = UcitajAsync();

    [RelayCommand]
    private void OcistiFilter()
    {
        FilterSifra = "";
        FilterNaziv = "";
    }

    [RelayCommand]
    private void Zatvori() => ZatvaranjeZahtevano?.Invoke();

    [RelayCommand]
    private void Dodaj(System.Windows.Window? vlasnik)
    {
        if (_dbfPath is null || _schema is null) return;

        var dijalogVm = new DbfUnosIzmenaViewModel(_schema, "NOVA STAVKA", bojaHeader: "#BF360C");
        var dijalog = new Views.DbfUnosIzmenaView { DataContext = dijalogVm, Owner = vlasnik };
        dijalog.ShowDialog();

        if (!dijalogVm.Uspesno || dijalogVm.Rezultat is null) return;

        var novaSifra = _sifraPolje.Length > 0 && dijalogVm.Rezultat.TryGetValue(_sifraPolje, out var s)
            ? s?.ToString()?.Trim() : null;
        if (_sifraPolje.Length > 0 && string.IsNullOrWhiteSpace(novaSifra))
        {
            StatusPoruka = "Šifra mora biti uneta.";
            return;
        }
        if (_sifraPolje.Length > 0 && _sveStavke.Any(z =>
                string.Equals(z.GetValueOrDefault(_sifraPolje)?.ToString()?.Trim(), novaSifra, StringComparison.OrdinalIgnoreCase)))
        {
            StatusPoruka = $"Šifra {novaSifra} već postoji.";
            return;
        }

        _sveStavke.Add(dijalogVm.Rezultat);
        SacuvajTabelu();
    }

    [RelayCommand]
    private void Izmeni(System.Windows.Window? vlasnik)
    {
        if (_dbfPath is null || _schema is null || IzabranaStavka is null) return;

        var dijalogVm = new DbfUnosIzmenaViewModel(_schema, "IZMENA STAVKE", IzabranaStavka, bojaHeader: "#BF360C");
        var dijalog = new Views.DbfUnosIzmenaView { DataContext = dijalogVm, Owner = vlasnik };
        dijalog.ShowDialog();

        if (!dijalogVm.Uspesno || dijalogVm.Rezultat is null) return;

        var indeks = _sveStavke.IndexOf(IzabranaStavka);
        if (indeks < 0) return;

        _sveStavke[indeks] = dijalogVm.Rezultat;
        SacuvajTabelu();
    }

    [RelayCommand]
    private void Obrisi()
    {
        if (_dbfPath is null || _schema is null || IzabranaStavka is null) return;

        var opis = _sifraPolje.Length > 0 ? IzabranaStavka.GetValueOrDefault(_sifraPolje)?.ToString()?.Trim() : "stavku";
        if (System.Windows.MessageBox.Show($"Obrisati: {opis}?", "Brisanje",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question)
            != System.Windows.MessageBoxResult.Yes)
            return;

        _sveStavke.Remove(IzabranaStavka);
        IzabranaStavka = null;
        SacuvajTabelu();
    }

    private void SacuvajTabelu()
    {
        if (_dbfPath is null || _schema is null) return;
        try
        {
            DbfTableWriter.WriteTable(_dbfPath, _schema, _sveStavke,
                (red, polje) => red.GetValueOrDefault(polje));
            PrimenjiFilter();
            StatusPoruka = $"Ukupno: {_sveStavke.Count} stavki.";
        }
        catch (Exception ex)
        {
            StatusPoruka = $"Greška pri čuvanju: {ex.Message}";
        }
    }

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
