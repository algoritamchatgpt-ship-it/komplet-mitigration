using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

public partial class RadnoVremeViewModel : ObservableObject
{
    public string FolderPath { get; }
    private DbfOptimisticConcurrency.FileSnapshot? _snapshot;

    [ObservableProperty] private ObservableCollection<RadnoVremeStavka> _stavke = [];
    [ObservableProperty] private string _naslov = "TABELA EVIDENCIJE RADNOG VREMENA";
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private bool _ucitava = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TabelaReadOnly))]
    [NotifyPropertyChangedFor(nameof(SacuvajVisible))]
    private bool _tabelaOtvorena = false;

    [ObservableProperty] private RadnoVremeStavka? _trenutnaStavka;
    [ObservableProperty] private string _lblRad = "";

    public bool TabelaReadOnly => !TabelaOtvorena;
    public bool SacuvajVisible => TabelaOtvorena;

    private string DbfPath    => Path.Combine(FolderPath, "ldradvre.dbf");
    private string LdRadPath  => Path.Combine(FolderPath, "ldrad.dbf");

    public RadnoVremeViewModel(string folderPath)
    {
        FolderPath = folderPath;
        UcitajPodatke();
    }

    public void UcitajPodatke()
    {
        Ucitava = true;
        Stavke.Clear();
        LblRad = "";

        if (!File.Exists(DbfPath))
        {
            Poruka = $"ldradvre.dbf nije pronađen u: {FolderPath}";
            Ucitava = false;
            return;
        }

        _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(DbfPath);

        try
        {
            var zapisi = DbfReader.CitajSveZapise(DbfPath);
            foreach (var z in zapisi)
            {
                Stavke.Add(new RadnoVremeStavka
                {
                    Broj    = DecToInt(z, "BROJ"),
                    ImePrez = Str(z, "IME_PREZ"),
                    Datum   = Dat(z, "DATUM"),
                    PocSat  = DecToInt(z, "POCSAT"),
                    ZadSat  = DecToInt(z, "ZADSAT"),
                    Sati    = DecToInt(z, "SATI"),
                    Preneto = Str(z, "PRENETO"),
                    IdBr    = DecToLong(z, "IDBR"),
                });
            }

            // GO BOTTOM — pozicioniramo se na zadnji zapis
            if (Stavke.Count > 0)
            {
                TrenutnaStavka = Stavke[^1];
                AzurirajLblRad();
            }

            Poruka = $"Učitano {Stavke.Count} zapisa.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čitanju: {ex.Message}";
        }

        Ucitava = false;
    }

    partial void OnTrenutnaStavkaChanged(RadnoVremeStavka? value) => AzurirajLblRad();

    private void AzurirajLblRad()
    {
        if (TrenutnaStavka is { } s)
            LblRad = $"{s.Broj,4} {s.ImePrez}";
        else
            LblRad = "";
    }

    // --- Navigacija ---

    [RelayCommand]
    private void Dole()
    {
        if (TrenutnaStavka == null && Stavke.Count > 0) { TrenutnaStavka = Stavke[0]; return; }
        var idx = Stavke.IndexOf(TrenutnaStavka!);
        if (idx < Stavke.Count - 1)
            TrenutnaStavka = Stavke[idx + 1];
    }

    [RelayCommand]
    private void Gore()
    {
        if (TrenutnaStavka == null) return;
        var idx = Stavke.IndexOf(TrenutnaStavka);
        if (idx > 0)
            TrenutnaStavka = Stavke[idx - 1];
    }

    [RelayCommand]
    private void Zadnji()
    {
        if (Stavke.Count > 0) TrenutnaStavka = Stavke[^1];
    }

    [RelayCommand]
    private void Prvi()
    {
        if (Stavke.Count > 0) TrenutnaStavka = Stavke[0];
    }

    // --- Izmene tabele ---

    [RelayCommand]
    private void OtvoriTabelu()
    {
        TabelaOtvorena = true;
        Poruka = "Tabela otvorena za izmene.";
    }

    [RelayCommand]
    private void SacuvajTabelu()
    {
        SacuvajDbf();
        TabelaOtvorena = false;
        Poruka = "Snimljeno.";
    }

    // PRERAČUNAJ: REPLACE SATI WITH ZADSAT-POCSAT za sve zapise
    [RelayCommand]
    private void Preracunaj()
    {
        foreach (var s in Stavke)
            s.Sati = s.ZadSat - s.PocSat;
        SacuvajDbf();
        Poruka = $"Preračunato {Stavke.Count} zapisa.";
    }

    // BRISANJE RADNIKA: DELETE NEXT 1 + PACK
    [RelayCommand]
    private void BrisanjeRadnika()
    {
        if (TrenutnaStavka == null) return;
        if (MessageBox.Show("Obrisati ovog radnika iz tabele?", "Brisanje radnika",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

        var idx = Stavke.IndexOf(TrenutnaStavka);
        Stavke.Remove(TrenutnaStavka);
        TrenutnaStavka = Stavke.Count > 0 ? Stavke[Math.Min(idx, Stavke.Count - 1)] : null;
        SacuvajDbf();
        Poruka = "Radnik obrisan.";
    }

    // BRISANJE DATUMA: DELETE ALL FOR DATUM=MDATUM + PACK
    [RelayCommand]
    private void BrisanjeDatuma()
    {
        if (TrenutnaStavka == null) return;
        var datum = TrenutnaStavka.Datum.Date;
        var toRemove = Stavke.Where(s => s.Datum.Date == datum).ToList();
        if (toRemove.Count == 0) return;

        if (MessageBox.Show(
            $"Obrisati sve zapise za datum {datum:dd.MM.yyyy}? ({toRemove.Count} zapisa)",
            "Brisanje datuma", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

        foreach (var s in toRemove) Stavke.Remove(s);
        TrenutnaStavka = Stavke.Count > 0 ? Stavke[0] : null;
        SacuvajDbf();
        Poruka = $"Obrisano {toRemove.Count} zapisa za {datum:dd.MM.yyyy}.";
    }

    // Poziva se iz ldradvredod dialoga — APPEND za sve aktivne radnike
    public void DodajSveRadnike(DateTime datum, int pocSat)
    {
        if (!File.Exists(LdRadPath))
        {
            Poruka = "ldrad.dbf nije pronađen.";
            return;
        }

        var ldrad = DbfReader.CitajSveZapise(LdRadPath);
        int dodato = 0;

        foreach (var z in ldrad)
        {
            // BRISANJE=' ' — aktivan radnik
            if (!string.IsNullOrEmpty(Str(z, "BRISANJE"))) continue;

            Stavke.Add(new RadnoVremeStavka
            {
                Broj    = DecToInt(z, "BROJ"),
                ImePrez = Str(z, "IME_PREZ"),
                Datum   = datum,
                PocSat  = pocSat,
                ZadSat  = 0,
                Sati    = 0,
                Preneto = "",
                IdBr    = 0,
            });
            dodato++;
        }

        SacuvajDbf();
        if (Stavke.Count > 0) TrenutnaStavka = Stavke[^1];
        AzurirajLblRad();
        Poruka = $"Dodato {dodato} radnika za {datum:dd.MM.yyyy}.";
    }

    [RelayCommand]
    private void Osvezi() => UcitajPodatke();

    private void SacuvajDbf()
    {
        if (!File.Exists(DbfPath)) return;

        if (_snapshot != null && DbfOptimisticConcurrency.HasFileChanged(DbfPath, _snapshot))
        {
            var r = MessageBox.Show(
                "Fajl ldradvre.dbf je izmenjen od strane drugog korisnika.\nNastaviti sa čuvanjem (prepisati)?",
                "Upozorenje — dual korisnici", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r != MessageBoxResult.Yes) return;
        }

        try
        {
            var schema = DbfTableWriter.LoadSchema(DbfPath);
            DbfTableWriter.WriteTable(DbfPath, schema, (IReadOnlyList<RadnoVremeStavka>)Stavke,
                (s, field) => field.ToUpperInvariant() switch
                {
                    "BROJ"     => (object?)s.Broj,
                    "IME_PREZ" => s.ImePrez,
                    "DATUM"    => s.Datum,
                    "POCSAT"   => s.PocSat,
                    "ZADSAT"   => s.ZadSat,
                    "SATI"     => s.Sati,
                    "PRENETO"  => s.Preneto,
                    "IDBR"     => s.IdBr,
                    _          => null
                });
            _snapshot = DbfOptimisticConcurrency.CaptureFileSnapshot(DbfPath);
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri snimanju: {ex.Message}";
        }
    }

    // --- Helperi za DBF vrednosti ---

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) ? Convert.ToString(v)?.Trim() ?? "" : "";

    private static int DecToInt(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v)) return 0;
        return v is decimal d ? (int)d : v is int i ? i : 0;
    }

    private static long DecToLong(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v)) return 0;
        return v is decimal d ? (long)d : v is long l ? l : 0;
    }

    private static DateTime Dat(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v)) return DateTime.MinValue;
        return v is DateTime d ? d : DateTime.MinValue;
    }
}

public class RadnoVremeStavka : ObservableObject
{
    private int      _broj;
    private string   _imePrez = "";
    private DateTime _datum   = DateTime.MinValue;
    private int      _pocSat;
    private int      _zadSat;
    private int      _sati;
    private string   _preneto = "";
    private long     _idBr;

    public int      Broj    { get => _broj;    set => SetProperty(ref _broj, value); }
    public string   ImePrez { get => _imePrez; set => SetProperty(ref _imePrez, value); }
    public DateTime Datum   { get => _datum;   set => SetProperty(ref _datum, value); }
    public int      PocSat  { get => _pocSat;  set => SetProperty(ref _pocSat, value); }
    public int      ZadSat  { get => _zadSat;  set => SetProperty(ref _zadSat, value); }
    public int      Sati    { get => _sati;    set => SetProperty(ref _sati, value); }
    public string   Preneto { get => _preneto; set => SetProperty(ref _preneto, value); }
    public long     IdBr    { get => _idBr;    set => SetProperty(ref _idBr, value); }
}
