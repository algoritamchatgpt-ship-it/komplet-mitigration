using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

public partial class BaznaKontaViewModel : ObservableObject
{
    // ldkon00.dbf — bazni šifarnik konta (shared, data00 folder u FoxPro)
    private string _dbfPath  = "";
    // ldkon0b.dbf — uzorak za prenos iz primera
    private string _uzorPath = "";
    private DbfOptimisticConcurrency.FileSnapshot? _snapshot;

    [ObservableProperty] private ObservableCollection<BaznaKontaStavka> _stavke = [];
    [ObservableProperty] private BaznaKontaStavka? _selektovana;
    [ObservableProperty] private string _poruka = "";

    public BaznaKontaViewModel(string folderPath)
    {
        // Traži ldkon00.dbf: prvo u folderPath, zatim u ../data00 (FoxPro original)
        _dbfPath  = NadjiDbf(folderPath, "ldkon00.dbf")
                 ?? NadjiDbfData00(folderPath, "ldkon00.dbf")
                 ?? Path.Combine(folderPath, "ldkon00.dbf");

        _uzorPath = NadjiDbf(folderPath, "ldkon0b.dbf")
                 ?? NadjiDbfData00(folderPath, "ldkon0b.dbf")
                 ?? Path.Combine(folderPath, "ldkon0b.dbf");

        UcitajPodatke();
    }

    // ── Čitanje ───────────────────────────────────────────────────────────────

    private void UcitajPodatke()
    {
        Stavke.Clear();

        if (!File.Exists(_dbfPath))
        {
            Poruka = $"Fajl ldkon00.dbf nije pronađen u: {Path.GetDirectoryName(_dbfPath)}";
            return;
        }

        _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(_dbfPath);

        try
        {
            var zapisi = DbfReader.CitajSveZapise(_dbfPath);
            foreach (var z in zapisi)
                Stavke.Add(MapirajStavku(z));

            Poruka = Stavke.Count > 0
                ? $"Učitano {Stavke.Count} konta."
                : "Nema unetih konta. Koristite 'Prenos iz primera' ili 'Dodavanje'.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čitanju: {ex.Message}";
        }
    }

    private static BaznaKontaStavka MapirajStavku(Dictionary<string, object?> z) => new()
    {
        Vrsta   = Str(z, "VRSTA"),
        Kod     = Str(z, "KOD"),
        Opis    = Str(z, "OPIS"),
        Konto   = Str(z, "KONTO"),
        Kontop  = Str(z, "KONTOP"),
        Preneto = Str(z, "PRENETO"),
        Idbr    = Long(z, "IDBR"),
    };

    // ── Komande ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Dodaj()
    {
        var nova = new BaznaKontaStavka();
        Stavke.Add(nova);
        Selektovana = nova;
        Poruka = "Dodat nov red — popunite podatke i sačuvajte.";
    }

    [RelayCommand]
    private void ObrisiRed()
    {
        if (Selektovana is null)
        {
            Poruka = "Nije izabran red za brisanje.";
            return;
        }

        var opis = string.IsNullOrWhiteSpace(Selektovana.Opis)
            ? "bez opisa"
            : Selektovana.Opis;

        if (MessageBox.Show(
                $"Obrisati konto \"{opis}\"?",
                "Brisanje reda",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            Stavke.Remove(Selektovana);
            Selektovana = Stavke.FirstOrDefault();
            SacuvajUDbf();
            Poruka = "Red je obrisan i sačuvan.";
        }
    }

    [RelayCommand]
    private void ObrisiSve()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema redova za brisanje.";
            return;
        }

        if (MessageBox.Show(
                $"Obrisati SVA {Stavke.Count} konta?\nOva akcija se ne može poništiti.",
                "Briši sve",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            Stavke.Clear();
            Selektovana = null;
            SacuvajUDbf();
            Poruka = "Sva konta su obrisana.";
        }
    }

    [RelayCommand]
    private void PrenosIzPrimera()
    {
        if (Stavke.Count > 0)
        {
            Poruka = "Prenos je moguć samo ako je tabela prazna.";
            return;
        }

        if (!File.Exists(_uzorPath))
        {
            Poruka = "Fajl uzorka ldkon0b.dbf nije pronađen.";
            return;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(_uzorPath);
            foreach (var z in zapisi)
                Stavke.Add(MapirajStavku(z));

            SacuvajUDbf();
            Poruka = $"Preneseno {Stavke.Count} konta iz primera.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri prenosu: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        SacuvajUDbf();
    }

    [RelayCommand]
    private void Osvezi()
    {
        UcitajPodatke();
    }

    // ── Čuvanje u DBF ─────────────────────────────────────────────────────────

    private void SacuvajUDbf()
    {
        if (!File.Exists(_dbfPath))
        {
            Poruka = "Ne mogu da sačuvam — ldkon00.dbf nije pronađen.";
            return;
        }

        if (_snapshot != null && DbfOptimisticConcurrency.HasFileChanged(_dbfPath, _snapshot))
        {
            var r = MessageBox.Show(
                "Fajl ldkon00.dbf je izmenjen od strane drugog korisnika.\nNastaviti sa čuvanjem (prepisati)?",
                "Upozorenje — dual korisnici", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r != MessageBoxResult.Yes) return;
        }

        try
        {
            var schema = DbfTableWriter.LoadSchema(_dbfPath);
            var rows = Stavke.Select(s => new Dictionary<string, object?>
            {
                ["VRSTA"]   = s.Vrsta,
                ["KOD"]     = s.Kod,
                ["OPIS"]    = s.Opis,
                ["KONTO"]   = s.Konto,
                ["KONTOP"]  = s.Kontop,
                ["PRENETO"] = s.Preneto,
                ["IDBR"]    = (decimal)s.Idbr,
            }).ToList();

            DbfTableWriter.WriteTable(
                _dbfPath, schema, rows,
                static (row, field) => row.TryGetValue(field, out var v) ? v : null);

            _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(_dbfPath);
            Poruka = $"Sačuvano {Stavke.Count} konta u ldkon00.dbf.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čuvanju: {ex.Message}";
        }
    }

    // ── Pomoćne ───────────────────────────────────────────────────────────────

    private static string? NadjiDbf(string folder, string ime)
    {
        if (string.IsNullOrWhiteSpace(folder)) return null;
        var p = Path.Combine(folder, ime);
        if (File.Exists(p)) return p;
        return Directory.GetFiles(folder, ime, SearchOption.TopDirectoryOnly).FirstOrDefault();
    }

    // FoxPro koristio MDATA00 = roditeljski data00 folder
    private static string? NadjiDbfData00(string folderPath, string ime)
    {
        try
        {
            var parent = Path.GetDirectoryName(folderPath);
            if (parent is null) return null;
            var data00 = Path.Combine(parent, "data00");
            return NadjiDbf(data00, ime);
        }
        catch { return null; }
    }

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;

    private static long Long(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) ? Convert.ToInt64(v ?? 0L) : 0L;
}

public partial class BaznaKontaStavka : ObservableObject
{
    [ObservableProperty] private string _vrsta   = string.Empty;
    [ObservableProperty] private string _kod     = string.Empty;
    [ObservableProperty] private string _opis    = string.Empty;
    [ObservableProperty] private string _konto   = string.Empty;
    [ObservableProperty] private string _kontop  = string.Empty;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private long   _idbr;
}
