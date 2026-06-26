using Algoritam.Domain.Entities;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Algoritam.WPF.Views.Zarade;

public partial class ZbirnaRekapitulacijaReportView : Window
{
    public string FirmaNaziv { get; set; }
    public string TekuciRacun { get; set; }
    public string Telefon { get; set; }
    public string Fax { get; set; }
    public string DatumIsplateTekst { get; set; }
    public string RedniBrIsplateTekst { get; set; }
    public string MesecNaziv { get; set; }
    public string MesecBrojTekst { get; set; }
    public string GodinaTekst { get; set; }
    public string VrstaIsplateTekst { get; set; }
    public string CenaRadaTekst { get; set; }
    public string CasoviMesecTekst { get; set; }
    public string CasoviPraznikTekst { get; set; }
    public string ZakonskiSatiTekst { get; set; }

    public IReadOnlyList<DetaljRow> RedovnaRows { get; set; }
    public IReadOnlyList<DetaljRow> NaknadeRows { get; set; }
    public IReadOnlyList<DetaljRow> DodatnaRows { get; set; }
    public IReadOnlyList<DetaljRow> OstaliDodaciRows { get; set; }

    public IReadOnlyList<IznosRow> StimulacijaRows { get; set; }
    public IReadOnlyList<IznosRow> OstalaPrimanjaRows { get; set; }
    public IReadOnlyList<IznosRow> BrutoRows { get; set; }
    public IReadOnlyList<IznosRow> DoprinosPoslodavcaRows { get; set; }
    public IReadOnlyList<IznosRow> NetoRows { get; set; }
    public IReadOnlyList<IznosRow> ObustaveRows { get; set; }

    public string UkupnoRedovnaCas { get; set; }
    public string UkupnoRedovnaIznos { get; set; }
    public string UkupnoNaknadeCas { get; set; }
    public string UkupnoNaknadeIznos { get; set; }
    public string UkupnoDodatnaCas { get; set; }
    public string UkupnoDodatnaIznos { get; set; }
    public string UkupnoOstaliDodaciIznos { get; set; }
    public string Ukupno1234Cas { get; set; }
    public string Ukupno1234Iznos { get; set; }
    public string UkupnaStimulacija { get; set; }
    public string UkupnaOstalaPrimanja { get; set; }

    public string UkupneObustave { get; set; }
    public string ZaIsplatu { get; set; }
    public string DoprinosBenefStaz { get; set; }

    public string Prebruto1Tekst { get; set; }
    public string Prebruto2Tekst { get; set; }
    public string Prebruto3Tekst { get; set; }
    public string Prepor1Tekst { get; set; }
    public string Prepor2Tekst { get; set; }
    public string Prepor3Tekst { get; set; }
    public string Osnovp1Tekst { get; set; }
    public string Osnovp2Tekst { get; set; }
    public string Osnovp3Tekst { get; set; }
    public string Osnovp4Tekst { get; set; }
    public string PioUmanjenjeRadnikTekst { get; set; }
    public string PioUmanjenjeFirmaTekst { get; set; }
    public string PioRazlikaRadnikTekst { get; set; }
    public string PioRazlikaFirmaTekst { get; set; }

    public ZbirnaRekapitulacijaReportView(
        IEnumerable<LdObracunStavka> stavke,
        LdParametar? parametar,
        Firma? firma,
        DateTime? datumIsplate)
    {
        var lista = stavke?.ToList() ?? [];
        var culture = CultureInfo.CurrentCulture;

        static decimal Sum(List<LdObracunStavka> src, Func<LdObracunStavka, decimal> selector) => src.Sum(selector);
        string N2(decimal v) => v.ToString("N2", culture);
        string N0(int v) => v.ToString(culture);

        decimal casuc = Sum(lista, s => s.Casuc);
        decimal casvr = Sum(lista, s => s.Casvr);
        decimal casdor = Sum(lista, s => s.Casdor);
        decimal cslput = Sum(lista, s => s.Cslput);
        decimal caspraz = Sum(lista, s => s.Caspraz);
        decimal casbol = Sum(lista, s => s.Casbol);
        decimal casbol2 = Sum(lista, s => s.Casbol2);
        decimal casplac = Sum(lista, s => s.Casplac);
        decimal casplac2 = Sum(lista, s => s.Casplac2);
        decimal casgod = Sum(lista, s => s.Casgod);
        decimal casprod = Sum(lista, s => s.Casprod);
        decimal casradnap = Sum(lista, s => s.Casradnap);
        decimal casned = Sum(lista, s => s.Casned);
        decimal casnoc = Sum(lista, s => s.Casnoc);
        decimal caspriprav = Sum(lista, s => s.Caspriprav);

        decimal dinuc = Sum(lista, s => s.Dinuc);
        decimal dinvr = Sum(lista, s => s.Dinvr);
        decimal dindor = Sum(lista, s => s.Dindor);
        decimal dinsl = Sum(lista, s => s.Dinsl);
        decimal dinpraz = Sum(lista, s => s.Dinpraz);
        decimal dinbol = Sum(lista, s => s.Dinbol);
        decimal dinbol2 = Sum(lista, s => s.Dinbol2);
        decimal dinplac = Sum(lista, s => s.Dinplac);
        decimal dinplac2 = Sum(lista, s => s.Dinplac2);
        decimal dingod = Sum(lista, s => s.Dingod);
        decimal dinprod = Sum(lista, s => s.Dinprod);
        decimal dinradnap = Sum(lista, s => s.Dinradnap);
        decimal dinned = Sum(lista, s => s.Dinned);
        decimal dinnoc = Sum(lista, s => s.Dinnoc);
        decimal dinpriprav = Sum(lista, s => s.Dinpriprav);
        decimal dinmin = Sum(lista, s => s.Dinmin);
        decimal din1 = Sum(lista, s => s.Din1);
        decimal din2 = Sum(lista, s => s.Din2);
        decimal din3 = Sum(lista, s => s.Din3);
        decimal dinuk = Sum(lista, s => s.Dinuk);

        decimal stim1 = Sum(lista, s => s.Stim1);
        decimal stim2 = Sum(lista, s => s.Stim2);
        decimal stim3 = Sum(lista, s => s.Stim3);

        decimal fiksna = Sum(lista, s => s.Fiksna);
        decimal dotacija = Sum(lista, s => s.Dotacija);
        decimal naknade = Sum(lista, s => s.Naknade);
        decimal topli = Sum(lista, s => s.Topli);
        decimal regres = Sum(lista, s => s.Regres);
        decimal terenski = Sum(lista, s => s.Terenski);
        decimal ldodaci = Sum(lista, s => s.Ldodaci);
        decimal porumanj = Sum(lista, s => s.Porumanj);

        decimal bruto = Sum(lista, s => s.Bruto);
        decimal porezUk = Sum(lista, s => s.Porez + s.Porezu);
        decimal dopsocr = Sum(lista, s => s.Dopsocr);
        decimal doppr = Sum(lista, s => s.Doppr);
        decimal dopzr = Sum(lista, s => s.Dopzr);
        decimal dopnr = Sum(lista, s => s.Dopnr);
        decimal dopsocf = Sum(lista, s => s.Dopsocf);
        decimal doppf = Sum(lista, s => s.Doppf);
        decimal dopzf = Sum(lista, s => s.Dopzf);
        decimal dopnf = Sum(lista, s => s.Dopnf);
        decimal neto = Sum(lista, s => s.Neto);
        decimal netoprev = Sum(lista, s => s.Netoprev);
        decimal netosve = Sum(lista, s => s.Netosve);
        decimal neto2 = Sum(lista, s => s.Neto2);
        decimal bendin = Sum(lista, s => s.Bendin);
        decimal dopumanj = Sum(lista, s => s.Dopumanj);
        decimal zaisplatu = Sum(lista, s => s.Zaisplatu);

        decimal krediti = Sum(lista, s => s.Krediti);
        decimal kreditia = Sum(lista, s => s.Kreditia);
        decimal akontac = Sum(lista, s => s.Akontac);
        decimal prevoz = Sum(lista, s => s.Prevoz);
        decimal aliment = Sum(lista, s => s.Aliment);
        decimal kasa = Sum(lista, s => s.Kasa);
        decimal kasarata = Sum(lista, s => s.Kasarata);
        decimal samodopr = Sum(lista, s => s.Samodopr);
        decimal sindikat1 = Sum(lista, s => s.Sindikat1);
        decimal sindikat2 = Sum(lista, s => s.Sindikat2);
        decimal solidarn = Sum(lista, s => s.Solidarn);
        decimal obust1 = Sum(lista, s => s.Obust1);
        decimal obust2 = Sum(lista, s => s.Obust2);
        decimal obust3 = Sum(lista, s => s.Obust3);
        decimal obust4 = Sum(lista, s => s.Obust4);
        decimal ukobust = Sum(lista, s => s.Ukobust);

        decimal prebruto1 = Sum(lista, s => s.Prebruto1);
        decimal prebruto2 = Sum(lista, s => s.Prebruto2);
        decimal prebruto3 = Sum(lista, s => s.Prebruto3);
        decimal prepor1 = Sum(lista, s => s.Prepor1);
        decimal prepor2 = Sum(lista, s => s.Prepor2);
        decimal prepor3 = Sum(lista, s => s.Prepor3);
        decimal osnovp1 = Sum(lista, s => s.Osnovp1);
        decimal osnovp2 = Sum(lista, s => s.Osnovp2);
        decimal osnovp3 = Sum(lista, s => s.Osnovp3);
        decimal osnovp4 = Sum(lista, s => s.Osnovp4);
        decimal pioumanjr = Sum(lista, s => s.Pioumanjr);
        decimal pioumanjf = Sum(lista, s => s.Pioumanjf);
        decimal doppru = Sum(lista, s => s.Doppru);
        decimal doppfu = Sum(lista, s => s.Doppfu);

        decimal ukupnoRedovnaCas = casuc + casvr + casdor + cslput;
        decimal ukupnoRedovnaIznos = dinuc + dinvr + dindor + dinsl;
        decimal ukupnoNaknadeCas = caspraz + casbol + casbol2 + casplac + casplac2 + casgod;
        decimal ukupnoNaknadeIznos = dinpraz + dinbol + dinbol2 + dinplac + dinplac2 + dingod;
        decimal ukupnoDodatnaCas = casprod + casradnap + casned + casnoc + caspriprav;
        decimal ukupnoDodatnaIznos = dinprod + dinradnap + dinned + dinnoc + dinpriprav + dinmin;
        decimal ukupnoOstaliDodaciIznos = din1 + din2 + din3;
        decimal ukupno1234Iznos = dinuk != 0m
            ? dinuk
            : ukupnoRedovnaIznos + ukupnoNaknadeIznos + ukupnoDodatnaIznos + ukupnoOstaliDodaciIznos;
        decimal ukupnaStimulacija = stim1 + stim2 + stim3;
        decimal ukupnaOstalaPrimanja = fiksna + dotacija + naknade + topli + regres + terenski + ldodaci;
        decimal svegaNeto = netosve != 0m ? netosve : neto + netoprev;
        decimal netoDva = neto2 != 0m ? neto2 : zaisplatu;
        decimal ukupnaObaveza = bruto + dopsocf;

        FirmaNaziv = Prazno(firma?.Naziv, "Firma");
        TekuciRacun = Prazno(firma?.ZiroRacun, "-");
        Telefon = Prazno(firma?.Telefon1, "-");
        Fax = Prazno(firma?.Fax1, "-");
        DatumIsplateTekst = datumIsplate?.ToString("dd.MM.yyyy", culture) ?? "-";
        RedniBrIsplateTekst = (parametar?.Redispl > 0 ? parametar.Redispl : 1).ToString(culture);
        MesecNaziv = Prazno(parametar?.Nazmes, "-").ToUpperInvariant();
        MesecBrojTekst = N0(parametar?.Mesec ?? 0);
        GodinaTekst = Prazno(parametar?.Godina, "-");
        VrstaIsplateTekst = Prazno(parametar?.Vrstaplate, "REDOVNA ZARADA");
        CenaRadaTekst = N2(parametar?.Cenarada ?? 0m);
        CasoviMesecTekst = N0(parametar?.Cmes ?? 0);
        CasoviPraznikTekst = N0(parametar?.Cpraz ?? 0);
        ZakonskiSatiTekst = N0(parametar?.Czakon ?? 0);

        RedovnaRows =
        [
            Red("PO VREMENU", "", casuc, dinuc, N2),
            Red("PO UCINKU", "", casvr, dinvr, N2),
            Red("DORUCAK", "", casdor, dindor, N2),
            Red("SLUZBENI PUT", "", cslput, dinsl, N2)
        ];

        NaknadeRows =
        [
            Red("PRAZNIK", "", caspraz, dinpraz, N2),
            Red("BOLOVANJE", Proc(parametar?.Procbol), casbol, dinbol, N2),
            Red("BOLOVANJE 100%", "", casbol2, dinbol2, N2),
            Red("PLACENO ODS", Proc(parametar?.Procplac), casplac, dinplac, N2),
            Red("PLACENO 100 %", "", casplac2, dinplac2, N2),
            Red("GODISNJI ODMOR", "", casgod, dingod, N2)
        ];

        DodatnaRows =
        [
            Red("PRODUZENI RAD", Proc(parametar?.Procprod), casprod, dinprod, N2),
            Red("RAD NA PRAZNIK", Proc(parametar?.Procpraz), casradnap, dinradnap, N2),
            Red("RAD NEDELJOM", Proc(parametar?.Procned), casned, dinned, N2),
            Red("NOCNI RAD", Proc(parametar?.Procnoc), casnoc, dinnoc, N2),
            Red("PRIPRAVNOST", Proc(parametar?.Priprav), caspriprav, dinpriprav, N2),
            Red("MINULI RAD", Proc(parametar?.Procmin), 0m, dinmin, N2)
        ];

        OstaliDodaciRows =
        [
            Red("ostalapr.1", "", 0m, din1, N2),
            Red("ostalapr.2", "", 0m, din2, N2),
            Red("ostalapr.3", "", 0m, din3, N2)
        ];

        StimulacijaRows =
        [
            Row("STIMULACIJA 1", stim1, N2),
            Row("STIMULACIJA 2", stim2, N2),
            Row("STIMULACIJA 3", stim3, N2)
        ];

        OstalaPrimanjaRows =
        [
            Row("FIKSNA PLATA", fiksna, N2),
            Row("DOTACIJA NA OBR.ZAR.", dotacija, N2),
            Row("Razl.dotac. na zar.", naknade, N2),
            Row("Topli obrok 2", topli, N2),
            Row("Regres", regres, N2),
            Row("Terenski dod.", terenski, N2),
            Row("Umanjenje", porumanj, N2)
        ];

        BrutoRows =
        [
            Row("BRUTO ZARADA", bruto, N2),
            Row("BRUTO UMANJENJA", dopumanj, N2),
            Row("POREZ", porezUk, N2),
            Row("DOPRINOSI", dopsocr, N2),
            Row("PIO", doppr, N2),
            Row("ZDRAVSTVENO", dopzr, N2),
            Row("ZAPOSLJAV.", dopnr, N2)
        ];

        DoprinosPoslodavcaRows =
        [
            Row("DOPRINOSI POSLODAVCA", dopsocf, N2),
            Row("PIO", doppf, N2),
            Row("ZDRAVSTVENO", dopzf, N2),
            Row("ZAPOSLAV.", dopnf, N2)
        ];

        NetoRows =
        [
            Row("NETO", neto, N2),
            Row("PREVOZ", netoprev, N2),
            Row("SVEGA NETO", svegaNeto, N2),
            Row("UKUPNA OBAVEZA", ukupnaObaveza, N2),
            Row("NETO 2", netoDva, N2)
        ];

        ObustaveRows =
        [
            Row("KREDITI", krediti, N2),
            Row("KREDITI AKONT.", kreditia, N2),
            Row("AKONTACIJA", akontac, N2),
            Row("PREVOZ", prevoz, N2),
            Row(Prazno(parametar?.Nazo1, "ALIMENT/IZVRSIT."), aliment, N2),
            Row(Prazno(parametar?.Nazo2, "KASA"), kasa, N2),
            Row(Prazno(parametar?.Nazo3, "KASA RATA"), kasarata, N2),
            Row(Prazno(parametar?.Nazo4, "SAMODOPRINOS"), samodopr, N2),
            Row(Prazno(parametar?.Nazo5, "SINDIKAT 1"), sindikat1, N2),
            Row(Prazno(parametar?.Nazo6, "SINDIKAT 2"), sindikat2, N2),
            Row("SOLIDARNOST", solidarn, N2),
            Row("ostal.ob.1", obust1, N2),
            Row("ostal.ob.2", obust2, N2),
            Row("ostal.ob.3", obust3, N2),
            Row("ostal.ob.4", obust4, N2)
        ];

        UkupnoRedovnaCas = N2(ukupnoRedovnaCas);
        UkupnoRedovnaIznos = N2(ukupnoRedovnaIznos);
        UkupnoNaknadeCas = N2(ukupnoNaknadeCas);
        UkupnoNaknadeIznos = N2(ukupnoNaknadeIznos);
        UkupnoDodatnaCas = N2(ukupnoDodatnaCas);
        UkupnoDodatnaIznos = N2(ukupnoDodatnaIznos);
        UkupnoOstaliDodaciIznos = N2(ukupnoOstaliDodaciIznos);
        Ukupno1234Cas = N2(ukupnoRedovnaCas + ukupnoNaknadeCas + ukupnoDodatnaCas);
        Ukupno1234Iznos = N2(ukupno1234Iznos);
        UkupnaStimulacija = N2(ukupnaStimulacija);
        UkupnaOstalaPrimanja = N2(ukupnaOstalaPrimanja);
        UkupneObustave = N2(ukobust);
        ZaIsplatu = N2(zaisplatu);
        DoprinosBenefStaz = N2(bendin);

        Prebruto1Tekst = N2(prebruto1);
        Prebruto2Tekst = N2(prebruto2);
        Prebruto3Tekst = N2(prebruto3);
        Prepor1Tekst = N2(prepor1);
        Prepor2Tekst = N2(prepor2);
        Prepor3Tekst = N2(prepor3);
        Osnovp1Tekst = N2(osnovp1);
        Osnovp2Tekst = N2(osnovp2);
        Osnovp3Tekst = N2(osnovp3);
        Osnovp4Tekst = N2(osnovp4);
        PioUmanjenjeRadnikTekst = N2(pioumanjr);
        PioUmanjenjeFirmaTekst = N2(pioumanjf);
        PioRazlikaRadnikTekst = N2(doppru);
        PioRazlikaFirmaTekst = N2(doppfu);

        InitializeComponent();
        DataContext = this;
    }

    private void Stampaj_Click(object sender, RoutedEventArgs e) => Stampaj();

    private void Zatvori_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F10)
        {
            Stampaj();
            e.Handled = true;
        }
    }

    private void Stampaj()
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
            return;

        dialog.PrintVisual(PrintRoot, "Zbirna rekapitulacija zarada");
    }

    private static string Prazno(params string?[] vrednosti)
    {
        foreach (var v in vrednosti)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return "-";
    }

    private static string Proc(decimal? v)
    {
        if (!v.HasValue || v.Value == 0m)
            return string.Empty;

        return v.Value.ToString("0.##", CultureInfo.CurrentCulture);
    }

    private static DetaljRow Red(string naziv, string proc, decimal casovi, decimal iznos, Func<decimal, string> n2)
        => new()
        {
            Naziv = naziv,
            Proc = proc,
            Casovi = n2(casovi),
            Iznos = n2(iznos)
        };

    private static IznosRow Row(string naziv, decimal iznos, Func<decimal, string> n2)
        => new()
        {
            Naziv = naziv,
            Iznos = n2(iznos)
        };

    public sealed class DetaljRow
    {
        public string Naziv { get; init; } = string.Empty;
        public string Proc { get; init; } = string.Empty;
        public string Casovi { get; init; } = "0.00";
        public string Iznos { get; init; } = "0.00";
    }

    public sealed class IznosRow
    {
        public string Naziv { get; init; } = string.Empty;
        public string Iznos { get; init; } = "0.00";
    }
}
