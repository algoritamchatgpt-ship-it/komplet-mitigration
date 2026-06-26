using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Algoritam.WPF.ViewModels;

public sealed class LdTekstIzjavaStavka
{
    public string Tekst1 { get; set; } = string.Empty;
    public string Tekst2 { get; set; } = string.Empty;
    public string Tekst3 { get; set; } = string.Empty;
    public string Tekst4 { get; set; } = string.Empty;
    public string Tekst5 { get; set; } = string.Empty;
    public string Tekst6 { get; set; } = string.Empty;
    public string Tekst7 { get; set; } = string.Empty;
    public string Tekst8 { get; set; } = string.Empty;
    public string Tekst9 { get; set; } = string.Empty;
    public string Tekst10 { get; set; } = string.Empty;
    public string Tekst11 { get; set; } = string.Empty;
    public string Tekst12 { get; set; } = string.Empty;
    public string Tekst13 { get; set; } = string.Empty;
    public string Tekst14 { get; set; } = string.Empty;
    public string Tekst15 { get; set; } = string.Empty;
    public string Preneto { get; set; } = string.Empty;
    public long Idbr { get; set; }
}

internal sealed record LdTekstIzjavaConfig(
    string DbfName,
    string Naslov,
    string NazivPregleda,
    int BrojTekstPolja,
    Func<Dictionary<string, object?>?, DateTime, IReadOnlyList<string>> KreirajPodrazumevaniTekst,
    string TekstFieldPrefix = "TEKST");

public partial class LdTekstIzjavaViewModel : ObservableObject
{
    private static readonly LdTekstIzjavaConfig ConfigInv2 = new(
        "ldinv2.dbf",
        "IZJAVA O ISPUNJAVANJU USLOVA ZA ODLAZAK U PENZIJU",
        "LDINV20 - PREGLED",
        15,
        (_, _) => new[]
        {
            "                          Potvrda",
            "Potvrdjujem da zaposleni invalidi rada II i III kategorije za koje je",
            "ispostavljen zahtev za refundaciju za mesec                          ",
            "godine, broj               od                   , prema podacima iz",
            "kadrovske evidencije ne ispunjava uslov za odlazak u starosnu penziju.",
            "Ova potvrda se izdaje u skladu sa clanom 227. Zakona o penzijskom i",
            "invalidskom osiguranju, prema kome osiguraniku prestaje pravo na",
            "isplatu novcane naknade po osnovu II i III kategorije invalidnosti",
            "danom ispunjenja uslova za starosnu penziju.",
            "Davalac potvrde izjavljuje da ce snositi materijalnu odgovornost u",
            "slucaju davanja netacnih podataka.",
            "Ime i prezime ovlascenog lica _______________________________",
            string.Empty,
            "Potpis i pecat _______________________________",
            string.Empty
        });

    private static readonly LdTekstIzjavaConfig ConfigInv3 = new(
        "ldinv3.dbf",
        "POTVRDA O IZVRSENOJ ISPLATI",
        "LDINV30 - PREGLED",
        10,
        (_, _) => new[]
        {
            "                          Potvrda",
            "Potvrdjujem da je istovremeno sa obracunom i isplatom zarade za",
            "sve zaposlene za mesec                                  godine,",
            "izvrsen obracun i isplata zarade invalida rada II i III kategorije",
            "invalidnosti.",
            "Davalac potvrde izjavljuje da ce snositi materijalnu odgovornost u",
            "slucaju davanja netacnih podataka.",
            "Ime i prezime ovlascenog lica _______________________________",
            string.Empty,
            "Potpis i pecat _______________________________"
        });

    private static readonly LdTekstIzjavaConfig ConfigIzjavaDop = new(
        "ldizjdop.dbf",
        "IZJAVA O IZMIRENIM DOPRINOSIMA",
        "LDIZJDOP0 - PREGLED",
        7,
        (firma, danas) =>
        {
            var fime = FirmaTekst(firma, "FIME");
            var fmes = FirmaTekst(firma, "FMES");
            var ful = FirmaTekst(firma, "FUL");
            var fulbr = FirmaTekst(firma, "FULBR");

            return new[]
            {
                " Izjavljujem pod materijalnom i krivicnom odgovornoscu da je ",
                $"{fime} iz {fmes}".Trim(),
                $"{ful} br.{fulbr}".Trim(),
                $"na dan {danas:dd.MM.yyyy} izmirio dospeli doprinos za zdravstveno ",
                "osiguranje za sve zaposlene kod poslodavca",
                string.Empty,
                string.Empty
            };
        });

    private static readonly LdTekstIzjavaConfig ConfigZzoMarkice = new(
        "ldzzo01.dbf",
        "ZAHTEV ZA MARKICE",
        "LDZZO01 - PREGLED",
        5,
        (firma, danas) =>
        {
            var fime = FirmaTekst(firma, "FIME");
            var fmes = FirmaTekst(firma, "FMES");

            return new[]
            {
                "Zahtev za overu zdravstvenih knjizica / markica",
                $"{fime} {fmes}".Trim(),
                $"Datum: {danas:dd.MM.yyyy}",
                "Za zaposlene navedene u evidenciji osiguranja.",
                "Potpis i pecat _______________________________"
            };
        },
        "TEXT");

    private static readonly LdTekstIzjavaConfig ConfigZzoOvlascenje = new(
        "ldzzo02.dbf",
        "OVLASCENJE ZA ZDRAVSTVENE KNJIZICE",
        "LDZZO02 - PREGLED",
        5,
        (firma, danas) =>
        {
            var fime = FirmaTekst(firma, "FIME");
            var fmes = FirmaTekst(firma, "FMES");

            return new[]
            {
                "Ovlascenje za preuzimanje zdravstvenih knjizica / markica",
                $"{fime} {fmes}".Trim(),
                $"Datum: {danas:dd.MM.yyyy}",
                "Ovlascuje se lice _______________________________",
                "Potpis i pecat _______________________________"
            };
        },
        "TEXT");

    private readonly string _folderPath;
    private readonly LdTekstIzjavaConfig _config;

    [ObservableProperty] private string _naslov = string.Empty;
    [ObservableProperty] private ObservableCollection<LdTekstIzjavaStavka> _stavke = [];
    [ObservableProperty] private LdTekstIzjavaStavka? _selektovana;
    [ObservableProperty] private string _poruka = string.Empty;

    public event Action? ZatvaranjeZatrazeno;
    public int BrojTekstPolja => _config.BrojTekstPolja;

    public static LdTekstIzjavaViewModel CreateInv2(AppState appState) => new(appState, ConfigInv2);
    public static LdTekstIzjavaViewModel CreateInv3(AppState appState) => new(appState, ConfigInv3);
    public static LdTekstIzjavaViewModel CreateIzjavaDop(AppState appState) => new(appState, ConfigIzjavaDop);
    public static LdTekstIzjavaViewModel CreateZzoMarkice(string folderPath) => new(folderPath, ConfigZzoMarkice);
    public static LdTekstIzjavaViewModel CreateZzoOvlascenje(string folderPath) => new(folderPath, ConfigZzoOvlascenje);

    private LdTekstIzjavaViewModel(AppState appState, LdTekstIzjavaConfig config)
        : this(appState.AktivnaFirma?.FolderPath ?? string.Empty, config)
    {
    }

    private LdTekstIzjavaViewModel(string folderPath, LdTekstIzjavaConfig config)
    {
        _folderPath = folderPath;
        _config = config;
        Naslov = config.Naslov;
        Ucitaj();
    }

    [RelayCommand]
    private void Prvi()
    {
        if (Stavke.Count == 0)
            return;

        Selektovana = Stavke[0];
    }

    [RelayCommand]
    private void Zadnji()
    {
        if (Stavke.Count == 0)
            return;

        Selektovana = Stavke[^1];
    }

    [RelayCommand]
    private void Gore()
    {
        if (Selektovana is null)
            return;

        var indeks = Stavke.IndexOf(Selektovana);
        if (indeks > 0)
            Selektovana = Stavke[indeks - 1];
    }

    [RelayCommand]
    private void Dole()
    {
        if (Selektovana is null)
            return;

        var indeks = Stavke.IndexOf(Selektovana);
        if (indeks >= 0 && indeks < Stavke.Count - 1)
            Selektovana = Stavke[indeks + 1];
    }

    [RelayCommand]
    private void UnesiTekst()
    {
        if (Selektovana is null)
        {
            Selektovana = new LdTekstIzjavaStavka();
            Stavke.Add(Selektovana);
        }

        var firma = UcitajFirmu();
        var podrazumevaniTekst = _config.KreirajPodrazumevaniTekst(firma, DateTime.Today);
        PrimeniTekst(Selektovana, podrazumevaniTekst, _config.BrojTekstPolja);
        Sacuvaj();

        Poruka = "Tekst je unet.";
    }

    [RelayCommand]
    private void Pregled()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za pregled.";
            return;
        }

        var view = new Views.Zarade.LdBolGenericReportView(
            _config.NazivPregleda,
            Stavke.ToList(),
            Stavke.Count);
        view.ShowDialog();
    }

    [RelayCommand]
    private void Brisanje()
    {
        if (Selektovana is null)
        {
            Poruka = "Nije selektovan red.";
            return;
        }

        Stavke.Remove(Selektovana);
        Selektovana = Stavke.FirstOrDefault();
        Sacuvaj();
        Poruka = "Obrisan je red.";
    }

    [RelayCommand]
    private void SacuvajRucno()
    {
        Sacuvaj();
        Poruka = "Izmene su sačuvane.";
    }

    [RelayCommand]
    private void Izlaz() => ZatvaranjeZatrazeno?.Invoke();

    private void Ucitaj()
    {
        Stavke.Clear();

        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        var dbfPath = LdBolovanjeDbfSupport.PronadjiDbf(_folderPath, _config.DbfName);
        if (dbfPath is null)
        {
            Poruka = $"{_config.DbfName} nije pronađen.";
            return;
        }

        try
        {
            foreach (var red in DbfReader.CitajSveZapise(dbfPath))
            {
                Stavke.Add(new LdTekstIzjavaStavka
                {
                    Tekst1 = LdBolovanjeDbfSupport.Str(red, TekstFieldName(1)),
                    Tekst2 = LdBolovanjeDbfSupport.Str(red, TekstFieldName(2)),
                    Tekst3 = LdBolovanjeDbfSupport.Str(red, TekstFieldName(3)),
                    Tekst4 = LdBolovanjeDbfSupport.Str(red, TekstFieldName(4)),
                    Tekst5 = LdBolovanjeDbfSupport.Str(red, TekstFieldName(5)),
                    Tekst6 = LdBolovanjeDbfSupport.Str(red, TekstFieldName(6)),
                    Tekst7 = LdBolovanjeDbfSupport.Str(red, TekstFieldName(7)),
                    Tekst8 = LdBolovanjeDbfSupport.Str(red, TekstFieldName(8)),
                    Tekst9 = LdBolovanjeDbfSupport.Str(red, TekstFieldName(9)),
                    Tekst10 = LdBolovanjeDbfSupport.Str(red, TekstFieldName(10)),
                    Tekst11 = LdBolovanjeDbfSupport.Str(red, TekstFieldName(11)),
                    Tekst12 = LdBolovanjeDbfSupport.Str(red, TekstFieldName(12)),
                    Tekst13 = LdBolovanjeDbfSupport.Str(red, TekstFieldName(13)),
                    Tekst14 = LdBolovanjeDbfSupport.Str(red, TekstFieldName(14)),
                    Tekst15 = LdBolovanjeDbfSupport.Str(red, TekstFieldName(15)),
                    Preneto = LdBolovanjeDbfSupport.Str(red, "PRENETO"),
                    Idbr = LdBolovanjeDbfSupport.Long(red, "IDBR")
                });
            }

            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Ucitano {Stavke.Count} redova iz {_config.DbfName}.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri ucitavanju: {ex.Message}";
        }
    }

    private void Sacuvaj()
    {
        if (string.IsNullOrWhiteSpace(_folderPath))
            return;

        try
        {
            LdBolovanjeDbfSupport.SacuvajTabelu(
                _folderPath,
                _config.DbfName,
                Stavke.ToList(),
                ResolveValue);
        }
        catch
        {
            // Poruka postavlja caller.
        }
    }

    private Dictionary<string, object?>? UcitajFirmu()
    {
        var firmaPath = LdBolovanjeDbfSupport.PronadjiDbf(_folderPath, "firma.dbf");
        if (firmaPath is null)
            return null;

        return DbfReader.CitajSveZapise(firmaPath).FirstOrDefault();
    }

    private static string FirmaTekst(Dictionary<string, object?>? firma, string kljuc)
    {
        if (firma is null)
            return string.Empty;

        return LdBolovanjeDbfSupport.Str(firma, kljuc);
    }

    private static void PrimeniTekst(LdTekstIzjavaStavka stavka, IReadOnlyList<string> tekstovi, int brojTekstPolja)
    {
        static string Uzmi(IReadOnlyList<string> src, int indeks) => indeks < src.Count ? src[indeks] : string.Empty;

        if (brojTekstPolja >= 1) stavka.Tekst1 = Uzmi(tekstovi, 0);
        if (brojTekstPolja >= 2) stavka.Tekst2 = Uzmi(tekstovi, 1);
        if (brojTekstPolja >= 3) stavka.Tekst3 = Uzmi(tekstovi, 2);
        if (brojTekstPolja >= 4) stavka.Tekst4 = Uzmi(tekstovi, 3);
        if (brojTekstPolja >= 5) stavka.Tekst5 = Uzmi(tekstovi, 4);
        if (brojTekstPolja >= 6) stavka.Tekst6 = Uzmi(tekstovi, 5);
        if (brojTekstPolja >= 7) stavka.Tekst7 = Uzmi(tekstovi, 6);
        if (brojTekstPolja >= 8) stavka.Tekst8 = Uzmi(tekstovi, 7);
        if (brojTekstPolja >= 9) stavka.Tekst9 = Uzmi(tekstovi, 8);
        if (brojTekstPolja >= 10) stavka.Tekst10 = Uzmi(tekstovi, 9);
        if (brojTekstPolja >= 11) stavka.Tekst11 = Uzmi(tekstovi, 10);
        if (brojTekstPolja >= 12) stavka.Tekst12 = Uzmi(tekstovi, 11);
        if (brojTekstPolja >= 13) stavka.Tekst13 = Uzmi(tekstovi, 12);
        if (brojTekstPolja >= 14) stavka.Tekst14 = Uzmi(tekstovi, 13);
        if (brojTekstPolja >= 15) stavka.Tekst15 = Uzmi(tekstovi, 14);
    }

    private string TekstFieldName(int indeks) => $"{_config.TekstFieldPrefix}{indeks}";

    private object? ResolveValue(LdTekstIzjavaStavka row, string fieldName)
    {
        for (var i = 1; i <= 15; i++)
        {
            if (fieldName.Equals(TekstFieldName(i), StringComparison.OrdinalIgnoreCase))
                return LdBolovanjeDbfSupport.NormalizeText(GetTekst(row, i));
        }

        return fieldName.ToUpperInvariant() switch
        {
            "PRENETO" => LdBolovanjeDbfSupport.NormalizeText(row.Preneto),
            "IDBR" => row.Idbr,
            _ => null
        };
    }

    private static string GetTekst(LdTekstIzjavaStavka row, int indeks) => indeks switch
    {
        1 => row.Tekst1,
        2 => row.Tekst2,
        3 => row.Tekst3,
        4 => row.Tekst4,
        5 => row.Tekst5,
        6 => row.Tekst6,
        7 => row.Tekst7,
        8 => row.Tekst8,
        9 => row.Tekst9,
        10 => row.Tekst10,
        11 => row.Tekst11,
        12 => row.Tekst12,
        13 => row.Tekst13,
        14 => row.Tekst14,
        15 => row.Tekst15,
        _ => string.Empty
    };
}
