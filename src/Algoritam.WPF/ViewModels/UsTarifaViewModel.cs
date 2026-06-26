using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class UsTarifaViewModel : ObservableObject
{
    private readonly string _folderPath;
    private List<Dictionary<string, object?>> _sveStavke = [];
    private string? _dbfPath;
    private DbfTableWriter.DbfSchema? _schema;

    public string Naslov => "TARIFNIK KOMUNALNIH USLUGA";

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _stavke = [];
    [ObservableProperty] private Dictionary<string, object?>? _izabranaStavka;
    [ObservableProperty] private string _filterTekst = "";
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;

    public event Action? ZatvaranjeZahtevano;

    public UsTarifaViewModel(string folderPath)
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
            _dbfPath = NadjiDbf(_folderPath, "ustar.dbf");
            if (_dbfPath is null)
            {
                StatusPoruka = "Tabela ustar.dbf nije pronađena.";
                return;
            }
            _schema = DbfTableWriter.LoadSchema(_dbfPath);
            _sveStavke = await Task.Run(() => DbfReader.CitajSveZapise(_dbfPath));
            PrimenjiFilter();
            StatusPoruka = $"Tarifa: {_sveStavke.Count}.";
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

        var dijalogVm = new DbfUnosIzmenaViewModel(_schema, "NOVA TARIFNA STAVKA", bojaHeader: "#37474F");
        var dijalog = new Views.DbfUnosIzmenaView { DataContext = dijalogVm, Owner = vlasnik };
        dijalog.ShowDialog();

        if (!dijalogVm.Uspesno || dijalogVm.Rezultat is null) return;

        var novaSifra = dijalogVm.Rezultat.TryGetValue("SIFRA", out var s) ? s?.ToString()?.Trim() : null;
        if (string.IsNullOrWhiteSpace(novaSifra))
        {
            StatusPoruka = "Šifra mora biti uneta.";
            return;
        }
        if (_sveStavke.Any(z => string.Equals(z.GetValueOrDefault("SIFRA")?.ToString()?.Trim(), novaSifra, StringComparison.OrdinalIgnoreCase)))
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

        var dijalogVm = new DbfUnosIzmenaViewModel(_schema, "IZMENA TARIFNE STAVKE", IzabranaStavka, bojaHeader: "#37474F");
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

        var sifra = IzabranaStavka.GetValueOrDefault("SIFRA")?.ToString()?.Trim();
        if (System.Windows.MessageBox.Show($"Obrisati uslugu: {sifra}?", "Brisanje",
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
            StatusPoruka = $"Tarifa: {_sveStavke.Count}.";
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
