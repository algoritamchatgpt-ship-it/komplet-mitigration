using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// ViewModel za obrazac Samodoprinos — čita/piše ldsamod.dbf iz foldera izabrane firme.
/// Originalni FoxPro poziv: DO FORM LDSAMOD
/// Dugmad: DODAJ (APPEND BLANK), DOLE/GORE/ZADNJI/PRVI, SAČUVAJ, IZLAZ
/// </summary>
public partial class SamodoprinosViewModel : ObservableObject
{
    private string _dbfPath = "";
    private DbfOptimisticConcurrency.FileSnapshot? _snapshot;

    [ObservableProperty] private ObservableCollection<SamodoprinosStavka> _stavke = [];
    [ObservableProperty] private SamodoprinosStavka? _selektovana;
    [ObservableProperty] private string _naslov = "EVIDENCIJA SAMODOPRINOSA";
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private bool _ucitava = true;

    public SamodoprinosViewModel(string folderPath)
    {
        _dbfPath = NadjiDbfPath(folderPath);
        UcitajPodatke();
    }

    private static string NadjiDbfPath(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return string.Empty;
        var p = Path.Combine(folderPath, "ldsamod.dbf");
        if (File.Exists(p)) return p;
        return Directory.GetFiles(folderPath, "ldsamod.dbf", SearchOption.TopDirectoryOnly)
                        .FirstOrDefault() ?? p;
    }

    private void UcitajPodatke()
    {
        Ucitava = true;
        Stavke.Clear();

        if (string.IsNullOrWhiteSpace(_dbfPath))
        {
            Poruka = "Nije izabrana firma.";
            Ucitava = false;
            return;
        }

        if (!File.Exists(_dbfPath))
        {
            Poruka = $"Fajl ldsamod.dbf nije pronađen: {_dbfPath}";
            Ucitava = false;
            return;
        }

        _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(_dbfPath);

        try
        {
            var zapisi = DbfReader.CitajSveZapise(_dbfPath);
            foreach (var z in zapisi)
            {
                Stavke.Add(new SamodoprinosStavka
                {
                    SamSif   = Int(z, "SAMSIF"),
                    SamoNaz  = Str(z, "SAMONAZ"),
                    SamoProc = Dec(z, "SAMOPROC"),
                    ZiroRac  = Str(z, "ZIRORAC"),
                    SamoDop  = Dec(z, "SAMODOP"),
                    Sam1     = Dec(z, "SAM1"),
                    Sam2     = Dec(z, "SAM2"),
                    Sam3     = Dec(z, "SAM3"),
                    Sam4     = Dec(z, "SAM4"),
                    Mesec    = Int(z, "MESEC"),
                    Isplata  = Int(z, "ISPLATA"),
                    Preneto  = Str(z, "PRENETO"),
                    IdBr     = Long(z, "IDBR"),
                });
            }
            Poruka = zapisi.Count == 0
                ? "Nema unetih podataka. Koristite DODAJ."
                : $"Učitano {zapisi.Count} stavki.";
            Selektovana = Stavke.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čitanju: {ex.Message}";
        }

        Ucitava = false;
    }

    // ── Navigacija ────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Dole()
    {
        if (Stavke.Count == 0) return;
        var idx = Selektovana is null ? -1 : Stavke.IndexOf(Selektovana);
        Selektovana = idx < Stavke.Count - 1 ? Stavke[idx + 1] : Stavke[^1];
    }

    [RelayCommand]
    private void Gore()
    {
        if (Stavke.Count == 0) return;
        var idx = Selektovana is null ? Stavke.Count : Stavke.IndexOf(Selektovana);
        Selektovana = idx > 0 ? Stavke[idx - 1] : Stavke[0];
    }

    [RelayCommand]
    private void Zadnji()
    {
        if (Stavke.Count > 0) Selektovana = Stavke[^1];
    }

    [RelayCommand]
    private void Prvi()
    {
        if (Stavke.Count > 0) Selektovana = Stavke[0];
    }

    // ── CRUD ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Dodaj()
    {
        // Fox: APPEND BLANK + REPLACE SAMSIF WITH RECNO()
        var sledeci = Stavke.Count > 0 ? Stavke.Max(s => s.SamSif) + 1 : 1;
        var nova = new SamodoprinosStavka { SamSif = sledeci };
        Stavke.Add(nova);
        Selektovana = nova;
        Poruka = "Dodat nov red — popunite podatke i sačuvajte (F2).";
    }

    [RelayCommand]
    private void Obrisi()
    {
        if (Selektovana is null)
        {
            Poruka = "Nije izabrana stavka za brisanje.";
            return;
        }
        var naziv = string.IsNullOrWhiteSpace(Selektovana.SamoNaz)
            ? $"šifra {Selektovana.SamSif}"
            : Selektovana.SamoNaz.Trim();
        if (MessageBox.Show($"Obrisati samodoprinos: {naziv}?", "Brisanje",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        Stavke.Remove(Selektovana);
        Selektovana = Stavke.FirstOrDefault();
        SacuvajUDbf();
        Poruka = $"Stavka '{naziv}' obrisana i sačuvana.";
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        SacuvajUDbf();
    }

    private void SacuvajUDbf()
    {
        if (!File.Exists(_dbfPath))
        {
            Poruka = $"Ne mogu da sačuvam — ldsamod.dbf nije pronađen: {_dbfPath}";
            return;
        }

        if (_snapshot != null && DbfOptimisticConcurrency.HasFileChanged(_dbfPath, _snapshot))
        {
            var r = MessageBox.Show(
                "Fajl ldsamod.dbf je izmenjen od strane drugog korisnika.\nNastaviti sa čuvanjem (prepisati)?",
                "Upozorenje", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r != MessageBoxResult.Yes) return;
        }

        try
        {
            var schema = DbfTableWriter.LoadSchema(_dbfPath);
            var rows = Stavke.Select(s => new Dictionary<string, object?>
            {
                ["SAMSIF"]   = (decimal)s.SamSif,
                ["SAMONAZ"]  = s.SamoNaz,
                ["SAMOPROC"] = s.SamoProc,
                ["ZIRORAC"]  = s.ZiroRac,
                ["SAMODOP"]  = s.SamoDop,
                ["SAM1"]     = s.Sam1,
                ["SAM2"]     = s.Sam2,
                ["SAM3"]     = s.Sam3,
                ["SAM4"]     = s.Sam4,
                ["MESEC"]    = (decimal)s.Mesec,
                ["ISPLATA"]  = (decimal)s.Isplata,
                ["PRENETO"]  = s.Preneto,
                ["IDBR"]     = (decimal)s.IdBr,
            }).ToList();

            DbfTableWriter.WriteTable(
                _dbfPath, schema, rows,
                static (row, field) => row.TryGetValue(field, out var v) ? v : null);

            _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(_dbfPath);
            Poruka = $"Sačuvano {Stavke.Count} stavki u ldsamod.dbf.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čuvanju: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Osvezi() => UcitajPodatke();

    // ── Pomoćne ───────────────────────────────────────────────────────────────

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s : string.Empty;

    private static int Int(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0;
        return v switch { decimal d => (int)d, int i => i, long l => (int)l, _ => 0 };
    }

    private static long Long(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0L;
        return v switch { decimal d => (long)d, long l => l, int i => i, _ => 0L };
    }

    private static decimal Dec(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;
}

public class SamodoprinosStavka
{
    public int     SamSif   { get; set; }
    public string  SamoNaz  { get; set; } = string.Empty;
    public decimal SamoProc { get; set; }
    public string  ZiroRac  { get; set; } = string.Empty;
    public decimal SamoDop  { get; set; }
    public decimal Sam1     { get; set; }
    public decimal Sam2     { get; set; }
    public decimal Sam3     { get; set; }
    public decimal Sam4     { get; set; }
    public int     Mesec    { get; set; }
    public int     Isplata  { get; set; }
    public string  Preneto  { get; set; } = string.Empty;
    public long    IdBr     { get; set; }
}
