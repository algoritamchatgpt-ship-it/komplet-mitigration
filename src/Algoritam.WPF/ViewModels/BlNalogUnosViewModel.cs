using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class BlNalogUnosViewModel : ObservableObject
{
    private readonly string _folderPath;
    private string? _dbfPath;
    private List<Dictionary<string, object?>> _sviNalozi = [];

    public string Naslov => "BLAGAJNA — UNOS NALOGA";

    private int? _indeksIzmene;

    // Pregled liste
    [ObservableProperty] private ObservableCollection<Dictionary<string, object?>> _nalozi = [];
    [ObservableProperty] private Dictionary<string, object?>? _izabranNalog;
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;
    [ObservableProperty] private string _filterTekst = "";
    [ObservableProperty] private DateTime _datumOd = new(DateTime.Today.Year, 1, 1);
    [ObservableProperty] private DateTime _datumDo = DateTime.Today;

    // Polja za unos novog naloga
    [ObservableProperty] private DateTime _datNalog = DateTime.Today;
    [ObservableProperty] private bool _jeUplata = true;
    partial void OnJeUplataChanged(bool value) => OnPropertyChanged(nameof(JeIsplata));
    public bool JeIsplata { get => !JeUplata; set => JeUplata = !value; }

    [ObservableProperty] private string _konto = "";
    [ObservableProperty] private string _iznos = "";
    [ObservableProperty] private string _opis = "";
    [ObservableProperty] private string _imePartnera = "";
    [ObservableProperty] private string _dokBroj = "";
    [ObservableProperty] private string _unosPoruka = "";

    public string DugmeTekst => _indeksIzmene is not null ? "SAČUVAJ IZMENU" : "DODAJ NALOG";
    public bool JeNoviUnos => _indeksIzmene is null;

    public event Action? ZatvaranjeZahtevano;

    public BlNalogUnosViewModel(string folderPath)
    {
        _folderPath = folderPath;
        _ = UcitajAsync();
    }

    private async Task UcitajAsync()
    {
        Ucitava = true;
        UnosPoruka = "";
        StatusPoruka = "Učitavanje...";
        try
        {
            _dbfPath = NadjiDbf(_folderPath, "kas.dbf");
            if (_dbfPath is null)
            {
                StatusPoruka = "kas.dbf nije pronađen.";
                return;
            }
            _sviNalozi = await Task.Run(() => DbfReader.CitajSveZapise(_dbfPath));
            PrimenjiFilter();
            StatusPoruka = $"Ukupno: {_sviNalozi.Count} zapisa.";
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

    partial void OnDatumOdChanged(DateTime value)   => PrimenjiFilter();
    partial void OnDatumDoChanged(DateTime value)   => PrimenjiFilter();
    partial void OnFilterTekstChanged(string value) => PrimenjiFilter();

    partial void OnIzabranNalogChanged(Dictionary<string, object?>? value)
    {
        UnosPoruka = "";
        if (value is null)
        {
            _indeksIzmene = null;
            OnPropertyChanged(nameof(DugmeTekst));
            OnPropertyChanged(nameof(JeNoviUnos));
            return;
        }

        _indeksIzmene = _sviNalozi.IndexOf(value);
        DatNalog = value.TryGetValue("DATDOK", out var dv) && dv is DateTime dt ? dt : DateTime.Today;
        JeUplata = IntZ(value, "NALU") > 0;
        Konto = value.GetValueOrDefault("KONTO")?.ToString()?.Trim() ?? "";
        var dug = DecZ(value, "DUG");
        var pot = DecZ(value, "POT");
        Iznos = (JeUplata ? dug : pot).ToString(System.Globalization.CultureInfo.InvariantCulture);
        Opis = value.GetValueOrDefault("OPIS")?.ToString()?.Trim() ?? "";
        ImePartnera = value.GetValueOrDefault("IME")?.ToString()?.Trim() ?? "";
        DokBroj = value.GetValueOrDefault("DOK")?.ToString()?.Trim() ?? "";

        OnPropertyChanged(nameof(DugmeTekst));
        OnPropertyChanged(nameof(JeNoviUnos));
    }

    [RelayCommand]
    private void NoviUnos()
    {
        IzabranNalog = null;
        Konto = "";
        Iznos = "";
        Opis = "";
        ImePartnera = "";
        DokBroj = "";
        DatNalog = DateTime.Today;
        UnosPoruka = "";
    }

    private void PrimenjiFilter()
    {
        var tekst = FilterTekst.Trim().ToLowerInvariant();
        var od    = DatumOd.Date;
        var do_   = DatumDo.Date;

        var q = _sviNalozi.AsEnumerable();
        q = q.Where(n =>
        {
            if (!n.TryGetValue("DATDOK", out var dv)) return true;
            var d = dv is DateTime dt ? dt :
                    DateTime.TryParse(dv?.ToString(), out var dt2) ? dt2 : (DateTime?)null;
            return d is null || (d.Value.Date >= od && d.Value.Date <= do_);
        });
        if (!string.IsNullOrEmpty(tekst))
            q = q.Where(n => n.Values.Any(v => v?.ToString()?.ToLowerInvariant().Contains(tekst) == true));

        Nalozi = new ObservableCollection<Dictionary<string, object?>>(q.ToList());
    }

    [RelayCommand]
    private async Task DodajNalog()
    {
        UnosPoruka = "";

        if (string.IsNullOrWhiteSpace(Konto))
        {
            UnosPoruka = "Unesite konto.";
            return;
        }
        if (!decimal.TryParse(Iznos.Replace(',', '.'), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var iznos) || iznos <= 0)
        {
            UnosPoruka = "Unesite iznos (broj veći od 0).";
            return;
        }
        if (_dbfPath is null)
        {
            UnosPoruka = "kas.dbf nije pronađen.";
            return;
        }

        try
        {
            string poruka;

            if (_indeksIzmene is int idx && idx >= 0 && idx < _sviNalozi.Count)
            {
                var original = _sviNalozi[idx];
                var bioUplata = IntZ(original, "NALU") > 0;

                var azuriranNalog = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["DATDOK"] = DatNalog,
                    ["DNEV"]   = original.GetValueOrDefault("DNEV"),
                    ["NALU"]   = original.GetValueOrDefault("NALU"),
                    ["NALI"]   = original.GetValueOrDefault("NALI"),
                    ["KONTO"]  = Konto.Trim().ToUpperInvariant(),
                    ["DUG"]    = bioUplata ? (object?)iznos : 0m,
                    ["POT"]    = bioUplata ? 0m : (object?)iznos,
                    ["OPIS"]   = Opis.Trim(),
                    ["SIFRA"]  = original.GetValueOrDefault("SIFRA"),
                    ["IME"]    = ImePartnera.Trim(),
                    ["DOK"]    = DokBroj.Trim(),
                    ["BRNAL"]  = original.GetValueOrDefault("BRNAL"),
                    ["MP"]     = original.GetValueOrDefault("MP"),
                };

                _sviNalozi[idx] = azuriranNalog;
                poruka = "Nalog uspešno izmenjen.";
            }
            else
            {
                int maxBrnal = _sviNalozi.Any() ? _sviNalozi.Max(n => IntZ(n, "BRNAL")) : 0;
                int maxNal   = JeUplata
                    ? _sviNalozi.Select(n => IntZ(n, "NALU")).DefaultIfEmpty(0).Max()
                    : _sviNalozi.Select(n => IntZ(n, "NALI")).DefaultIfEmpty(0).Max();

                var noviNalog = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["DATDOK"] = DatNalog,
                    ["DNEV"]   = 0m,
                    ["NALU"]   = JeUplata ? (object?)(decimal)(maxNal + 1) : 0m,
                    ["NALI"]   = JeUplata ? 0m : (object?)(decimal)(maxNal + 1),
                    ["KONTO"]  = Konto.Trim().ToUpperInvariant(),
                    ["DUG"]    = JeUplata ? (object?)iznos : 0m,
                    ["POT"]    = JeUplata ? 0m : (object?)iznos,
                    ["OPIS"]   = Opis.Trim(),
                    ["SIFRA"]  = "",
                    ["IME"]    = ImePartnera.Trim(),
                    ["DOK"]    = DokBroj.Trim(),
                    ["BRNAL"]  = (decimal)(maxBrnal + 1),
                    ["MP"]     = "",
                };

                _sviNalozi.Add(noviNalog);
                poruka = $"Nalog {maxBrnal + 1} uspešno dodat.";
            }

            var schema = DbfTableWriter.LoadSchema(_dbfPath);
            await Task.Run(() => DbfTableWriter.WriteTable(
                _dbfPath,
                schema,
                _sviNalozi,
                (row, field) => row.TryGetValue(field, out var v) ? v : null));

            IzabranNalog = null;
            PrimenjiFilter();
            StatusPoruka = $"Ukupno: {_sviNalozi.Count} zapisa.";

            // Reset polja za unos
            Konto        = "";
            Iznos        = "";
            Opis         = "";
            ImePartnera  = "";
            DokBroj      = "";
            UnosPoruka   = poruka;
        }
        catch (Exception ex)
        {
            await UcitajAsync();
            UnosPoruka = $"Greška pri čuvanju: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OcistiFilter()
    {
        DatumOd    = new DateTime(DateTime.Today.Year, 1, 1);
        DatumDo    = DateTime.Today;
        FilterTekst = "";
    }

    [RelayCommand]
    private void Osvezi() => _ = UcitajAsync();

    [RelayCommand]
    private void Zatvori() => ZatvaranjeZahtevano?.Invoke();

    private static int IntZ(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0;
        return v switch { decimal d => (int)d, int i => i, long l => (int)l, _ => 0 };
    }

    private static decimal DecZ(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0m;
        return v switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            _ => decimal.TryParse(v.ToString(), out var p) ? p : 0m
        };
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
