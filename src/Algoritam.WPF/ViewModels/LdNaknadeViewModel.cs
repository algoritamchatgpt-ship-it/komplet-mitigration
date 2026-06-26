using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// ViewModel za LDNAKNADE — Obračun naknada (bolovanje, porodiljsko).
/// Originalni FoxPro: DO FORM LDNAKNADE, čita ldgod0.dbf
/// Dugmad: DODAJ, navigacija, Prenos iz arhive, Obračun naknada, IZLAZ
///
/// Prenos iz arhive: čita ld00.dbf → kopira u ldgod0 sa izračunatim POSATU
/// Obračun naknada: iz ldgod0.POSATU i ld00 CAS polja → računa DIN naknade u ld00
/// </summary>
public partial class LdNaknadeViewModel : ObservableObject
{
    private readonly string _folderPath;
    private string _dbfPath = "";
    private DbfOptimisticConcurrency.FileSnapshot? _snapshot;

    [ObservableProperty] private ObservableCollection<LdNaknadeStavka> _stavke = [];
    [ObservableProperty] private LdNaknadeStavka? _selektovana;
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private bool _ucitava = true;

    public string Naslov => "OBRAČUN NAKNADA — LDGOD0";

    public LdNaknadeViewModel(string folderPath)
    {
        _folderPath = folderPath;
        _dbfPath = NadjiDbf(folderPath, "ldgod0.dbf");
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
            Poruka = $"Fajl ldgod0.dbf nije pronađen u: {_folderPath}";
            Ucitava = false;
            return;
        }

        _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(_dbfPath);

        try
        {
            var zapisi = DbfReader.CitajSveZapise(_dbfPath);
            foreach (var z in zapisi)
            {
                Stavke.Add(new LdNaknadeStavka
                {
                    Broj    = Int(z, "BROJ"),
                    ImePrez = Str(z, "IME_PREZ"),
                    Casuk   = Dec(z, "CASUK"),
                    Dinuk   = Dec(z, "DINUK"),
                    Posatu  = Dec(z, "POSATU"),
                    Preneto = Str(z, "PRENETO"),
                    IdBr    = Long(z, "IDBR"),
                });
            }
            Poruka = zapisi.Count == 0
                ? "Nema unetih naknada. Koristite 'Prenos iz arhive'."
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
        // Fox: APPEND BLANK (prazan red u ldgod0)
        var sledeci = Stavke.Count > 0 ? Stavke.Max(s => s.Broj) + 1 : 1;
        var nova = new LdNaknadeStavka { Broj = sledeci };
        Stavke.Add(nova);
        Selektovana = nova;
        Poruka = "Dodat nov red — unesite podatke i sačuvajte.";
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        SacuvajUDbf();
    }

    // ── Prenos iz arhive ─────────────────────────────────────────────────────
    // Fox: APPEND FROM ld00.dbf → ldgod0; pa računa CASUK, DINUK, POSATU

    [RelayCommand]
    private void PrenosIzArhive()
    {
        var ld00Path = NadjiDbf(_folderPath, "ld00.dbf");
        if (!File.Exists(ld00Path))
        {
            Poruka = "Fajl ld00.dbf nije pronađen — otvorite Platni spisak i prenesite radnike.";
            return;
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
            foreach (var z in zapisi)
            {
                var casuc      = Dec(z, "CASUC");
                var casradnap  = Dec(z, "CASRADNAP");
                var cas1       = Dec(z, "CAS1");
                var cas2       = Dec(z, "CAS2");
                var cas3       = Dec(z, "CAS3");
                var dinuc      = Dec(z, "DINUC");
                var dinnoc     = Dec(z, "DINNOC");
                var dinprod    = Dec(z, "DINPROD");
                var dinned     = Dec(z, "DINNED");
                var dinradnap  = Dec(z, "DINRADNAP");
                var din1       = Dec(z, "DIN1");
                var din2       = Dec(z, "DIN2");
                var din3       = Dec(z, "DIN3");
                var stim1      = Dec(z, "STIM1");

                // Fox: REPLACE ALL casuk WITH casuc+casradnap+cas1+cas2+cas3
                var casuk = casuc + casradnap + cas1 + cas2 + cas3;
                // Fox: REPLACE ALL dinuk WITH dinuc+dinnoc+dinprod+dinned+dinradnap+din1+din2+din3+stim1
                var dinuk = dinuc + dinnoc + dinprod + dinned + dinradnap + din1 + din2 + din3 + stim1;
                // Fox: REPLACE posatu WITH ROUND(dinuk/casuk,2)
                var posatu = casuk != 0 ? Math.Round(dinuk / casuk, 2) : 0m;

                Stavke.Add(new LdNaknadeStavka
                {
                    Broj    = Int(z, "BROJ"),
                    ImePrez = Str(z, "IME_PREZ"),
                    Casuk   = casuk,
                    Dinuk   = dinuk,
                    Posatu  = posatu,
                    Preneto = Str(z, "PRENETO"),
                    IdBr    = Long(z, "IDBR"),
                });
            }

            SacuvajUDbf();
            Poruka = $"Prenos iz arhive: {Stavke.Count} radnika. Posatu je izračunat.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri prenosu: {ex.Message}";
        }
    }

    // ── Obračun naknada ──────────────────────────────────────────────────────
    // Fox: za svaki red ldgod0, uzmi POSATU, nađi odgovarajući red u ld00,
    //      izračunaj DIN naknade (praz, bol, plac itd.) i upiši u ld00.

    [RelayCommand]
    private async Task ObracunNaknada()
    {
        var ld00Path = NadjiDbf(_folderPath, "ld00.dbf");
        if (!File.Exists(ld00Path))
        {
            Poruka = "Fajl ld00.dbf nije pronađen.";
            return;
        }

        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka u ldgod0 — pokrenite 'Prenos iz arhive' prvo.";
            return;
        }

        // Učitaj stope iz LDPARAM
        decimal procBol = 0, procPlac = 0, procSus = 0;
        var paramPath = NadjiDbf(_folderPath, "ldparam.dbf");
        if (File.Exists(paramPath))
        {
            var par = DbfReader.CitajSveZapise(paramPath).FirstOrDefault();
            if (par != null)
            {
                procBol  = Dec(par, "PROCBOL");
                procPlac = Dec(par, "PROCPLAC");
                procSus  = Dec(par, "PROCSUS");
            }
        }

        try
        {
            // Učitaj sve ld00 zapise i ažuriraj
            var ld00Zapisi = DbfReader.CitajSveZapise(ld00Path);
            var godPoBroju = Stavke.ToDictionary(s => s.Broj, s => s);

            foreach (var z in ld00Zapisi)
            {
                var broj = Int(z, "BROJ");
                if (!godPoBroju.TryGetValue(broj, out var god)) continue;

                var posatu = god.Posatu;

                // Fox: MDINPRAZ = CASPRAZ * POSATU  (praznik — 100%)
                var caspraz  = Dec(z, "CASPRAZ");
                var casbol   = Dec(z, "CASBOL");
                var casbol2  = Dec(z, "CASBOL2");
                var casplac  = Dec(z, "CASPLAC");
                var casplac2 = Dec(z, "CASPLAC2");
                var casgod   = Dec(z, "CASGOD");
                var casvv    = Dec(z, "CASVV");
                var cassus   = Dec(z, "CASSUS");

                z["DINPRAZ"]  = Math.Round(caspraz  * posatu, 2);
                z["DINBOL"]   = Math.Round(casbol   * posatu * procBol  / 100, 2);
                z["DINBOL2"]  = Math.Round(casbol2  * posatu, 2);
                z["DINPLAC"]  = Math.Round(casplac  * posatu * procPlac / 100, 2);
                z["DINPLAC2"] = Math.Round(casplac2 * posatu, 2);
                z["DINGOD"]   = Math.Round(casgod   * posatu, 2);
                z["DINVV"]    = Math.Round(casvv    * posatu, 2);
                z["DINSUS"]   = Math.Round(cassus   * posatu * procSus  / 100, 2);
            }

            // Upišemo ažurirane zapise nazad u ld00.dbf
            var schema = DbfTableWriter.LoadSchema(ld00Path);
            DbfTableWriter.WriteTable(
                ld00Path, schema, ld00Zapisi,
                static (row, field) => row.TryGetValue(field, out var v) ? v : null);

            Poruka = $"Obračun naknada završen — ažurirano {ld00Zapisi.Count} zapisa u ld00.dbf.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri obračunu: {ex.Message}";
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private void Osvezi() => UcitajPodatke();

    // ── Čuvanje u DBF ─────────────────────────────────────────────────────────

    private void SacuvajUDbf()
    {
        if (!File.Exists(_dbfPath))
        {
            Poruka = $"Ne mogu da sačuvam — ldgod0.dbf nije pronađen.";
            return;
        }

        if (_snapshot != null && DbfOptimisticConcurrency.HasFileChanged(_dbfPath, _snapshot))
        {
            var r = MessageBox.Show(
                "Fajl ldgod0.dbf je izmenjen od strane drugog korisnika. Nastaviti?",
                "Upozorenje", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r != MessageBoxResult.Yes) return;
        }

        try
        {
            var schema = DbfTableWriter.LoadSchema(_dbfPath);
            var rows = Stavke.Select(s => new Dictionary<string, object?>
            {
                ["BROJ"]    = (decimal)s.Broj,
                ["IME_PREZ"]= s.ImePrez,
                ["CASUK"]   = s.Casuk,
                ["DINUK"]   = s.Dinuk,
                ["POSATU"]  = s.Posatu,
                ["PRENETO"] = s.Preneto,
                ["IDBR"]    = (decimal)s.IdBr,
            }).ToList();

            DbfTableWriter.WriteTable(
                _dbfPath, schema, rows,
                static (row, field) => row.TryGetValue(field, out var v) ? v : null);

            _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(_dbfPath);
            Poruka = $"Sačuvano {Stavke.Count} stavki.";
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
}

public class LdNaknadeStavka
{
    public int     Broj    { get; set; }
    public string  ImePrez { get; set; } = string.Empty;
    public decimal Casuk   { get; set; }
    public decimal Dinuk   { get; set; }
    public decimal Posatu  { get; set; }
    public string  Preneto { get; set; } = string.Empty;
    public long    IdBr    { get; set; }
}
