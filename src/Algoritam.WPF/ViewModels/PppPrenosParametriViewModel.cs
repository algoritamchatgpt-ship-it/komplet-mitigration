using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

public partial class PppPrenosParametriViewModel : ObservableObject
{
    private readonly string _folderPath;
    private string? _dbfPath;
    private DbfTableWriter.DbfSchema? _schema;

    [ObservableProperty] private ObservableCollection<PppPrenosParametarStavka> _stavke = [];
    [ObservableProperty] private PppPrenosParametarStavka? _selektovanaStavka;
    [ObservableProperty] private string _poruka = string.Empty;

    public Action? ZatvoriAction { get; set; }

    public PppPrenosParametriViewModel(string folderPath)
    {
        _folderPath = folderPath ?? string.Empty;
        Ucitaj();
    }

    [RelayCommand]
    private void Osvezi() => Ucitaj();

    [RelayCommand]
    private void Sacuvaj()
    {
        if (string.IsNullOrWhiteSpace(_dbfPath))
        {
            Poruka = "Nije pronađena putanja za xm2zarpr.dbf.";
            return;
        }

        _schema ??= DbfTableWriter.LoadSchema(_dbfPath);

        var rows = Stavke.Select(s => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["OPIS"] = Safe(s.Opis),
            ["ISPLATA"] = s.Isplata,
            ["MESEC"] = s.Mesec,
            ["TABELA"] = Safe(s.Tabela),
            ["TABELA2"] = Safe(s.Tabela2),
            ["PRENETO"] = Safe(s.Preneto),
            ["IDBR"] = s.Idbr <= 0 ? 0m : s.Idbr
        }).ToList();

        DbfTableWriter.WriteTable(
            _dbfPath,
            _schema,
            rows,
            static (r, fieldName) => r.TryGetValue(fieldName, out var value) ? value : null);

        Poruka = "Parametri prenosa su sačuvani.";
    }

    [RelayCommand]
    private void Dodaj()
    {
        var nextId = Stavke.Count == 0 ? 1 : Stavke.Max(s => s.Idbr) + 1;
        var nova = new PppPrenosParametarStavka
        {
            Opis = "Novi prenos",
            Isplata = 1,
            Mesec = 0,
            Tabela = "ld",
            Tabela2 = "ldrad",
            Preneto = string.Empty,
            Idbr = nextId
        };
        Stavke.Add(nova);
        SelektovanaStavka = nova;
        Poruka = "Dodat je novi red.";
    }

    [RelayCommand]
    private void Obrisi()
    {
        if (SelektovanaStavka is null)
        {
            Poruka = "Izaberite red za brisanje.";
            return;
        }

        Stavke.Remove(SelektovanaStavka);
        SelektovanaStavka = Stavke.Count > 0 ? Stavke[0] : null;
        Poruka = "Red je obrisan.";
    }

    [RelayCommand]
    private void ObrisiSve()
    {
        var potvrda = MessageBox.Show(
            "Obrisati sve stavke parametara prenosa?",
            "Parametri prenosa",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (potvrda != MessageBoxResult.Yes)
            return;

        Stavke.Clear();
        SelektovanaStavka = null;
        Poruka = "Sve stavke su obrisane.";
    }

    [RelayCommand]
    private void Prvi()
    {
        if (Stavke.Count > 0)
            SelektovanaStavka = Stavke[0];
    }

    [RelayCommand]
    private void Zadnji()
    {
        if (Stavke.Count > 0)
            SelektovanaStavka = Stavke[^1];
    }

    [RelayCommand]
    private void Gore()
    {
        if (Stavke.Count == 0 || SelektovanaStavka is null)
            return;

        var idx = Stavke.IndexOf(SelektovanaStavka);
        if (idx > 0)
            SelektovanaStavka = Stavke[idx - 1];
    }

    [RelayCommand]
    private void Dole()
    {
        if (Stavke.Count == 0 || SelektovanaStavka is null)
            return;

        var idx = Stavke.IndexOf(SelektovanaStavka);
        if (idx >= 0 && idx < Stavke.Count - 1)
            SelektovanaStavka = Stavke[idx + 1];
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriAction?.Invoke();

    private void Ucitaj()
    {
        Stavke.Clear();

        _dbfPath = PronadjiIliKreirajDbf("xm2zarpr.dbf");
        if (string.IsNullOrWhiteSpace(_dbfPath) || !File.Exists(_dbfPath))
        {
            Poruka = "Nije pronađen xm2zarpr.dbf.";
            return;
        }

        _schema = DbfTableWriter.LoadSchema(_dbfPath);
        var rows = DbfReader.CitajSveZapise(_dbfPath);

        if (rows.Count == 0)
        {
            rows = KreirajPodrazumevaneStavke()
                .Select(s => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["OPIS"] = s.Opis,
                    ["ISPLATA"] = s.Isplata,
                    ["MESEC"] = s.Mesec,
                    ["TABELA"] = s.Tabela,
                    ["TABELA2"] = s.Tabela2,
                    ["PRENETO"] = s.Preneto,
                    ["IDBR"] = s.Idbr
                })
                .ToList();

            DbfTableWriter.WriteTable(
                _dbfPath,
                _schema,
                rows,
                static (r, fieldName) => r.TryGetValue(fieldName, out var value) ? value : null);
        }

        foreach (var row in rows)
        {
            Stavke.Add(new PppPrenosParametarStavka
            {
                Opis = Str(row, "OPIS"),
                Isplata = Int(row, "ISPLATA"),
                Mesec = Int(row, "MESEC"),
                Tabela = Str(row, "TABELA"),
                Tabela2 = Str(row, "TABELA2"),
                Preneto = Str(row, "PRENETO"),
                Idbr = Dec(row, "IDBR")
            });
        }

        SelektovanaStavka = Stavke.Count > 0 ? Stavke[0] : null;
        Poruka = $"Ucitano {Stavke.Count} stavki.";
    }

    private string? PronadjiIliKreirajDbf(string fileName)
    {
        var found = PronadjiDbf(_folderPath, fileName);
        if (!string.IsNullOrWhiteSpace(found) && File.Exists(found))
            return found;

        if (string.IsNullOrWhiteSpace(_folderPath))
            return null;

        var targetPath = Path.Combine(_folderPath, fileName);
        var template = PronadjiTemplateDbf(fileName);
        if (template is null)
            return null;

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.Copy(template, targetPath, overwrite: true);
        return targetPath;
    }

    private static string? PronadjiTemplateDbf(string fileName)
    {
        foreach (var root in KandidatiZaRoot())
        {
            var kandidati = new[]
            {
                Path.Combine(root, "newproject", "templates", "F1", fileName),
                Path.Combine(root, "newproject", "instalacije", "AlgoritamOffice", "templates", "F1", fileName),
                Path.Combine(root, "instalacije", "AlgoritamOffice", "templates", "F1", fileName)
            };

            foreach (var kandidat in kandidati)
            {
                if (File.Exists(kandidat))
                    return kandidat;
            }
        }

        return null;
    }

    private static IEnumerable<string> KandidatiZaRoot()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        DodajParents(roots, AppContext.BaseDirectory);
        DodajParents(roots, Environment.CurrentDirectory);
        return roots;

        static void DodajParents(HashSet<string> target, string? startPath)
        {
            if (string.IsNullOrWhiteSpace(startPath))
                return;

            var info = new DirectoryInfo(startPath);
            while (info != null)
            {
                target.Add(info.FullName);
                info = info.Parent;
            }
        }
    }

    private static List<PppPrenosParametarStavka> KreirajPodrazumevaneStavke()
    {
        var rows = new List<(string tabela, int isplata, string tabela2, string opis)>
        {
            ("ld", 1, "ldrad", "REDOVNA ZARADA"),
            ("ld", 2, "ldrad", "PORODILJSKO BOLOVANJE"),
            ("ld", 3, "ldrad", "BOLOVANJE PREKO 30 DANA"),
            ("ld", 4, "ldrad", "INVALIDI"),
            ("ld", 5, "ldrad", "ZAPOSLENI PENZIONERI"),
            ("ld", 6, "ldrad", "UGOVOR O PRIV.I POV.POSLOVIMA"),
            ("ldprev", 1, "ldrad", "PREVOZ RADNIKA"),
            ("ldopj1n", 1, "ldrad", "OBRAZAC OPJ-1"),
            ("ldopj2", 1, "ldrad0", "OBRAZAC OPJ-2"),
            ("ldopj3", 1, "ldrad0", "OBRAZAC OPJ-3"),
            ("ldopj4", 1, "ldrad0", "OBRAZAC OPJ-4"),
            ("ldopj5", 1, "ldrad0", "OBRAZAC OPJ-5"),
            ("ldopj6", 1, "ldrad0", "OBRAZAC OPJ-6"),
            ("ldopj7", 1, "ldrad0", "OBRAZAC OPJ-7"),
            ("ldopj8", 1, "ldrad0", "OBRAZAC OPJ-8"),
            ("ldppodp", 1, "ldrad0", "OBRAZAC PP OD P"),
            ("ldppodo", 1, "ldrad0", "OBRAZAC PP OD O")
        };

        var result = new List<PppPrenosParametarStavka>(rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            result.Add(new PppPrenosParametarStavka
            {
                Opis = row.opis,
                Isplata = row.isplata,
                Mesec = 0,
                Tabela = row.tabela,
                Tabela2 = row.tabela2,
                Preneto = string.Empty,
                Idbr = i + 1
            });
        }

        return result;
    }

    private static string? PronadjiDbf(string folderPath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return null;

        var kandidati = new List<string>();
        if (Directory.Exists(folderPath))
        {
            kandidati.Add(folderPath);
            kandidati.Add(Path.Combine(folderPath, "zarade"));
            kandidati.Add(Path.Combine(folderPath, "data00"));
            kandidati.Add(Path.Combine(folderPath, "01"));
            kandidati.Add(Path.Combine(folderPath, "data01"));
        }

        var parent = Directory.Exists(folderPath) ? Directory.GetParent(folderPath)?.FullName : null;
        if (!string.IsNullOrWhiteSpace(parent))
        {
            kandidati.Add(parent);
            kandidati.Add(Path.Combine(parent, "zarade"));
            kandidati.Add(Path.Combine(parent, "data00"));
            kandidati.Add(Path.Combine(parent, "01"));
            kandidati.Add(Path.Combine(parent, "data01"));
        }

        foreach (var kandidat in kandidati.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(kandidat) || !Directory.Exists(kandidat))
                continue;

            var exact = Path.Combine(kandidat, fileName);
            if (File.Exists(exact))
                return exact;

            var found = Directory.GetFiles(kandidat, "*.dbf", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(found))
                return found;
        }

        return null;
    }

    private static string Safe(string? value) => (value ?? string.Empty).Trim();

    private static string Str(Dictionary<string, object?> row, string fieldName)
        => row.TryGetValue(fieldName, out var value) ? (value?.ToString() ?? string.Empty).Trim() : string.Empty;

    private static int Int(Dictionary<string, object?> row, string fieldName)
    {
        if (!row.TryGetValue(fieldName, out var value) || value is null)
            return 0;

        if (value is decimal d)
            return (int)d;

        return int.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    private static decimal Dec(Dictionary<string, object?> row, string fieldName)
    {
        if (!row.TryGetValue(fieldName, out var value) || value is null)
            return 0m;

        if (value is decimal d)
            return d;

        var text = value.ToString() ?? string.Empty;
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : (decimal.TryParse(text, out parsed) ? parsed : 0m);
    }
}

public partial class PppPrenosParametarStavka : ObservableObject
{
    [ObservableProperty] private string _opis = string.Empty;
    [ObservableProperty] private int _isplata;
    [ObservableProperty] private int _mesec;
    [ObservableProperty] private string _tabela = string.Empty;
    [ObservableProperty] private string _tabela2 = string.Empty;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private decimal _idbr;
}

