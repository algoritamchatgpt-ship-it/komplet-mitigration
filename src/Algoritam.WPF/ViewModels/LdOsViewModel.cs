using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// ViewModel za LDOS / LDOS1 — ZSP umanjenje poreza.
/// Originalni FoxPro: DO FORM LDOS / DO FORM LDOS1
/// Dugmad: DODAJ (APPEND BLANK), PREUZIMANJE (iz LD00), BRISANJE, SAČUVAJ, OSVEZI
/// </summary>
public partial class LdOsViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly string _dbfName;
    private string _dbfPath = "";
    private DbfOptimisticConcurrency.FileSnapshot? _snapshot;

    [ObservableProperty] private ObservableCollection<LdOsStavka> _stavke = [];
    [ObservableProperty] private LdOsStavka? _selektovana;
    [ObservableProperty] private string _naslov = "";
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private bool _ucitava = true;

    public LdOsViewModel(string folderPath, string dbfName = "ldos.dbf", string naslov = "OBRAZAC ZSP — UMANJENJE POREZA")
    {
        _folderPath = folderPath;
        _dbfName = dbfName;
        Naslov = naslov;
        _dbfPath = NadjiDbf(folderPath, dbfName);
        UcitajPodatke();
    }

    private static string NadjiDbf(string folder, string ime)
    {
        if (string.IsNullOrWhiteSpace(folder)) return string.Empty;
        var p = Path.Combine(folder, ime);
        if (File.Exists(p)) return p;
        return Directory.GetFiles(folder, ime, SearchOption.TopDirectoryOnly)
                        .FirstOrDefault() ?? p;
    }

    private void UcitajPodatke()
    {
        Ucitava = true;
        Stavke.Clear();

        if (!File.Exists(_dbfPath))
        {
            Poruka = $"Fajl {_dbfName} nije pronađen u: {_folderPath}";
            Ucitava = false;
            return;
        }

        _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(_dbfPath);

        try
        {
            var zapisi = DbfReader.CitajSveZapise(_dbfPath);
            foreach (var z in zapisi)
            {
                Stavke.Add(new LdOsStavka
                {
                    Broj      = Int(z, "BROJ"),
                    ImePrez   = Str(z, "IME_PREZ"),
                    Datod     = Dat(z, "DATOD"),
                    Datdo     = Dat(z, "DATDO"),
                    Bruto     = Dec(z, "BRUTO"),
                    Doppr     = Dec(z, "DOPPR"),
                    Dopzr     = Dec(z, "DOPZR"),
                    Dopnr     = Dec(z, "DOPNR"),
                    Doppf     = Dec(z, "DOPPF"),
                    Bendin    = Dec(z, "BENDIN"),
                    Dopzf     = Dec(z, "DOPZF"),
                    Dopnf     = Dec(z, "DOPNF"),
                    Opstina   = Str(z, "OPSTINA"),
                    Mp        = Str(z, "MP"),
                    Mesec     = Int(z, "MESEC"),
                    Nazmes    = Str(z, "NAZMES"),
                    Isplata   = Int(z, "ISPLATA"),
                    MaticniBr = Str(z, "MATICNIBR"),
                    Preneto   = Str(z, "PRENETO"),
                    IdBr      = Long(z, "IDBR"),
                });
            }
            Poruka = zapisi.Count == 0 ? "Nema podataka. Koristite PREUZIMANJE ili DODAJ." : $"Učitano {zapisi.Count} stavki.";
            Selektovana = Stavke.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čitanju: {ex.Message}";
        }

        Ucitava = false;
    }

    // ── CRUD ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Dodaj()
    {
        // Fox: APPEND BLANK
        var nova = new LdOsStavka
        {
            Broj   = Stavke.Count > 0 ? Stavke.Max(s => s.Broj) + 1 : 1,
            Datod  = DateTime.Today,
            Datdo  = DateTime.Today,
        };
        Stavke.Add(nova);
        Selektovana = nova;
        Poruka = "Dodat nov red — unesite podatke i sačuvajte.";
    }

    [RelayCommand]
    private void Brisanje()
    {
        if (Selektovana is null)
        {
            Poruka = "Nije izabran red za brisanje.";
            return;
        }

        var ime = string.IsNullOrWhiteSpace(Selektovana.ImePrez) ? "red" : Selektovana.ImePrez.Trim();
        if (MessageBox.Show($"Obrisati: {ime}?", "Brisanje",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            Stavke.Remove(Selektovana);
            Selektovana = Stavke.FirstOrDefault();
            SacuvajUDbf();
            Poruka = "Red je obrisan i sačuvan.";
        }
    }

    // ── Preuzimanje iz LD00 ──────────────────────────────────────────────────
    // Fox: čita LD00 (platni spisak) i kopira u ldos/ldos1 polja za OS obrazac

    [RelayCommand]
    private void Preuzimanje()
    {
        var ld00Path = NadjiDbf(_folderPath, "ld00.dbf");
        if (!File.Exists(ld00Path))
        {
            Poruka = "Fajl ld00.dbf nije pronađen — otvorite Platni spisak.";
            return;
        }

        // Datum od/do iz LDPARAM (DATOD1 / DATDO1)
        DateTime? datod = null, datdo = null;
        var paramPath = NadjiDbf(_folderPath, "ldparam.dbf");
        if (File.Exists(paramPath))
        {
            var par = DbfReader.CitajSveZapise(paramPath).FirstOrDefault();
            if (par != null)
            {
                datod = Dat(par, "DATOD1");
                datdo = Dat(par, "DATDO1");
            }
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(ld00Path);
            if (zapisi.Count == 0)
            {
                Poruka = "Platni spisak (ld00.dbf) je prazan.";
                return;
            }

            Stavke.Clear();
            int numred = 1;
            foreach (var z in zapisi)
            {
                Stavke.Add(new LdOsStavka
                {
                    Broj      = Int(z, "BROJ"),
                    ImePrez   = Str(z, "IME_PREZ"),
                    Datod     = datod ?? Dat(z, "DATOD"),
                    Datdo     = datdo ?? Dat(z, "DATDO"),
                    Bruto     = Dec(z, "BRUTO"),
                    Doppr     = Dec(z, "DOPPR"),
                    Dopzr     = Dec(z, "DOPZR"),
                    Dopnr     = Dec(z, "DOPNR"),
                    Doppf     = Dec(z, "DOPPF"),
                    Bendin    = Dec(z, "BENDIN"),
                    Dopzf     = Dec(z, "DOPZF"),
                    Dopnf     = Dec(z, "DOPNF"),
                    Opstina   = Str(z, "OPSTINA"),
                    Mp        = Str(z, "MP"),
                    Mesec     = Int(z, "MESEC"),
                    Nazmes    = Str(z, "NAZMES"),
                    Isplata   = Int(z, "ISPLATA"),
                    MaticniBr = Str(z, "MATICNIBR"),
                    IdBr      = Long(z, "IDBR"),
                    Preneto   = Str(z, "PRENETO"),
                });
                numred++;
            }

            SacuvajUDbf();
            Poruka = $"Preuzeto {Stavke.Count} radnika iz platnog spiska.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri preuzimanju: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Sacuvaj() => SacuvajUDbf();

    [RelayCommand]
    private void Osvezi() => UcitajPodatke();

    // ── Čuvanje u DBF ─────────────────────────────────────────────────────────

    private void SacuvajUDbf()
    {
        if (!File.Exists(_dbfPath))
        {
            Poruka = $"Ne mogu da sačuvam — {_dbfName} nije pronađen.";
            return;
        }

        if (_snapshot != null && DbfOptimisticConcurrency.HasFileChanged(_dbfPath, _snapshot))
        {
            var r = MessageBox.Show(
                $"Fajl {_dbfName} je izmenjen od strane drugog korisnika. Nastaviti?",
                "Upozorenje", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r != MessageBoxResult.Yes) return;
        }

        try
        {
            var schema = DbfTableWriter.LoadSchema(_dbfPath);
            var rows = Stavke.Select(s => new Dictionary<string, object?>
            {
                ["BROJ"]      = (decimal)s.Broj,
                ["IME_PREZ"]  = s.ImePrez,
                ["DATOD"]     = s.Datod,
                ["DATDO"]     = s.Datdo,
                ["BRUTO"]     = s.Bruto,
                ["DOPPR"]     = s.Doppr,
                ["DOPZR"]     = s.Dopzr,
                ["DOPNR"]     = s.Dopnr,
                ["DOPPF"]     = s.Doppf,
                ["BENDIN"]    = s.Bendin,
                ["DOPZF"]     = s.Dopzf,
                ["DOPNF"]     = s.Dopnf,
                ["OPSTINA"]   = s.Opstina,
                ["MP"]        = s.Mp,
                ["MESEC"]     = (decimal)s.Mesec,
                ["NAZMES"]    = s.Nazmes,
                ["ISPLATA"]   = (decimal)s.Isplata,
                ["MATICNIBR"] = s.MaticniBr,
                ["PRENETO"]   = s.Preneto,
                ["IDBR"]      = (decimal)s.IdBr,
            }).ToList();

            DbfTableWriter.WriteTable(
                _dbfPath, schema, rows,
                static (row, field) => row.TryGetValue(field, out var v) ? v : null);

            _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(_dbfPath);
            Poruka = $"Sačuvano {Stavke.Count} stavki u {_dbfName}.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čuvanju: {ex.Message}";
        }
    }

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
    private static DateTime? Dat(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is DateTime dt ? dt : null;
}

public class LdOsStavka
{
    public int       Broj      { get; set; }
    public string    ImePrez   { get; set; } = string.Empty;
    public DateTime? Datod     { get; set; }
    public DateTime? Datdo     { get; set; }
    public decimal   Bruto     { get; set; }
    public decimal   Doppr     { get; set; }
    public decimal   Dopzr     { get; set; }
    public decimal   Dopnr     { get; set; }
    public decimal   Doppf     { get; set; }
    public decimal   Bendin    { get; set; }
    public decimal   Dopzf     { get; set; }
    public decimal   Dopnf     { get; set; }
    public string    Opstina   { get; set; } = string.Empty;
    public string    Mp        { get; set; } = string.Empty;
    public int       Mesec     { get; set; }
    public string    Nazmes    { get; set; } = string.Empty;
    public int       Isplata   { get; set; }
    public string    MaticniBr { get; set; } = string.Empty;
    public string    Preneto   { get; set; } = string.Empty;
    public long      IdBr      { get; set; }
}
