using Algoritam.Domain.Entities;
using Algoritam.WPF.Utilities;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Printing;

namespace Algoritam.WPF.Views.Zarade;

public partial class JedanListicReportView : Window
{
    public LdObracunStavka Stavka { get; set; }
    public LdParametar? Parametar { get; set; }

    public string FirmaNaziv { get; set; } = string.Empty;
    public string FirmaPib { get; set; } = string.Empty;
    public string FirmaTelefon { get; set; } = string.Empty;
    public string RadnikIme { get; set; } = string.Empty;
    public string RadnikAdresa { get; set; } = string.Empty;
    public string RadnikJmbg { get; set; } = string.Empty;
    public string DatumIsplateTekst { get; set; } = "-";
    public string RedniIsplateTekst { get; set; } = "-";
    public string KonacnaTekst { get; set; } = "-";
    public string MesecNaziv { get; set; } = "-";
    public string GodinaTekst { get; set; } = "-";
    public string VrstaIsplateTekst { get; set; } = "-";
    public string CasUkupnoTekst { get; set; } = "0.00";
    public string DinUkupnoTekst { get; set; } = "0.00";
    public string UkupneObustaveTekst { get; set; } = "0.00";
    public string ZaIsplatuTekst { get; set; } = "0.00";

    public IReadOnlyList<ListicRow> RedovnaRows { get; set; } = [];
    public IReadOnlyList<ListicRow> DodatnaRows { get; set; } = [];
    public IReadOnlyList<ListicRow> NaknadeRows { get; set; } = [];
    public IReadOnlyList<ListicRow> OstalaRows { get; set; } = [];
    public IReadOnlyList<ListicRow> PorezRows { get; set; } = [];
    public IReadOnlyList<ListicRow> DoprinosFirmaRows { get; set; } = [];
    public IReadOnlyList<ListicRow> UkupnaRows { get; set; } = [];
    public IReadOnlyList<ListicRow> ObustaveRows { get; set; } = [];
    public IReadOnlyList<ListicRow> OsnovicaRows { get; set; } = [];

    public JedanListicReportView(
        LdObracunStavka stavka,
        Radnik? radnik,
        LdParametar? parametar,
        Firma? firma,
        DateTime? datumIsplate)
    {
        Stavka = stavka;
        Parametar = parametar;

        FirmaNaziv = Prazno(firma?.Naziv, "Firma");
        FirmaPib = Prazno(firma?.Pib);
        FirmaTelefon = Prazno(firma?.Telefon1);
        RadnikIme = Prazno(radnik?.ImePrezime, stavka.ImePrez);
        RadnikAdresa = FormirajAdresu(radnik);
        RadnikJmbg = Prazno(radnik?.MaticniBroj, stavka.Maticnibr);
        DatumIsplateTekst = datumIsplate?.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture) ?? "-";
        RedniIsplateTekst = (stavka.Isplata > 0 ? stavka.Isplata : (parametar?.Redispl ?? 0)).ToString(CultureInfo.CurrentCulture);
        KonacnaTekst = Prazno(parametar?.Konacna);
        MesecNaziv = Prazno(stavka.Nazmes, parametar?.Nazmes, "-");
        GodinaTekst = Prazno(stavka.Godina, parametar?.Godina, "-");
        VrstaIsplateTekst = Prazno(parametar?.Vrstaplate);
        CasUkupnoTekst = N2(stavka.Casuk);
        DinUkupnoTekst = N2(IzracunajDinUkupnoZaPrikaz(stavka));
        UkupneObustaveTekst = N2(stavka.Ukobust);
        ZaIsplatuTekst = N2(stavka.Zaisplatu);

        RedovnaRows = FormirajRedovna(stavka);
        DodatnaRows = FormirajDodatna(stavka, parametar);
        NaknadeRows = FormirajNaknade(stavka, parametar);
        OstalaRows = FormirajOstala(stavka);
        PorezRows = FormirajPorez(stavka, parametar);
        DoprinosFirmaRows = FormirajDoprinosFirma(stavka, parametar);
        UkupnaRows = FormirajUkupna(stavka);
        ObustaveRows = FormirajObustave(stavka);
        OsnovicaRows = FormirajOsnovice(stavka);

        InitializeComponent();
        DataContext = this;
    }

    private static IReadOnlyList<ListicRow> FormirajRedovna(LdObracunStavka s) =>
    [
        Red("PO VREMENU", "", s.Casuc, s.Dinuc),
        Red("PO UCINKU", "", s.Casvr, s.Dinvr),
        Red("DORUCAK", "", s.Casdor, s.Dindor),
        Red("SLUZBENI PUT", "", s.Cslput, s.Dinsl)
    ];

    private static IReadOnlyList<ListicRow> FormirajDodatna(LdObracunStavka s, LdParametar? p) =>
    [
        Red("PRODUZENI RAD", Proc(p?.Procprod), s.Casprod, s.Dinprod),
        Red("RAD NA PRAZNIK", Proc(p?.Procpraz), s.Casradnap, s.Dinradnap),
        Red("RAD NEDELJOM", Proc(p?.Procned), s.Casned, s.Dinned),
        Red("NOCNI RAD", Proc(p?.Procnoc), s.Casnoc, s.Dinnoc),
        Red("PRIPRAVNOST", Proc(p?.Priprav), s.Caspriprav, s.Dinpriprav),
        Red("PAUSAL", "", s.Cas1, s.Din1),
        Red("MINULI RAD", Proc(p?.Procmin), 0m, s.Dinmin),
        Red("TOPLI OBROK", "", s.Casvv, s.Dinvv)
    ];

    private static IReadOnlyList<ListicRow> FormirajNaknade(LdObracunStavka s, LdParametar? p) =>
    [
        Red("PRAZNIK", "", s.Caspraz, s.Dinpraz),
        Red("BOLOVANJE", Proc(p?.Procbol), s.Casbol, s.Dinbol),
        Red("BOLOVANJE 100%", "", s.Casbol2, s.Dinbol2),
        Red("PLACENO ODSUSTVO", Proc(p?.Procplac), s.Casplac, s.Dinplac),
        Red("PLACENO 100%", "", s.Casplac2, s.Dinplac2),
        Red("GODISNJI ODMOR", "", s.Casgod, s.Dingod),
        Red("NEPLACENO ODSUSTVO", "", s.Casneplac, 0m)
    ];

    private static IReadOnlyList<ListicRow> FormirajOstala(LdObracunStavka s) =>
    [
        Red("Fiksni dodatak", s.Fiksna),
        Red("Dotacija na osn.zar.", s.Dotacija),
        Red("Razl.odl.ac. na oda.", s.Naknade),
        Red("Topli obrok 2", s.Topli),
        Red("Regres za g.o.", s.Regres),
        Red("Terenski d.", s.Terenski),
        Red("Umanjenje", s.Porumanj)
    ];

    private static IReadOnlyList<ListicRow> FormirajPorez(LdObracunStavka s, LdParametar? p) =>
    [
        Red("Bruto zarada", s.Bruto),
        Red("Osnovica poreza", s.Osnovica),
        Red($"Porez {ProcBezZnaka(p?.Procpor)} %", s.Porez),
        Red("Doprinosi radnika", s.Dopsocr),
        Red($"Pio {ProcBezZnaka(p?.Doppr1)} %", s.Doppr),
        Red($"Zdravstvo {ProcBezZnaka(p?.Dopzr1)} %", s.Dopzr),
        Red($"Nezaposl. {ProcBezZnaka(p?.Dopnr1)} %", s.Dopnr),
        Red("Neto", s.Neto),
        Red("Prevoz", s.Prevoz),
        Red("Svega neto", s.Netosve != 0m ? s.Netosve : s.Neto + s.Netoprev),
        Red("Neto ostalo", s.Netoost),
        Red("Solidarni porez", s.Solpor),
        Red("Neto 2", s.Neto2),
        Red("Bruto zarada", s.Bruto)
    ];

    private static IReadOnlyList<ListicRow> FormirajDoprinosFirma(LdObracunStavka s, LdParametar? p) =>
    [
        Red($"Pio {ProcBezZnaka(p?.Doppf1)} %", s.Doppf),
        Red($"Zdravstvo {ProcBezZnaka(p?.Dopzf1)} %", s.Dopzf),
        Red($"Nezaposl. {ProcBezZnaka(p?.Dopnf1)} %", s.Dopnf),
        Red("Ukupno", s.Dopsocf)
    ];

    private static IReadOnlyList<ListicRow> FormirajUkupna(LdObracunStavka s) =>
    [
        Red("Ukupna obaveza", s.Bruto + s.Dopsocf),
        Red("umanjenje doprinosa za pio", s.Dopumanj),
        Red("umanj. pio radnik", s.Pioumanjr),
        Red("razlika za uplatu PIOR", s.Doppru),
        Red("razlika za uplatu PIOF", s.Doppfu)
    ];

    private static IReadOnlyList<ListicRow> FormirajObustave(LdObracunStavka s) =>
    [
        Red("KREDITI", s.Krediti),
        Red("KREDITI AKONT.", s.Kreditia),
        Red("AKONTACIJA", s.Akontac),
        Red("PREVOZ", s.Prevoz),
        Red("ALIMENTACIJA", s.Aliment),
        Red("KASA", s.Kasa),
        Red("KASA RATA", s.Kasarata),
        Red("SAMODOPRINOS", s.Samodopr),
        Red("SINDIKAT 1", s.Sindikat1),
        Red("SINDIKAT 2", s.Sindikat2),
        Red("SOLID.KOM.LED.", s.Solidarn),
        Red("ostal.ob.1", s.Obust1),
        Red("ostal.ob.2", s.Obust2),
        Red("ostal.ob.3", s.Obust3),
        Red("ostal.ob.4", s.Obust4)
    ];

    private static IReadOnlyList<ListicRow> FormirajOsnovice(LdObracunStavka s) =>
    [
        Red("OSNOVICA DOPR.", s.Osnovica),
        Red("PROPISANA OSN.", s.Propisana),
        Red("doprinos za beneficirani staz", s.Bendin),
        Red("stopa", s.Benproc),
        Red("iznos", s.Bendin)
    ];

    private static ListicRow Red(string naziv, string proc, decimal casovi, decimal iznos) =>
        new()
        {
            Naziv = naziv,
            Proc = proc,
            Casovi = N2(casovi),
            Iznos = N2(iznos)
        };

    private static ListicRow Red(string naziv, decimal iznos) =>
        new()
        {
            Naziv = naziv,
            Iznos = N2(iznos)
        };

    private static string Prazno(params string?[] vrednosti)
    {
        foreach (var v in vrednosti)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return "-";
    }

    private static string FormirajAdresu(Radnik? radnik)
    {
        if (radnik == null)
            return "-";

        var adresa = Prazno(radnik.Adresa, string.Empty);
        var mesto = Prazno(radnik.Posta, string.Empty);
        var grad = Prazno(radnik.Mesto, string.Empty);

        var parts = new List<string>();
        if (adresa != "-") parts.Add(adresa);
        if (mesto != "-") parts.Add(mesto);
        if (grad != "-") parts.Add(grad);

        return parts.Count == 0 ? "-" : string.Join(" ", parts);
    }

    private static string Proc(decimal? vrednost) => vrednost.HasValue ? $"{vrednost.Value:0.##}%" : string.Empty;
    private static string ProcBezZnaka(decimal? vrednost) => vrednost.HasValue ? $"{vrednost.Value:0.##}" : "0";

    private static string N2(decimal vrednost) => vrednost.ToString("N2", CultureInfo.CurrentCulture);

    private void Stampaj_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        dialog.PrintTicket ??= new PrintTicket();
        dialog.PrintTicket.PageMediaSize ??= new PageMediaSize(PageMediaSizeName.ISOA4);
        if (dialog.ShowDialog() != true)
            return;

        PrintScaledToPage(dialog, PrintRoot, $"Jedan listic - {RadnikIme}");
    }

    private void Pdf_Click(object sender, RoutedEventArgs e)
    {
        var pocetniFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Algoritam",
            "PdfTest");

        Directory.CreateDirectory(pocetniFolder);

        var dialog = new SaveFileDialog
        {
            Title = "Sacuvaj test PDF",
            Filter = "PDF fajl (*.pdf)|*.pdf",
            FileName = $"listic_{Stavka.Broj}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
            InitialDirectory = pocetniFolder,
            AddExtension = true,
            DefaultExt = ".pdf",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            var lines = FormirajPdfLinije();
            SimplePdfWriter.WriteTextPdf(dialog.FileName, "Isplatni listic", lines);

            MessageBox.Show(
                this,
                $"PDF je uspesno kreiran:\n{dialog.FileName}",
                "PDF test",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Neuspesno kreiranje PDF fajla.\n{ex.Message}",
                "PDF test",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Zatvori_Click(object sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// Generiše linije tekst-PDF za isplatni listić bez otvaranja prozora.
    /// Može se koristiti van UI thread-a (ne poziva InitializeComponent).
    /// </summary>
    public static IReadOnlyList<string> GenerirajPdfLinijeStat(
        LdObracunStavka stavka, Radnik? radnik, LdParametar? parametar,
        Firma? firma, DateTime? datumIsplate)
    {
        var firmaNaziv  = Prazno(firma?.Naziv, "Firma");
        var firmaPib    = Prazno(firma?.Pib);
        var firmaTelefon = Prazno(firma?.Telefon1);
        var radnikIme   = Prazno(radnik?.ImePrezime, stavka.ImePrez);
        var radnikAdresa = FormirajAdresu(radnik);
        var radnikJmbg  = Prazno(radnik?.MaticniBroj, stavka.Maticnibr);
        var datumTekst  = datumIsplate?.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.CurrentCulture) ?? "-";
        var redniTekst  = (stavka.Isplata > 0 ? stavka.Isplata : (parametar?.Redispl ?? 0)).ToString();
        var konacna     = Prazno(parametar?.Konacna);
        var mesecNaziv  = Prazno(stavka.Nazmes, parametar?.Nazmes, "-");
        var godinaTekst = Prazno(stavka.Godina, parametar?.Godina, "-");
        var vrstaIspl   = Prazno(parametar?.Vrstaplate);
        var casUkupno   = N2(stavka.Casuk);
        var dinUkupno   = N2(IzracunajDinUkupnoZaPrikaz(stavka));
        var ukObustave  = N2(stavka.Ukobust);
        var zaIsplatu   = N2(stavka.Zaisplatu);

        var redovna  = FormirajRedovna(stavka);
        var dodatna  = FormirajDodatna(stavka, parametar);
        var naknade  = FormirajNaknade(stavka, parametar);
        var ostala   = FormirajOstala(stavka);
        var porez    = FormirajPorez(stavka, parametar);
        var dopFirma = FormirajDoprinosFirma(stavka, parametar);
        var ukupna   = FormirajUkupna(stavka);
        var obustave = FormirajObustave(stavka);
        var osnovice = FormirajOsnovice(stavka);

        int w = Algoritam.WPF.Utilities.SimplePdfWriter.LineWidth;
        string Sep()  => new string('-', w);
        string Sep2() => new string('=', w);
        string Naslov(string t) { var pad = (w - t.Length - 4) / 2; return new string(' ', Math.Max(0, pad)) + "[ " + t + " ]"; }
        string Polje(string label, string val)
        {
            int dots = w - label.Length - val.Length - 2;
            return label + " " + (dots > 0 ? new string('.', dots) : "") + " " + val;
        }

        var lines = new List<string>();
        lines.Add(Sep2());
        lines.Add(Naslov("ISPLATNI LISTIC"));
        lines.Add(Sep2());
        lines.Add(Polje("Firma:", firmaNaziv));
        if (firmaPib != "-")    lines.Add(Polje("PIB:", firmaPib));
        if (firmaTelefon != "-") lines.Add(Polje("Tel:", firmaTelefon));
        lines.Add(Sep());
        lines.Add(Polje("Radnik:", radnikIme));
        if (radnikAdresa != "-") lines.Add(Polje("Adresa:", radnikAdresa));
        lines.Add(Polje("JMBG:", radnikJmbg));
        lines.Add(Sep());
        lines.Add(Polje("Mesec/Godina:", $"{mesecNaziv} {godinaTekst}"));
        lines.Add(Polje("Datum isplate:", datumTekst));
        lines.Add(Polje("Redni br. isplate:", redniTekst));
        if (vrstaIspl != "-") lines.Add(Polje("Vrsta isplate:", vrstaIspl));
        lines.Add(Polje("Konacna:", konacna));
        lines.Add(Sep());
        lines.Add(Polje("Casovi ukupno:", casUkupno));
        lines.Add(Polje("Dinari ukupno:", dinUkupno));
        lines.Add(Polje("Ukupne obustave:", ukObustave));
        lines.Add(Sep2());
        lines.Add(Polje(">>> ZA ISPLATU:", zaIsplatu));
        lines.Add(Sep2());

        void DodajSekcijuF(string title, IReadOnlyList<ListicRow> stavke)
        {
            var vidljive = stavke.Where(r => !string.IsNullOrWhiteSpace(r.Iznos) && r.Iznos != "0,00" && r.Iznos != "0.00").ToList();
            if (vidljive.Count == 0) return;
            lines.Add(string.Empty);
            lines.Add(Naslov(title));
            lines.Add(Sep());
            foreach (var r in vidljive)
            {
                var label = string.IsNullOrWhiteSpace(r.Proc) ? r.Naziv : $"{r.Naziv} ({r.Proc})";
                var val   = r.Iznos;
                if (!string.IsNullOrWhiteSpace(r.Casovi) && r.Casovi != "0,00" && r.Casovi != "0.00")
                    val = $"{r.Casovi}h  {r.Iznos}";
                lines.Add(Polje("  " + label + ":", val));
            }
        }

        DodajSekcijuF("REDOVNA PRIMANJA",   redovna);
        DodajSekcijuF("DODATNA ZARADA",      dodatna);
        DodajSekcijuF("NAKNADE",             naknade);
        DodajSekcijuF("OSTALA PRIMANJA",     ostala);
        DodajSekcijuF("POREZ I DOPRINOSI",   porez);
        DodajSekcijuF("DOPRINOSI FIRME",     dopFirma);
        DodajSekcijuF("UKUPNA OBAVEZA",      ukupna);
        DodajSekcijuF("OBUSTAVE",            obustave);
        DodajSekcijuF("OSNOVICE",            osnovice);

        lines.Add(string.Empty);
        lines.Add(Sep2());
        lines.Add($"Kreirano: {DateTime.Now:dd.MM.yyyy HH:mm}");
        lines.Add(Sep2());

        return lines;
    }

    private IReadOnlyList<string> FormirajPdfLinije()
    {
        var lines = new List<string>
        {
            "ISPLATNI LISTIC - TEST PDF (FOX LDLISTIC)",
            $"Datum kreiranja: {DateTime.Now:dd.MM.yyyy HH:mm}",
            string.Empty,
            $"Firma: {FirmaNaziv}",
            $"PIB: {FirmaPib}",
            $"Telefon: {FirmaTelefon}",
            $"Radnik: {RadnikIme}",
            $"Adresa: {RadnikAdresa}",
            $"JMBG: {RadnikJmbg}",
            $"Datum isplate: {DatumIsplateTekst}",
            $"Redni broj isplate: {RedniIsplateTekst}",
            $"Konacna: {KonacnaTekst}",
            $"Mesec/Godina: {MesecNaziv} {GodinaTekst}",
            $"Vrsta isplate: {VrstaIsplateTekst}",
            string.Empty,
            $"Casovi ukupno: {CasUkupnoTekst}",
            $"Dinari ukupno: {DinUkupnoTekst}",
            $"Ukupne obustave: {UkupneObustaveTekst}",
            $"Za isplatu: {ZaIsplatuTekst}"
        };

        DodajSekciju(lines, "REDOVNA PRIMANJA", RedovnaRows);
        DodajSekciju(lines, "DODATNA ZARADA", DodatnaRows);
        DodajSekciju(lines, "NAKNADE", NaknadeRows);
        DodajSekciju(lines, "OSTALA PRIMANJA", OstalaRows);
        DodajSekciju(lines, "POREZ I DOPRINOSI", PorezRows);
        DodajSekciju(lines, "DOPRINOSI FIRME", DoprinosFirmaRows);
        DodajSekciju(lines, "UKUPNA OBAVEZA", UkupnaRows);
        DodajSekciju(lines, "OBUSTAVE", ObustaveRows);
        DodajSekciju(lines, "OSNOVICE", OsnovicaRows);

        return lines;
    }

    private static decimal IzracunajDinUkupnoZaPrikaz(LdObracunStavka stavka)
        => stavka.Dinuk != 0m
            ? stavka.Dinuk
            : stavka.Dinuc + stavka.Dinvr + stavka.Dinnoc + stavka.Dinprod
            + stavka.Dinned + stavka.Dinpraz + stavka.Dinbol + stavka.Dinbol2
            + stavka.Dinplac + stavka.Dinplac2 + stavka.Dingod + stavka.Dinsl
            + stavka.Dindor + stavka.Dinmin + stavka.Dinvv + stavka.Dinpriprav
            + stavka.Din1 + stavka.Din2 + stavka.Din3 + stavka.Dinsus;

    private static void DodajSekciju(List<string> lines, string naslov, IReadOnlyList<ListicRow> rows)
    {
        lines.Add(string.Empty);
        lines.Add($"[{naslov}]");

        foreach (var row in rows)
        {
            var detalji = new List<string>();

            if (!string.IsNullOrWhiteSpace(row.Proc))
                detalji.Add($"proc={row.Proc}");
            if (!string.IsNullOrWhiteSpace(row.Casovi))
                detalji.Add($"cas={row.Casovi}");
            if (!string.IsNullOrWhiteSpace(row.Iznos))
                detalji.Add($"iznos={row.Iznos}");

            if (detalji.Count == 0)
            {
                lines.Add($"- {row.Naziv}");
                continue;
            }

            lines.Add($"- {row.Naziv}: {string.Join(", ", detalji)}");
        }
    }

    public sealed class ListicRow
    {
        public string Naziv { get; init; } = string.Empty;
        public string Proc { get; init; } = string.Empty;
        public string Casovi { get; init; } = string.Empty;
        public string Iznos { get; init; } = string.Empty;
    }

    private static void PrintScaledToPage(PrintDialog dialog, FrameworkElement source, string jobName)
    {
        source.UpdateLayout();

        var contentWidth = source.ActualWidth;
        var contentHeight = source.ActualHeight;
        if (contentWidth <= 0 || contentHeight <= 0)
        {
            source.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            source.Arrange(new Rect(source.DesiredSize));
            contentWidth = source.DesiredSize.Width;
            contentHeight = source.DesiredSize.Height;
        }

        var printableWidth = dialog.PrintableAreaWidth;
        var printableHeight = dialog.PrintableAreaHeight;
        if (printableWidth <= 0 || printableHeight <= 0 || contentWidth <= 0 || contentHeight <= 0)
        {
            dialog.PrintVisual(source, jobName);
            return;
        }

        const double pageMargin = 24d;
        var availableWidth = Math.Max(1d, printableWidth - (pageMargin * 2d));
        var availableHeight = Math.Max(1d, printableHeight - (pageMargin * 2d));
        var scale = Math.Min(availableWidth / contentWidth, availableHeight / contentHeight);
        if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0)
            scale = 1d;

        var scaledWidth = contentWidth * scale;
        var scaledHeight = contentHeight * scale;
        var offsetX = (printableWidth - scaledWidth) / 2d;
        var offsetY = (printableHeight - scaledHeight) / 2d;

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, printableWidth, printableHeight));
            dc.PushTransform(new TranslateTransform(offsetX, offsetY));
            dc.PushTransform(new ScaleTransform(scale, scale));
            dc.DrawRectangle(new VisualBrush(source), null, new Rect(0, 0, contentWidth, contentHeight));
            dc.Pop();
            dc.Pop();
        }

        dialog.PrintVisual(visual, jobName);
    }
}
