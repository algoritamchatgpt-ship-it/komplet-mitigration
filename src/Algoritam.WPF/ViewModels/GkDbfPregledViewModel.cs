using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Generički ViewModel za pregled DBF tabele — koristi se za sve GK šifrarnike i preglede.
/// Ekvivalent FoxPro BROWSE komande za read-only prikaz tabele.
/// </summary>
public partial class GkDbfPregledViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly string _dbfName;
    private readonly string _bojaHeaderHex;
    private string? _dbfPath;
    private DbfTableWriter.DbfSchema? _schema;

    public string Naslov { get; }
    public bool DozvoljenaIzmena { get; }

    public Brush BojaHeaderBrush { get; }
    public Brush BojaLightBrush  { get; }

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _stavke = [];
    [ObservableProperty] private Dictionary<string, object?>? _izabranaStavka;
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;
    [ObservableProperty] private string _filterTekst = "";
    [ObservableProperty] private IReadOnlyList<string> _kolone = [];

    private List<Dictionary<string, object?>> _sveStavke = [];

    public event Action? ZatvaranjeZahtevano;

    public GkDbfPregledViewModel(string folderPath, string dbfName, string naslov,
        string bojaHeader = "#1A237E", string bojaLight = "#E8EAF6", bool dozvoliIzmenu = false)
    {
        _folderPath = folderPath;
        _dbfName = dbfName;
        _bojaHeaderHex = bojaHeader;
        Naslov = naslov;
        DozvoljenaIzmena = dozvoliIzmenu;
        BojaHeaderBrush = BrushIzHex(bojaHeader);
        BojaLightBrush  = BrushIzHex(bojaLight);
        _ = UcitajAsync();
    }

    private static SolidColorBrush BrushIzHex(string hex)
    {
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        catch { return new SolidColorBrush(Colors.DarkBlue); }
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
                StatusPoruka = $"Tabela {_dbfName} nije pronađena u folderu firme.";
                return;
            }

            if (DozvoljenaIzmena)
                _schema = DbfTableWriter.LoadSchema(_dbfPath);
            _sveStavke = await Task.Run(() => DbfReader.CitajSveZapise(_dbfPath));
            if (_sveStavke.Count > 0)
                Kolone = _sveStavke[0].Keys.ToList();

            PrimenjiFilter();
            StatusPoruka = $"Ukupno: {_sveStavke.Count} zapisa.";
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
        if (string.IsNullOrWhiteSpace(FilterTekst))
        {
            Stavke = new ObservableCollection<Dictionary<string, object?>>(_sveStavke);
            return;
        }

        var upit = FilterTekst.ToLowerInvariant();
        var filtrirane = _sveStavke.Where(s =>
            s.Values.Any(v => v?.ToString()?.ToLowerInvariant().Contains(upit) == true)).ToList();
        Stavke = new ObservableCollection<Dictionary<string, object?>>(filtrirane);
    }

    [RelayCommand]
    private void Osvezi() => _ = UcitajAsync();

    [RelayCommand]
    private void Zatvori() => ZatvaranjeZahtevano?.Invoke();

    [RelayCommand]
    private void Dodaj(System.Windows.Window? vlasnik)
    {
        if (!DozvoljenaIzmena || _dbfPath is null || _schema is null) return;

        var dijalogVm = new DbfUnosIzmenaViewModel(_schema, $"NOVI ZAPIS — {Naslov}", bojaHeader: _bojaHeaderHex);
        var dijalog = new Views.DbfUnosIzmenaView { DataContext = dijalogVm, Owner = vlasnik };
        dijalog.ShowDialog();

        if (!dijalogVm.Uspesno || dijalogVm.Rezultat is null) return;

        if (Kolone.Count > 0)
        {
            var kljucPolje = Kolone[0];
            var novaVrednost = dijalogVm.Rezultat.GetValueOrDefault(kljucPolje)?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(novaVrednost) &&
                _sveStavke.Any(z => string.Equals(z.GetValueOrDefault(kljucPolje)?.ToString()?.Trim(), novaVrednost, StringComparison.OrdinalIgnoreCase)))
            {
                StatusPoruka = $"Zapis sa {kljucPolje}={novaVrednost} već postoji.";
                return;
            }
        }

        _sveStavke.Add(dijalogVm.Rezultat);
        SacuvajTabelu();
    }

    [RelayCommand]
    private void Izmeni(System.Windows.Window? vlasnik)
    {
        if (!DozvoljenaIzmena || _dbfPath is null || _schema is null || IzabranaStavka is null) return;

        var dijalogVm = new DbfUnosIzmenaViewModel(_schema, $"IZMENA — {Naslov}", IzabranaStavka, bojaHeader: _bojaHeaderHex);
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
        if (!DozvoljenaIzmena || _dbfPath is null || _schema is null || IzabranaStavka is null) return;

        var opis = Kolone.Count > 0 ? IzabranaStavka.GetValueOrDefault(Kolone[0])?.ToString()?.Trim() : "zapis";
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
            StatusPoruka = $"Ukupno: {_sveStavke.Count} zapisa.";
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
