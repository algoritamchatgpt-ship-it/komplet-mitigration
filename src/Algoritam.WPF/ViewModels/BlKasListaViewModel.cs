using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class BlKasListaViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly string _dbfName;
    private List<Dictionary<string, object?>> _sveStavke = [];
    private string? _dbfPath;
    private DbfTableWriter.DbfSchema? _schema;

    public string Naslov { get; }

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _stavke = [];
    [ObservableProperty] private Dictionary<string, object?>? _izabranaStavka;
    [ObservableProperty] private string _filterTekst = "";
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;

    public decimal UkupnoSaldo => Stavke.Sum(s => ExtractDecimal(s, "SALDO"));

    public event Action? ZatvaranjeZahtevano;

    public BlKasListaViewModel(string folderPath, string dbfName = "kaslist.dbf",
        string naslov = "LISTA KASA")
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
            PrimenjiFilter();
            StatusPoruka = $"Kasa: {_sveStavke.Count}.";
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
            ? _sveStavke
            : _sveStavke.Where(s =>
                s.Values.Any(v => v?.ToString()?.ToLowerInvariant().Contains(txt) == true)).ToList();

        Stavke = new ObservableCollection<Dictionary<string, object?>>(q);
        OnPropertyChanged(nameof(UkupnoSaldo));
    }

    private static decimal ExtractDecimal(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var v) || v is null) return 0;
        if (v is decimal d) return d;
        return decimal.TryParse(v.ToString(), out var dp) ? dp : 0;
    }

    [RelayCommand]
    private void OcistiFilter() => FilterTekst = "";

    [RelayCommand]
    private void Osvezi() => _ = UcitajAsync();

    [RelayCommand]
    private void Zatvori() => ZatvaranjeZahtevano?.Invoke();

    [RelayCommand]
    private void Dodaj(System.Windows.Window? vlasnik)
    {
        if (_dbfPath is null || _schema is null) return;

        var dijalogVm = new DbfUnosIzmenaViewModel(_schema, "NOVA KASA", bojaHeader: "#1B5E20");
        var dijalog = new Views.DbfUnosIzmenaView { DataContext = dijalogVm, Owner = vlasnik };
        dijalog.ShowDialog();

        if (!dijalogVm.Uspesno || dijalogVm.Rezultat is null) return;

        var novaSifra = dijalogVm.Rezultat.TryGetValue("KASIFRA", out var s) ? s?.ToString()?.Trim() : null;
        if (string.IsNullOrWhiteSpace(novaSifra))
        {
            StatusPoruka = "Šifra kase mora biti uneta.";
            return;
        }
        if (_sveStavke.Any(z => string.Equals(z.GetValueOrDefault("KASIFRA")?.ToString()?.Trim(), novaSifra, StringComparison.OrdinalIgnoreCase)))
        {
            StatusPoruka = $"Kasa {novaSifra} već postoji.";
            return;
        }

        _sveStavke.Add(dijalogVm.Rezultat);
        SacuvajTabelu();
    }

    [RelayCommand]
    private void Izmeni(System.Windows.Window? vlasnik)
    {
        if (_dbfPath is null || _schema is null || IzabranaStavka is null) return;

        var dijalogVm = new DbfUnosIzmenaViewModel(_schema, "IZMENA KASE", IzabranaStavka, bojaHeader: "#1B5E20");
        var dijalog = new Views.DbfUnosIzmenaView { DataContext = dijalogVm, Owner = vlasnik };
        dijalog.ShowDialog();

        if (!dijalogVm.Uspesno || dijalogVm.Rezultat is null) return;

        var sifra = dijalogVm.Rezultat.TryGetValue("KASIFRA", out var s) ? s?.ToString()?.Trim() : null;
        if (string.IsNullOrWhiteSpace(sifra))
        {
            StatusPoruka = "Šifra kase mora biti uneta.";
            return;
        }

        var indeks = _sveStavke.IndexOf(IzabranaStavka);
        if (indeks < 0) return;

        _sveStavke[indeks] = dijalogVm.Rezultat;
        SacuvajTabelu();
    }

    [RelayCommand]
    private void Obrisi()
    {
        if (_dbfPath is null || _schema is null || IzabranaStavka is null) return;

        var sifra = IzabranaStavka.GetValueOrDefault("KASIFRA")?.ToString()?.Trim();
        if (System.Windows.MessageBox.Show($"Obrisati kasu: {sifra}?", "Brisanje",
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
            StatusPoruka = $"Kasa: {_sveStavke.Count}.";
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
                    .FirstOrDefault(x => Path.GetFileName(x)
                        .Equals(fileName, StringComparison.OrdinalIgnoreCase));
                if (ci is not null) return ci;
            }
            catch { }
        }
        return null;
    }
}
