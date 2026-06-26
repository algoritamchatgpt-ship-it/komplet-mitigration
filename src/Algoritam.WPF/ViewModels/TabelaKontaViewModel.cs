using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class TabelaKontaViewModel : ObservableObject
{
    private readonly string _folderPath;

    [ObservableProperty] private ObservableCollection<TabelaKontaStavka> _stavke = [];
    [ObservableProperty] private string _naslov = "SIFARNIK KONTA ZA KNJIZENJE ZARADA";
    [ObservableProperty] private string _poruka = string.Empty;
    [ObservableProperty] private bool _ucitava;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ObrisiCommand))]
    private TabelaKontaStavka? _selektovana;

    public TabelaKontaViewModel(string folderPath)
    {
        _folderPath = folderPath;
        UcitajPodatke();
    }

    private void UcitajPodatke()
    {
        Ucitava = true;
        Stavke.Clear();

        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            Ucitava = false;
            return;
        }

        var dbfPath = NadjiDbf(_folderPath, "ldkon00.dbf");
        if (dbfPath == null)
        {
            Poruka = $"Fajl ldkon00.dbf nije pronađen u: {_folderPath}";
            Ucitava = false;
            return;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);
            foreach (var z in zapisi)
                Stavke.Add(MapirajStavku(z));

            Poruka = Stavke.Count > 0
                ? $"Ucitano {Stavke.Count} konta."
                : "Nema unetih podataka za tabelu konta.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri citanju: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    [RelayCommand]
    private void Osvezi() => UcitajPodatke();

    [RelayCommand]
    private async Task Dodaj()
    {
        var nova = new TabelaKontaStavka();
        Stavke.Add(nova);
        Selektovana = nova;

        if (await SacuvajCoreAsync(prikaziPorukuUspeha: false))
            Poruka = "Dodata je nova stavka.";
    }

    private bool MozeObrisi() => Selektovana != null;

    [RelayCommand(CanExecute = nameof(MozeObrisi))]
    private async Task Obrisi()
    {
        if (Selektovana == null)
            return;

        var za = Selektovana;
        Stavke.Remove(za);
        Selektovana = Stavke.FirstOrDefault();

        if (await SacuvajCoreAsync(prikaziPorukuUspeha: false))
            Poruka = $"Obrisana je stavka '{za.Kod}'.";
    }

    [RelayCommand]
    private async Task BrisiSve()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema stavki za brisanje.";
            return;
        }

        Stavke.Clear();
        Selektovana = null;

        if (await SacuvajCoreAsync(prikaziPorukuUspeha: false))
            Poruka = "Sve stavke su obrisane.";
    }

    public Task<bool> SacuvajBezPorukeAsync() => SacuvajCoreAsync(prikaziPorukuUspeha: false);

    private async Task<bool> SacuvajCoreAsync(bool prikaziPorukuUspeha)
    {
        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return false;
        }

        var dbfPath = NadjiDbf(_folderPath, "ldkon00.dbf");
        if (dbfPath == null)
        {
            Poruka = "ldkon00.dbf nije pronađen - nije moguće sačuvati.";
            return false;
        }

        try
        {
            var schema = DbfTableWriter.LoadSchema(dbfPath);
            var redovi = Stavke.Select(s => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["VRSTA"]   = s.Vrsta?.Trim() ?? string.Empty,
                ["KOD"]     = s.Kod?.Trim() ?? string.Empty,
                ["OPIS"]    = s.Opis?.Trim() ?? string.Empty,
                ["KONTO"]   = s.Konto?.Trim() ?? string.Empty,
                ["KONTOP"]  = s.KontoP?.Trim() ?? string.Empty,
                ["PRENETO"] = s.Preneto?.Trim() ?? string.Empty,
                ["IDBR"]    = (decimal)s.IdBr
            }).ToList();

            await Task.Run(() => DbfTableWriter.WriteTable(
                dbfPath, schema, redovi,
                static (r, f) => r.TryGetValue(f, out var v) ? v : null));

            if (prikaziPorukuUspeha)
                Poruka = $"Sačuvano {redovi.Count} konta u ldkon00.dbf.";

            return true;
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri cuvanju: {ex.Message}";
            return false;
        }
    }

    [RelayCommand]
    private async Task PrenosIzBaznihKonta()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Izaberite izvorni ldkon00.dbf (bazna konta)",
            Filter = "DBF fajlovi (*.dbf)|*.dbf|Svi fajlovi|*.*",
            FileName = "ldkon00.dbf"
        };

        if (dlg.ShowDialog() != true)
            return;

        var izvorPath = dlg.FileName;
        try
        {
            var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(izvorPath));
            if (zapisi.Count == 0)
            {
                Poruka = "Izabrani fajl ne sadrzi zapise.";
                return;
            }

            var prviZapis = zapisi[0];
            if (!prviZapis.ContainsKey("KOD") || !prviZapis.ContainsKey("KONTO"))
            {
                Poruka = "Izabrani fajl nema strukturu konta sifarnika (nedostaje KOD/KONTO polje).";
                return;
            }

            Stavke.Clear();
            foreach (var z in zapisi)
                Stavke.Add(MapirajStavku(z));

            Selektovana = Stavke.FirstOrDefault();

            if (await SacuvajCoreAsync(prikaziPorukuUspeha: false))
                Poruka = $"Preneto {Stavke.Count} konta iz baznog sifarnika.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri prenosu: {ex.Message}";
        }
    }

    private static TabelaKontaStavka MapirajStavku(Dictionary<string, object?> z) => new()
    {
        Vrsta   = Str(z, "VRSTA"),
        Kod     = Str(z, "KOD"),
        Opis    = Str(z, "OPIS"),
        Konto   = Str(z, "KONTO"),
        KontoP  = Str(z, "KONTOP"),
        Preneto = Str(z, "PRENETO"),
        IdBr    = Long(z, "IDBR"),
    };

    private static string? NadjiDbf(string folder, string ime)
    {
        if (string.IsNullOrWhiteSpace(folder)) return null;
        var p = Path.Combine(folder, ime);
        if (File.Exists(p)) return p;
        var found = Directory.GetFiles(folder, ime, SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (found != null) return found;
        // FoxPro koristio data00 u root folderu — fallback za FIN rezim
        try
        {
            var parent = Path.GetDirectoryName(folder);
            if (parent is null) return null;
            var data00 = Path.Combine(parent, "data00");
            if (!Directory.Exists(data00)) return null;
            var p2 = Path.Combine(data00, ime);
            if (File.Exists(p2)) return p2;
            return Directory.GetFiles(data00, ime, SearchOption.TopDirectoryOnly).FirstOrDefault();
        }
        catch { return null; }
    }

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;

    private static long Long(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v == null) return 0L;
        if (v is decimal d) return (long)d;
        if (v is long l) return l;
        if (long.TryParse(v.ToString(), out var p)) return p;
        return 0L;
    }
}

public partial class TabelaKontaStavka : ObservableObject
{
    [ObservableProperty] private string _vrsta   = string.Empty;
    [ObservableProperty] private string _kod     = string.Empty;
    [ObservableProperty] private string _opis    = string.Empty;
    [ObservableProperty] private string _konto   = string.Empty;
    [ObservableProperty] private string _kontoP  = string.Empty;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private long   _idBr;
}
