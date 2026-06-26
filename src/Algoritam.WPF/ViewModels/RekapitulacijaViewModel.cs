using Algoritam.Application;
using Algoritam.Domain.Entities;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Fox forma: LDREKAP / ldrekap2.prg + ldrek.prg + ldpodaci.prg (PUNIPOD01).
/// Rekapitulacija zarade — 34 stavke sa zbirnim iznosima svih radnika iz platnog spiska.
/// Identičan Fox tok: sumira polja iz LD tabele (LdObracunStavke) po svim radnicima.
/// </summary>
public partial class RekapitulacijaViewModel : ObservableObject
{
    private readonly AppState _appState;
    private List<LdObracunStavka> _obracunStavke = [];
    private LdParametar? _parametar;

    [ObservableProperty]
    private ObservableCollection<RekapitulacijaStavka> _stavke = [];

    [ObservableProperty]
    private string _poruka = string.Empty;

    [ObservableProperty]
    private string _naslov = "REKAPITULACIJA ZARADE";

    [ObservableProperty]
    private int _mesec;

    [ObservableProperty]
    private int _isplata;

    [ObservableProperty]
    private string _datumIsplate = string.Empty;

    [ObservableProperty]
    private int _brojRadnika;

    public RekapitulacijaViewModel(AppState appState)
    {
        _appState = appState;
        _ = UcitajAsync();
    }

    [RelayCommand]
    private async Task UcitajAsync()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Stavke = [];
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        try
        {
            // Ucitaj parametre iz ldparam.dbf
            var param = UcitajParametarIzDbf(folder);
            if (param == null)
            {
                Poruka = "Parametri nisu pronađeni (ldparam.dbf).";
                return;
            }
            _parametar = param;

            int redispl = param.Redispl;
            if (redispl < 1) redispl = 1;
            if (redispl > 4) redispl = 4;

            Mesec = param.Mesec;
            Isplata = redispl;

            DatumIsplate = redispl switch
            {
                1 => param.Dat1?.ToString("dd.MM.yyyy") ?? "",
                2 => param.Dat2?.ToString("dd.MM.yyyy") ?? "",
                3 => param.Dat3?.ToString("dd.MM.yyyy") ?? "",
                _ => param.Dat4?.ToString("dd.MM.yyyy") ?? ""
            };

            // Ucitaj sve radnike iz LD*.dbf fajlova
            var sviRadnici = await Task.Run(() => LdObracunDbfReader.CitajSve(folder));
            var radnici = sviRadnici
                .Where(r => r.Mesec == Mesec)
                .ToList();
            _obracunStavke = radnici;

            BrojRadnika = radnici.Count;

            if (radnici.Count == 0)
            {
                Stavke = [];
                Poruka = $"Nema radnika za rekapitulaciju za mesec {Mesec}.";
                Naslov = $"REKAPITULACIJA ZARADE — Mesec: {Mesec}, Isplata: {Isplata} (prazno)";
                return;
            }

            // ══════════════════════════════════════════════════════════════
            //  Sumiranje — identično Fox ldpodaci.prg linije 664–749
            //  DO WHILE .NOT. EOF() → SUM svih polja
            // ══════════════════════════════════════════════════════════════

            var zbir = new ZbirniPodaci();

            foreach (var r in radnici)
            {
                zbir.Bruto += r.Bruto;
                zbir.Neto += r.Neto;
                zbir.Porez += r.Porez + r.Porezu;
                zbir.DopSocR += r.Dopsocr;       // doprinosi radnika ukupno
                zbir.DopSocF += r.Dopsocf;       // doprinosi firma ukupno
                zbir.Doppr += r.Doppr;            // PIO radnika
                zbir.Dopzr += r.Dopzr;            // zdravstveno radnika
                zbir.Dopnr += r.Dopnr;            // zapošljavanje radnika
                zbir.Doppf += r.Doppf;            // PIO firma
                zbir.Dopzf += r.Dopzf;            // zdravstveno firma
                zbir.Dopnf += r.Dopnf;            // zapošljavanje firma
                zbir.Bendin += r.Bendin;          // beneficirani staž
                zbir.Komorajd += r.Komorajd;      // komora jugoslovenska/ukupno
                zbir.Komorasd += r.Komorasd;      // komora Srbije
                zbir.Komorard += r.Komorard;      // komora regiona
                zbir.Krediti += r.Krediti + r.Kreditia;
                zbir.Akontac += r.Akontac;
                zbir.Solidarn += r.Solidarn;
                zbir.Samodopr += r.Samodopr;
                zbir.Sindikat1 += r.Sindikat1;
                zbir.Sindikat2 += r.Sindikat2;
                zbir.Aliment += r.Aliment;
                zbir.Kasa += r.Kasa;
                zbir.Kasarata += r.Kasarata;
                zbir.Prevoz += r.Prevoz;          // obustava prevoza
                zbir.Netoprev += r.Netoprev;      // naknada prevoza
                zbir.Obust1 += r.Obust1;
                zbir.Obust2 += r.Obust2;
                zbir.Obust3 += r.Obust3;
                zbir.Ukobust += r.Ukobust;
                zbir.Zaisplatu += r.Zaisplatu;
            }

            // ══════════════════════════════════════════════════════════════
            //  Popuni 34 stavki rekapitulacije — ekvivalent ldrekap2.prg
            //  REKUNESI definicije + REKPUNI logika (SADA = SUM iz LD)
            // ══════════════════════════════════════════════════════════════

            var rezultat = new List<RekapitulacijaStavka>
            {
                R( 1, "24.",  " BRUTO ZARADA",               zbir.Bruto,     true),
                R( 2, "22.",  " POREZ NA ZARADE",             zbir.Porez,     false),
                R( 3, "26.",  " DOPRINOSI NA TERET RADNIKA",  zbir.DopSocR,   false),
                R( 4, "21.",  " NETO ZARADA",                 zbir.Neto,      true),
                R( 5, "26.",  " DOPRINOSI RADNIKA",           zbir.DopSocR,   false),
                R( 6, "27.",  "   PIO",                       zbir.Doppr,     false),
                R( 7, "28.",  "   ZDRAVSTVENO",               zbir.Dopzr,     false),
                R( 8, "29.",  "   ZAPOSLJAVANJE",             zbir.Dopnr,     true),
                R( 9, "30.",  " DOPRINOSI POSLODAVCA",        zbir.DopSocF,   false),
                R(10, "31.",  "   PIO",                       zbir.Doppf,     false),
                R(11, "32.",  "   ZDRAVSTVENO",               zbir.Dopzf,     false),
                R(12, "33.",  "   ZAPOSLJAVANJE",             zbir.Dopnf,     true),
                R(13, "25.",  " POREZ NA FOND ZARADA",        0m,             false),
                R(14, "52.",  " DOP ZA BENEFIC.STAZ",         zbir.Bendin,    true),
                R(15, "34.",  " CLANARINA KOMORI",            zbir.Komorajd,  false),
                R(16, "35.",  " CLANARINA KOMORI SRBIJE",     zbir.Komorasd,  false),
                R(17, "36.",  " CLANARINA KOMORI REGIONA",    zbir.Komorard,  true),
                R(18, "153.", " NAKNADA PREVOZA",             zbir.Netoprev,  true),
                // Naslov sekcije — nema iznos
                new() { RedniBroj = 19, Opis = "      OBUSTAVE" },
                R(20, "38.",  " KREDITI",                     zbir.Krediti,   false),
                R(21, "39.",  " AKONTACIJA",                  zbir.Akontac,   false),
                R(22, "40.",  " SOLIDARNOST",                 zbir.Solidarn,  false),
                R(23, "41.",  " SAMODOPRINOS",                zbir.Samodopr,  false),
                R(24, "42.",  " SINDIKAT 1",                  zbir.Sindikat1, false),
                R(25, "43.",  " SINDIKAT 2",                  zbir.Sindikat2, false),
                R(26, "44.",  " ALIMENTACIJA/IZVRSITELJ",     zbir.Aliment,   false),
                R(27, "45.",  " KASA",                        zbir.Kasa,      false),
                R(28, "46.",  " KASA RATA",                   zbir.Kasarata,  false),
                R(29, "154.", " OBUSTAVA PREVOZA",            zbir.Prevoz,    true),
                R(30, "47.",  " OSTALE OBUSTAVE 1",           zbir.Obust1,    true),
                R(31, "48.",  " OSTALE OBUSTAVE 2",           zbir.Obust2,    true),
                R(32, "49.",  " OSTALE OBUSTAVE 3",           zbir.Obust3,    true),
                R(33, "50.",  " UKUPNE OBUSTAVE",             zbir.Ukobust,   true),
                R(34, "51.",  " ZA ISPLATU",                  zbir.Zaisplatu, true),
            };

            Stavke = new ObservableCollection<RekapitulacijaStavka>(rezultat);

            Naslov = $"REKAPITULACIJA ZARADE — Mesec: {Mesec}, Isplata: {Isplata}";
            Poruka = $"Ucitano {BrojRadnika} radnika. Bruto: {zbir.Bruto:N2} | Neto: {zbir.Neto:N2} | Za isplatu: {zbir.Zaisplatu:N2}";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RekapitulacijaF7()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za pregled.";
            return;
        }
        Poruka = "Pregled rekapitulacije F7 — podaci su prikazani u tabeli.";
    }

    [RelayCommand]
    private void StampaF10()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za stampu.";
            return;
        }

        try
        {
            var view = new Views.Zarade.ZbirnaRekapitulacijaReportView(
                _obracunStavke,
                _parametar,
                _appState.AktivnaFirma,
                DatumIsplateDatum(_parametar));

            var aktivniProzor = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (aktivniProzor != null && !ReferenceEquals(aktivniProzor, view))
                view.Owner = aktivniProzor;

            view.ShowDialog();
            Poruka = "Otvoren FOX prikaz zbirne rekapitulacije za stampu.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri stampi: {ex.Message}";
        }
    }

    private static DateTime? DatumIsplateDatum(LdParametar? parametar)
    {
        if (parametar == null)
            return null;

        return parametar.Redispl switch
        {
            1 => parametar.Dat1,
            2 => parametar.Dat2,
            3 => parametar.Dat3,
            _ => parametar.Dat4
        };
    }

    private string FormirajTekstZaStampu()
    {
        var sb = new System.Text.StringBuilder();
        var firmaName = _appState.AktivnaFirma?.Naziv ?? "";

        sb.AppendLine($"  {firmaName}");
        sb.AppendLine($"  REKAPITULACIJA ZARADE — Mesec: {Mesec}, Isplata: {Isplata}");
        if (!string.IsNullOrWhiteSpace(DatumIsplate))
            sb.AppendLine($"  Datum isplate: {DatumIsplate}");
        sb.AppendLine($"  Broj radnika: {BrojRadnika}");
        sb.AppendLine();
        sb.AppendLine($"  {"R.br.",-6} {"Kod",-8} {"Opis",-30} {"Iznos",15}");
        sb.AppendLine(new string('-', 65));

        foreach (var s in Stavke)
        {
            if (string.IsNullOrWhiteSpace(s.Kod))
            {
                sb.AppendLine($"  {"",6} {"",8} {s.Opis,-30}");
            }
            else
            {
                sb.AppendLine($"  {s.RedniBroj,-6} {s.Kod,-8} {s.Opis,-30} {s.Sada,15:N2}");
            }

            if (s.ImaLiniju)
                sb.AppendLine(new string('-', 65));
        }

        sb.AppendLine(new string('=', 65));
        return sb.ToString();
    }

    /// <summary>
    /// Helper — kreira jednu stavku rekapitulacije sa iznosom.
    /// </summary>
    private static RekapitulacijaStavka R(int rb, string kod, string opis, decimal iznos, bool linija) => new()
    {
        RedniBroj = rb,
        Kod = kod,
        Opis = opis,
        Sada = iznos,
        ImaLiniju = linija
    };

    private static LdParametar? UcitajParametarIzDbf(string folder)
    {
        var putanja = LdObracunDbfReader.PronadjiDbf(folder, "ldparam.dbf");
        if (putanja == null) return null;

        try
        {
            var zapisi = DbfReader.CitajSveZapise(putanja);
            var red = zapisi.FirstOrDefault();
            if (red == null) return null;

            static string Str(Dictionary<string, object?> z, string k)
                => z.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;
            static int Int(Dictionary<string, object?> z, string k)
            {
                if (!z.TryGetValue(k, out var v) || v == null) return 0;
                if (v is decimal d) return (int)d;
                if (v is int i) return i;
                if (int.TryParse(v.ToString(), out var p)) return p;
                return 0;
            }
            static DateTime? Dat(Dictionary<string, object?> z, string k)
            {
                if (!z.TryGetValue(k, out var v) || v == null) return null;
                if (v is DateTime dt) return dt;
                if (DateTime.TryParse(v.ToString(), out var p)) return p;
                return null;
            }

            return new LdParametar
            {
                Mesec   = Int(red, "MESEC"),
                Godina  = Str(red, "GODINA"),
                Isplata = Int(red, "ISPLATA"),
                Redispl = Int(red, "REDISPL"),
                Dat1    = Dat(red, "DAT1"),
                Dat2    = Dat(red, "DAT2"),
                Dat3    = Dat(red, "DAT3"),
                Dat4    = Dat(red, "DAT4"),
            };
        }
        catch { return null; }
    }

    /// <summary>
    /// Zbirni podaci sumirani iz svih radnika platnog spiska.
    /// Fox: ldpodaci.prg linije 585–749 — M-varijable.
    /// </summary>
    private class ZbirniPodaci
    {
        public decimal Bruto;
        public decimal Neto;
        public decimal Porez;        // Porez + Porezu
        public decimal DopSocR;      // Dopsocr — doprinosi na teret radnika ukupno
        public decimal DopSocF;      // Dopsocf — doprinosi firma ukupno
        public decimal Doppr;        // PIO radnika
        public decimal Dopzr;        // Zdravstveno radnika
        public decimal Dopnr;        // Zapošljavanje radnika
        public decimal Doppf;        // PIO firma
        public decimal Dopzf;        // Zdravstveno firma
        public decimal Dopnf;        // Zapošljavanje firma
        public decimal Bendin;       // Beneficirani staž
        public decimal Komorajd;     // Komora
        public decimal Komorasd;     // Komora Srbije
        public decimal Komorard;     // Komora regiona
        public decimal Krediti;      // Krediti + Kreditia
        public decimal Akontac;
        public decimal Solidarn;
        public decimal Samodopr;
        public decimal Sindikat1;
        public decimal Sindikat2;
        public decimal Aliment;
        public decimal Kasa;
        public decimal Kasarata;
        public decimal Prevoz;       // Obustava prevoza
        public decimal Netoprev;     // Naknada prevoza
        public decimal Obust1;
        public decimal Obust2;
        public decimal Obust3;
        public decimal Ukobust;
        public decimal Zaisplatu;
    }

    [RelayCommand]
    private Task OsveziAsync() => UcitajAsync();
}
