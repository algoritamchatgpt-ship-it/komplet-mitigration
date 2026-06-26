using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using Algoritam.WPF.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Fox forma LDZ1 — zahtev za naknadu plate za vreme porodiljskog odsustva.
/// Tabela ldz1.dbf: br, opis, iznos, podatak, rlini, preneto, idbr
/// </summary>
public partial class LdZ1ViewModel : ObservableObject
{
    private readonly string _folderPath;
    private string? _dbfPath;
    private DbfTableWriter.DbfSchema? _schema;

    [ObservableProperty] private ObservableCollection<LdZ1Stavka> _stavke = [];
    [ObservableProperty] private LdZ1Stavka? _selektovana;
    [ObservableProperty] private string _naslov = "ZAHTEV ZA NAKNADU PLATE — PORODILJSKO ODSUSTVO (Z-1)";
    [ObservableProperty] private string _poruka = string.Empty;

    public event Action? ZatvaranjeZatrazeno;

    public LdZ1ViewModel(AppState appState)
    {
        _folderPath = appState.AktivnaFirma?.FolderPath ?? string.Empty;
        Ucitaj();
    }

    [RelayCommand]
    private void Osvezi() => Ucitaj();

    [RelayCommand]
    private void Dodaj()
    {
        var nova = new LdZ1Stavka
        {
            Br = (Stavke.Count + 1).ToString("D2", CultureInfo.InvariantCulture)
        };
        Stavke.Add(nova);
        Selektovana = nova;
        Poruka = "Dodat je novi red.";
    }

    [RelayCommand]
    private void BrisanjeReda()
    {
        if (Selektovana is null)
        {
            Poruka = "Izaberite red za brisanje.";
            return;
        }

        Stavke.Remove(Selektovana);
        Selektovana = Stavke.Count > 0 ? Stavke[Math.Max(0, Stavke.Count - 1)] : null;
        Poruka = "Red je obrisan.";
    }

    [RelayCommand]
    private void Brisi()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Tabela je vec prazna.";
            return;
        }

        Stavke.Clear();
        Selektovana = null;
        Sacuvaj();
        Poruka = "Svi redovi su obrisani.";
    }

    [RelayCommand]
    private void Preuzimanje()
    {
        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        var ldPath = PronadjiDbf(_folderPath, "ld00.dbf", "ld.dbf");
        var ldradPath = PronadjiDbf(_folderPath, "ldrad.dbf");
        if (ldPath is null || ldradPath is null)
        {
            Poruka = "Nedostaju LD/LDRAD tabele.";
            return;
        }

        try
        {
            var ldZapisi = DbfReader.CitajSveZapise(ldPath);
            var radnici = DbfReader.CitajSveZapise(ldradPath);
            var imePoBroju = radnici
                .GroupBy(r => Int(r, "BROJ"))
                .ToDictionary(g => g.Key, g => g.First());

            int redBr = Stavke.Count + 1;
            int dodato = 0;

            foreach (var z in ldZapisi)
            {
                int broj = Int(z, "BROJ");
                string imePrez = imePoBroju.TryGetValue(broj, out var rad)
                    ? Str(rad, "IME_PREZ")
                    : Str(z, "IME_PREZ");

                decimal bruto = Dec(z, "BRUTO");
                decimal neto = Dec(z, "NETO");

                if (bruto == 0m && neto == 0m)
                    continue;

                Stavke.Add(new LdZ1Stavka
                {
                    Br = (redBr++).ToString("D2", CultureInfo.InvariantCulture),
                    Opis = imePrez.Length > 59 ? imePrez[..59] : imePrez,
                    Iznos = bruto,
                    Idbr = (long)Int(z, "IDBR")
                });
                dodato++;
            }

            Poruka = $"Preuzeto {dodato} stavki iz platnog spiska.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri preuzimanju: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        if (string.IsNullOrWhiteSpace(_folderPath))
            return;

        _dbfPath ??= PronadjiDbf(_folderPath, "ldz1.dbf");
        if (_dbfPath is null)
        {
            Poruka = "ldz1.dbf nije pronađen.";
            return;
        }

        try
        {
            _schema ??= DbfTableWriter.LoadSchema(_dbfPath);
            var redovi = Stavke.Select(s => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["BR"]      = (s.Br ?? string.Empty).Trim(),
                ["OPIS"]    = (s.Opis ?? string.Empty).Trim(),
                ["IZNOS"]   = s.Iznos,
                ["PODATAK"] = (s.Podatak ?? string.Empty).Trim(),
                ["RLINI"]   = (s.Rlini ?? string.Empty).Trim(),
                ["PRENETO"] = (s.Preneto ?? string.Empty).Trim(),
                ["IDBR"]    = (decimal)s.Idbr
            }).ToList();

            DbfTableWriter.WriteTable(
                _dbfPath, _schema, redovi,
                static (r, f) => r.TryGetValue(f, out var v) ? v : null);

            Poruka = $"Sačuvano {redovi.Count} stavki.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri cuvanju: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Pregled()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za pregled.";
            return;
        }

        var redovi = Stavke.Select(s => new PregledTabelaStavka
        {
            Sifra = s.Br ?? string.Empty,
            Naziv = s.Opis ?? string.Empty,
            Iznos1 = s.Iznos,
            Iznos2 = 0m
        }).ToList();

        var view = new Views.Zarade.FoxPregledTabelaView(
            "ZAHTEV Z-1",
            "Zahtev za naknadu plate za vreme porodiljskog odsustva",
            redovi,
            "IZNOS",
            string.Empty);
        view.ShowDialog();
    }

    [RelayCommand]
    private void Zatvori() => ZatvaranjeZatrazeno?.Invoke();

    private void Ucitaj()
    {
        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        _dbfPath = PronadjiDbf(_folderPath, "ldz1.dbf");
        if (_dbfPath is null)
        {
            Stavke = [];
            Poruka = "ldz1.dbf nije pronađen — tabela je prazna.";
            return;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(_dbfPath);
            var lista = zapisi.Select(z => new LdZ1Stavka
            {
                Br      = Str(z, "BR"),
                Opis    = Str(z, "OPIS"),
                Iznos   = Dec(z, "IZNOS"),
                Podatak = Str(z, "PODATAK"),
                Rlini   = Str(z, "RLINI"),
                Preneto = Str(z, "PRENETO"),
                Idbr    = Long(z, "IDBR")
            }).ToList();

            Stavke = new ObservableCollection<LdZ1Stavka>(lista);
            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Ucitano {Stavke.Count} stavki.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri ucitavanju: {ex.Message}";
        }
    }

    private static string? PronadjiDbf(string folder, params string[] names)
    {
        foreach (var name in names)
        {
            var path = Path.Combine(folder, name);
            if (File.Exists(path))
                return path;

            var upper = Path.Combine(folder, name.ToUpperInvariant());
            if (File.Exists(upper))
                return upper;
        }
        return null;
    }

    private static string Str(Dictionary<string, object?> z, string k)
        => z.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;

    private static int Int(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0;
        if (v is decimal d) return (int)d;
        if (v is int i) return i;
        if (int.TryParse(v.ToString(), out var p)) return p;
        return 0;
    }

    private static long Long(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0L;
        if (v is decimal d) return (long)d;
        if (v is long l) return l;
        if (long.TryParse(v.ToString(), out var p)) return p;
        return 0L;
    }

    private static decimal Dec(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0m;
        if (v is decimal d) return d;
        if (decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var p)) return p;
        return 0m;
    }
}

public partial class LdZ1Stavka : ObservableObject
{
    [ObservableProperty] private string _br = string.Empty;
    [ObservableProperty] private string _opis = string.Empty;
    [ObservableProperty] private decimal _iznos;
    [ObservableProperty] private string _podatak = string.Empty;
    [ObservableProperty] private string _rlini = string.Empty;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private long _idbr;
}
