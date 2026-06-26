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
/// Fox forma LDPORSTA — potvrda o radnom stazu za zaposlene na porodiljskom odsustvu.
/// Tabela ldporsta.dbf.
/// </summary>
public partial class LdPorstViewModel : ObservableObject
{
    private readonly string _folderPath;
    private string? _dbfPath;
    private DbfTableWriter.DbfSchema? _schema;

    [ObservableProperty] private ObservableCollection<LdPorstStavka> _stavke = [];
    [ObservableProperty] private LdPorstStavka? _selektovana;
    [ObservableProperty] private string _naslov = "POTVRDA O RADNOM STAZU — PORODILJSKO ODSUSTVO";
    [ObservableProperty] private string _poruka = string.Empty;

    public event Action? ZatvaranjeZatrazeno;

    public LdPorstViewModel(AppState appState)
    {
        _folderPath = appState.AktivnaFirma?.FolderPath ?? string.Empty;
        Ucitaj();
    }

    [RelayCommand]
    private void Osvezi() => Ucitaj();

    [RelayCommand]
    private void Dodaj()
    {
        var nova = new LdPorstStavka { Oddat = DateTime.Today, Dodat = DateTime.Today };
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
    private void Preuzimanje()
    {
        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        var ldradPath = PronadjiDbf(_folderPath, "ldrad.dbf");
        if (ldradPath is null)
        {
            Poruka = "ldrad.dbf nije pronađen.";
            return;
        }

        try
        {
            var radnici = DbfReader.CitajSveZapise(ldradPath);
            int dodato = 0;
            var postojeciBrojevi = new HashSet<int>(Stavke.Select(s => s.Broj));

            foreach (var r in radnici)
            {
                int broj = Int(r, "BROJ");
                if (broj <= 0 || postojeciBrojevi.Contains(broj))
                    continue;

                string vrsta = Str(r, "VRSTA");
                if (string.IsNullOrWhiteSpace(vrsta) || vrsta.Equals("N", StringComparison.OrdinalIgnoreCase))
                    continue;

                Stavke.Add(new LdPorstStavka
                {
                    Broj    = broj,
                    ImePrez = Str(r, "IME_PREZ"),
                    Oddat   = DateTime.Today,
                    Dodat   = DateTime.Today,
                    Idbr    = Long(r, "IDBR")
                });
                postojeciBrojevi.Add(broj);
                dodato++;
            }

            Poruka = $"Preuzeto {dodato} zaposlenih iz LDRAD.";
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

        _dbfPath ??= PronadjiDbf(_folderPath, "ldporsta.dbf");
        if (_dbfPath is null)
        {
            Poruka = "ldporsta.dbf nije pronađen.";
            return;
        }

        try
        {
            _schema ??= DbfTableWriter.LoadSchema(_dbfPath);
            var redovi = Stavke.Select(s => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["BROJ"]       = (decimal)s.Broj,
                ["IME_PREZ"]   = (s.ImePrez ?? string.Empty).Trim(),
                ["GOD"]        = (decimal)s.God,
                ["MESECI"]     = (decimal)s.Meseci,
                ["DANA"]       = (decimal)s.Dana,
                ["PROCVR"]     = (decimal)s.Procvr,
                ["ODDAT"]      = s.Oddat,
                ["DODAT"]      = s.Dodat,
                ["ODDATNEGA"]  = s.Oddatnega,
                ["DODATNEGA"]  = s.Dodatnega,
                ["DATPOS"]     = s.Datpos,
                ["DATZAHTEV"]  = s.Datzahtev,
                ["DATDOZNA"]   = s.Datdozna,
                ["ZCNAZIV"]    = (s.Zcnaziv ?? string.Empty).Trim(),
                ["ZCMESTO"]    = (s.Zcmesto ?? string.Empty).Trim(),
                ["IZVODMK"]    = (s.Izvodmk ?? string.Empty).Trim(),
                ["IZVODMES"]   = (s.Izvodmes ?? string.Empty).Trim(),
                ["DETEIME"]    = (s.Deteime ?? string.Empty).Trim(),
                ["DETERODJ"]   = s.Deterodj,
                ["DETERED"]    = (s.Detered ?? string.Empty).Trim(),
                ["NAZSUDA"]    = (s.Nazsuda ?? string.Empty).Trim(),
                ["PRENETO"]    = (s.Preneto ?? string.Empty).Trim(),
                ["IDBR"]       = (decimal)s.Idbr
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
            Sifra  = s.Broj.ToString(CultureInfo.InvariantCulture),
            Naziv  = s.ImePrez ?? string.Empty,
            Iznos1 = s.God,
            Iznos2 = s.Meseci
        }).ToList();

        var view = new Views.Zarade.FoxPregledTabelaView(
            "POTVRDA O STAZU",
            "Radni staz zaposlenih na porodiljskom odsustvu",
            redovi,
            "GODINA",
            "MESECI");
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

        _dbfPath = PronadjiDbf(_folderPath, "ldporsta.dbf");
        if (_dbfPath is null)
        {
            Stavke = [];
            Poruka = "ldporsta.dbf nije pronađen — tabela je prazna.";
            return;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(_dbfPath);
            var lista = zapisi.Select(z => new LdPorstStavka
            {
                Broj       = Int(z, "BROJ"),
                ImePrez    = Str(z, "IME_PREZ"),
                God        = Int(z, "GOD"),
                Meseci     = Int(z, "MESECI"),
                Dana       = Int(z, "DANA"),
                Procvr     = Int(z, "PROCVR"),
                Oddat      = Dat(z, "ODDAT"),
                Dodat      = Dat(z, "DODAT"),
                Oddatnega  = Dat(z, "ODDATNEGA"),
                Dodatnega  = Dat(z, "DODATNEGA"),
                Datpos     = Dat(z, "DATPOS"),
                Datzahtev  = Dat(z, "DATZAHTEV"),
                Datdozna   = Dat(z, "DATDOZNA"),
                Zcnaziv    = Str(z, "ZCNAZIV"),
                Zcmesto    = Str(z, "ZCMESTO"),
                Izvodmk    = Str(z, "IZVODMK"),
                Izvodmes   = Str(z, "IZVODMES"),
                Deteime    = Str(z, "DETEIME"),
                Deterodj   = Dat(z, "DETERODJ"),
                Detered    = Str(z, "DETERED"),
                Nazsuda    = Str(z, "NAZSUDA"),
                Preneto    = Str(z, "PRENETO"),
                Idbr       = Long(z, "IDBR")
            }).ToList();

            Stavke = new ObservableCollection<LdPorstStavka>(lista);
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
            if (File.Exists(path)) return path;
            var upper = Path.Combine(folder, name.ToUpperInvariant());
            if (File.Exists(upper)) return upper;
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

    private static DateTime Dat(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return DateTime.MinValue;
        if (v is DateTime dt) return dt;
        if (DateTime.TryParse(v.ToString(), out var p)) return p;
        return DateTime.MinValue;
    }
}

public partial class LdPorstStavka : ObservableObject
{
    [ObservableProperty] private int _broj;
    [ObservableProperty] private string _imePrez = string.Empty;
    [ObservableProperty] private int _god;
    [ObservableProperty] private int _meseci;
    [ObservableProperty] private int _dana;
    [ObservableProperty] private int _procvr;
    [ObservableProperty] private DateTime _oddat = DateTime.MinValue;
    [ObservableProperty] private DateTime _dodat = DateTime.MinValue;
    [ObservableProperty] private DateTime _oddatnega = DateTime.MinValue;
    [ObservableProperty] private DateTime _dodatnega = DateTime.MinValue;
    [ObservableProperty] private DateTime _datpos = DateTime.MinValue;
    [ObservableProperty] private DateTime _datzahtev = DateTime.MinValue;
    [ObservableProperty] private DateTime _datdozna = DateTime.MinValue;
    [ObservableProperty] private string _zcnaziv = string.Empty;
    [ObservableProperty] private string _zcmesto = string.Empty;
    [ObservableProperty] private string _izvodmk = string.Empty;
    [ObservableProperty] private string _izvodmes = string.Empty;
    [ObservableProperty] private string _deteime = string.Empty;
    [ObservableProperty] private DateTime _deterodj = DateTime.MinValue;
    [ObservableProperty] private string _detered = string.Empty;
    [ObservableProperty] private string _nazsuda = string.Empty;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private long _idbr;
}
