using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

public enum OsRezim { Pregled, Otpis, Prenos }

/// <summary>
/// Pregled osnovnih sredstava — os.dbf sa filterom po grupi i nazivu.
/// Opciono podržava write-mode za otpis (OsRezim.Otpis) i prenos (OsRezim.Prenos).
/// </summary>
public partial class OsPregledViewModel : ObservableObject
{
    private readonly string _folderPath;
    private readonly string _dbfName;

    public OsRezim Rezim { get; }
    public string Naslov { get; }

    public bool JeOtpisRezim  => Rezim == OsRezim.Otpis;
    public bool JePrenosRezim => Rezim == OsRezim.Prenos;
    public bool JePregledRezim => Rezim == OsRezim.Pregled;

    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _sredstva = [];
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OtpisiSelektovanoCommand))]
    [NotifyCanExecuteChangedFor(nameof(PrenesSredstvoCommand))]
    [NotifyCanExecuteChangedFor(nameof(IzmeniCommand))]
    private Dictionary<string, object?>? _izabranoSredstvo;
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;
    [ObservableProperty] private string _filterNaziv = "";
    [ObservableProperty] private string _filterGrupa = "";

    // Otpis
    [ObservableProperty] private DateTime _datumOtpisa = DateTime.Today;

    // Prenos
    [ObservableProperty] private string _novaGrupa = "";

    private List<Dictionary<string, object?>> _svaSredstva = [];

    public event Action? ZatvaranjeZahtevano;

    public OsPregledViewModel(string folderPath, string dbfName = "os.dbf",
        string naslov = "OSNOVNA SREDSTVA — PREGLED", OsRezim rezim = OsRezim.Pregled)
    {
        _folderPath = folderPath;
        _dbfName = dbfName;
        Naslov = naslov;
        Rezim = rezim;
        _ = UcitajAsync();
    }

    private async Task UcitajAsync()
    {
        Ucitava = true;
        StatusPoruka = "Učitavanje...";
        try
        {
            var dbfPath = NadjiDbf(_folderPath, _dbfName);
            if (dbfPath is null)
            {
                StatusPoruka = $"Tabela {_dbfName} nije pronađena.";
                return;
            }
            _svaSredstva = await Task.Run(() => DbfReader.CitajSveZapise(dbfPath));
            PrimenjiFilter();
            StatusPoruka = $"Ukupno: {_svaSredstva.Count} zapisa.";
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

    partial void OnFilterNazivChanged(string value) => PrimenjiFilter();
    partial void OnFilterGrupaChanged(string value) => PrimenjiFilter();

    private void PrimenjiFilter()
    {
        var naziv = FilterNaziv.Trim().ToLowerInvariant();
        var grupa = FilterGrupa.Trim().ToLowerInvariant();

        var filtrirani = _svaSredstva.AsEnumerable();

        if (!string.IsNullOrEmpty(naziv))
            filtrirani = filtrirani.Where(s =>
                s.Values.Any(v => v?.ToString()?.ToLowerInvariant().Contains(naziv) == true));

        if (!string.IsNullOrEmpty(grupa))
            filtrirani = filtrirani.Where(s =>
                (s.TryGetValue("GRUPA",  out var g) && g?.ToString()?.ToLowerInvariant().StartsWith(grupa) == true) ||
                (s.TryGetValue("GRBROJ", out var gb) && gb?.ToString()?.ToLowerInvariant().StartsWith(grupa) == true));

        Sredstva = new ObservableCollection<Dictionary<string, object?>>(filtrirani.ToList());
    }

    [RelayCommand]
    private void Osvezi() => _ = UcitajAsync();

    [RelayCommand]
    private void OcistiFilter()
    {
        FilterNaziv = "";
        FilterGrupa = "";
    }

    [RelayCommand]
    private void Zatvori() => ZatvaranjeZahtevano?.Invoke();

    // ── Izmena (samo u OsRezim.Pregled) ──────────────────────────────────────

    [RelayCommand(CanExecute = nameof(MozeIzmeniti))]
    private void Izmeni(Window? vlasnik)
    {
        if (IzabranoSredstvo is null) return;

        var dbfPath = NadjiDbf(_folderPath, _dbfName);
        if (dbfPath is null) { StatusPoruka = "Fajl nije dostupan."; return; }

        var schema = DbfTableWriter.LoadSchema(dbfPath);
        var dijalogVm = new DbfUnosIzmenaViewModel(schema, "IZMENA OSNOVNOG SREDSTVA", IzabranoSredstvo, bojaHeader: "#4527A0");
        var dijalog = new Views.DbfUnosIzmenaView { DataContext = dijalogVm, Owner = vlasnik };
        dijalog.ShowDialog();

        if (!dijalogVm.Uspesno || dijalogVm.Rezultat is null) return;

        try
        {
            var sviZapisi = DbfReader.CitajSveZapise(dbfPath);
            var idx = NadjiIndeks(sviZapisi, IzabranoSredstvo);
            if (idx < 0) { StatusPoruka = "Zapis nije pronađen u fajlu."; return; }

            sviZapisi[idx] = dijalogVm.Rezultat;
            DbfTableWriter.WriteTable(dbfPath, schema, sviZapisi,
                (row, fieldName) => row.TryGetValue(fieldName, out var v) ? v : null);

            StatusPoruka = "Osnovno sredstvo izmenjeno.";
            _ = UcitajAsync();
        }
        catch (Exception ex)
        {
            StatusPoruka = $"Greška pri izmeni: {ex.Message}";
        }
    }

    private bool MozeIzmeniti() => Rezim == OsRezim.Pregled && IzabranoSredstvo is not null;

    // ── Otpis ──────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(MozeOtpisati))]
    private void OtpisiSelektovano()
    {
        if (IzabranoSredstvo is null) return;

        var naziv = DajNazivSredstva(IzabranoSredstvo);

        var potvrda = MessageBox.Show(
            $"Otpisati osnovno sredstvo:\n{naziv}\n\nDatum otpisa: {DatumOtpisa:dd.MM.yyyy}\n\nNastaviti?",
            "Otpis OS",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (potvrda != MessageBoxResult.Yes) return;

        try
        {
            var dbfPath = NadjiDbf(_folderPath, _dbfName);
            if (dbfPath is null) { StatusPoruka = "Fajl nije dostupan."; return; }

            var schema = DbfTableWriter.LoadSchema(dbfPath);
            var sviZapisi = DbfReader.CitajSveZapise(dbfPath);
            var idx = NadjiIndeks(sviZapisi, IzabranoSredstvo);
            if (idx < 0) { StatusPoruka = "Zapis nije pronađen u fajlu."; return; }

            var zapis = sviZapisi[idx];
            PostaviPolje(zapis, schema, "OTPIS",    true);
            PostaviPolje(zapis, schema, "OTPISAN",  true);
            PostaviPolje(zapis, schema, "DATOTPIS", DatumOtpisa);
            PostaviPolje(zapis, schema, "SVRED",    0m);
            PostaviPolje(zapis, schema, "SADASNJA", 0m);

            DbfTableWriter.WriteTable(dbfPath, schema, sviZapisi,
                (row, fieldName) => row.TryGetValue(fieldName, out var v) ? v : null);

            StatusPoruka = $"Osnovno sredstvo '{naziv}' uspešno otpisano.";
            _ = UcitajAsync();
        }
        catch (Exception ex)
        {
            StatusPoruka = $"Greška pri otpisu: {ex.Message}";
        }
    }

    private bool MozeOtpisati() => IzabranoSredstvo is not null;

    // ── Prenos ─────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(MozePrenositi))]
    private void PrenesSredstvo()
    {
        if (IzabranoSredstvo is null) return;
        if (string.IsNullOrWhiteSpace(NovaGrupa))
        {
            StatusPoruka = "Unesite broj nove grupe.";
            return;
        }

        var naziv = DajNazivSredstva(IzabranoSredstvo);

        var potvrda = MessageBox.Show(
            $"Preneti osnovno sredstvo:\n{naziv}\n\nu grupu: {NovaGrupa}\n\nNastaviti?",
            "Prenos OS",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (potvrda != MessageBoxResult.Yes) return;

        try
        {
            var dbfPath = NadjiDbf(_folderPath, _dbfName);
            if (dbfPath is null) { StatusPoruka = "Fajl nije dostupan."; return; }

            var schema = DbfTableWriter.LoadSchema(dbfPath);
            var sviZapisi = DbfReader.CitajSveZapise(dbfPath);
            var idx = NadjiIndeks(sviZapisi, IzabranoSredstvo);
            if (idx < 0) { StatusPoruka = "Zapis nije pronađen u fajlu."; return; }

            var zapis = sviZapisi[idx];

            if (decimal.TryParse(NovaGrupa, out var gBroj))
                PostaviPolje(zapis, schema, "GRBROJ", gBroj);
            else
                PostaviPolje(zapis, schema, "GRBROJ", NovaGrupa);

            PostaviPolje(zapis, schema, "PRENOS",  true);
            PostaviPolje(zapis, schema, "DATOPR",  DateTime.Today);

            DbfTableWriter.WriteTable(dbfPath, schema, sviZapisi,
                (row, fieldName) => row.TryGetValue(fieldName, out var v) ? v : null);

            StatusPoruka = $"Osnovno sredstvo '{naziv}' preneto u grupu {NovaGrupa}.";
            NovaGrupa = "";
            _ = UcitajAsync();
        }
        catch (Exception ex)
        {
            StatusPoruka = $"Greška pri prenosu: {ex.Message}";
        }
    }

    private bool MozePrenositi() => IzabranoSredstvo is not null;

    // ── Helpers ────────────────────────────────────────────────────────────

    private static string DajNazivSredstva(Dictionary<string, object?> sredstvo)
    {
        if (sredstvo.TryGetValue("NAZIV", out var n) && n?.ToString()?.Trim() is { Length: > 0 } nStr)
            return nStr;
        if (sredstvo.TryGetValue("SIFOS", out var s) && s?.ToString()?.Trim() is { Length: > 0 } sStr)
            return sStr;
        return "?";
    }

    private static int NadjiIndeks(
        List<Dictionary<string, object?>> lista,
        Dictionary<string, object?> trazeni)
    {
        // Prvo po referenci (isti objekat)
        var idxRef = lista.IndexOf(trazeni);
        if (idxRef >= 0) return idxRef;

        // Fallback: po vrijednostima svih ključnih polja
        for (int i = 0; i < lista.Count; i++)
        {
            if (lista[i].Count != trazeni.Count) continue;
            bool isti = true;
            foreach (var kv in trazeni)
            {
                if (!lista[i].TryGetValue(kv.Key, out var v) ||
                    !Equals(v?.ToString(), kv.Value?.ToString()))
                { isti = false; break; }
            }
            if (isti) return i;
        }
        return -1;
    }

    private static void PostaviPolje(
        Dictionary<string, object?> zapis,
        DbfTableWriter.DbfSchema schema,
        string imePolja,
        object? vrednost)
    {
        if (!schema.Fields.Any(f => f.Name.Equals(imePolja, StringComparison.OrdinalIgnoreCase)))
            return;
        var kljuc = zapis.Keys.FirstOrDefault(k =>
            k.Equals(imePolja, StringComparison.OrdinalIgnoreCase));
        if (kljuc is not null) zapis[kljuc] = vrednost;
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
