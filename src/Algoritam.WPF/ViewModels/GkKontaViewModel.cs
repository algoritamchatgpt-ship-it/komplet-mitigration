using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class GkKontaViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly string _dbfName;
    private string? _dbfPath;
    private DbfTableWriter.DbfSchema? _schema;

    public string Naslov { get; }

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _konti = [];
    [ObservableProperty] private Dictionary<string, object?>? _izabranKonto;
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;
    [ObservableProperty] private string _filterKonto = "";
    [ObservableProperty] private string _filterNaziv = "";

    private List<Dictionary<string, object?>> _sviKonti = [];

    public event Action? ZatvaranjeZahtevano;

    public GkKontaViewModel(string folderPath,
        string dbfName = "konto.dbf",
        string naslov = "KONTI — KONTNI PLAN")
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
            _sviKonti = await Task.Run(() => DbfReader.CitajSveZapise(_dbfPath));
            PrimenjiFilter();
            StatusPoruka = $"Ukupno: {_sviKonti.Count} konta.";
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

    partial void OnFilterKontoChanged(string value) => PrimenjiFilter();
    partial void OnFilterNazivChanged(string value) => PrimenjiFilter();

    private void PrimenjiFilter()
    {
        var konto = FilterKonto.Trim().ToLowerInvariant();
        var naziv = FilterNaziv.Trim().ToLowerInvariant();

        var filtrirani = _sviKonti.AsEnumerable();

        if (!string.IsNullOrEmpty(konto))
            filtrirani = filtrirani.Where(k =>
                k.TryGetValue("KONTO", out var v) && v?.ToString()?.ToLowerInvariant().StartsWith(konto) == true);

        if (!string.IsNullOrEmpty(naziv))
            filtrirani = filtrirani.Where(k =>
                k.TryGetValue("NAZIV", out var v) && v?.ToString()?.ToLowerInvariant().Contains(naziv) == true);

        Konti = new ObservableCollection<Dictionary<string, object?>>(filtrirani.ToList());
    }

    [RelayCommand]
    private void Osvezi() => _ = UcitajAsync();

    [RelayCommand]
    private void OcistiFilter()
    {
        FilterKonto = "";
        FilterNaziv = "";
    }

    [RelayCommand]
    private void Zatvori() => ZatvaranjeZahtevano?.Invoke();

    [RelayCommand]
    private void Dodaj(System.Windows.Window? vlasnik)
    {
        if (_dbfPath is null || _schema is null) return;

        var dijalogVm = new DbfUnosIzmenaViewModel(_schema, "NOVI KONTO", bojaHeader: "#1A237E");
        var dijalog = new Views.DbfUnosIzmenaView { DataContext = dijalogVm, Owner = vlasnik };
        dijalog.ShowDialog();

        if (!dijalogVm.Uspesno || dijalogVm.Rezultat is null) return;

        var noviKonto = dijalogVm.Rezultat.TryGetValue("KONTO", out var k) ? k?.ToString()?.Trim() : null;
        if (string.IsNullOrWhiteSpace(noviKonto))
        {
            StatusPoruka = "Konto mora biti unet.";
            return;
        }
        if (_sviKonti.Any(z => string.Equals(z.GetValueOrDefault("KONTO")?.ToString()?.Trim(), noviKonto, StringComparison.OrdinalIgnoreCase)))
        {
            StatusPoruka = $"Konto {noviKonto} već postoji.";
            return;
        }

        _sviKonti.Add(dijalogVm.Rezultat);
        SacuvajTabelu();
    }

    [RelayCommand]
    private void Izmeni(System.Windows.Window? vlasnik)
    {
        if (_dbfPath is null || _schema is null || IzabranKonto is null) return;

        var dijalogVm = new DbfUnosIzmenaViewModel(_schema, "IZMENA KONTA", IzabranKonto, bojaHeader: "#1A237E");
        var dijalog = new Views.DbfUnosIzmenaView { DataContext = dijalogVm, Owner = vlasnik };
        dijalog.ShowDialog();

        if (!dijalogVm.Uspesno || dijalogVm.Rezultat is null) return;

        var izmenjenKonto = dijalogVm.Rezultat.TryGetValue("KONTO", out var k) ? k?.ToString()?.Trim() : null;
        if (string.IsNullOrWhiteSpace(izmenjenKonto))
        {
            StatusPoruka = "Konto mora biti unet.";
            return;
        }

        var indeks = _sviKonti.IndexOf(IzabranKonto);
        if (indeks < 0) return;

        _sviKonti[indeks] = dijalogVm.Rezultat;
        SacuvajTabelu();
    }

    [RelayCommand]
    private void Obrisi()
    {
        if (_dbfPath is null || _schema is null || IzabranKonto is null) return;

        var konto = IzabranKonto.GetValueOrDefault("KONTO")?.ToString()?.Trim();
        if (System.Windows.MessageBox.Show($"Obrisati konto: {konto}?", "Brisanje",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question)
            != System.Windows.MessageBoxResult.Yes)
            return;

        _sviKonti.Remove(IzabranKonto);
        IzabranKonto = null;
        SacuvajTabelu();
    }

    private void SacuvajTabelu()
    {
        if (_dbfPath is null || _schema is null) return;
        try
        {
            DbfTableWriter.WriteTable(_dbfPath, _schema, _sviKonti,
                (red, polje) => red.GetValueOrDefault(polje));
            PrimenjiFilter();
            StatusPoruka = $"Ukupno: {_sviKonti.Count} konta.";
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
