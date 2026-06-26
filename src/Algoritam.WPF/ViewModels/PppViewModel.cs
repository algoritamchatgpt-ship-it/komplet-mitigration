using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class PppViewModel : ObservableObject
{
    private const string DefaultLinkPut = @"C:\Program Files\Mozilla Firefox\firefox.exe http://eporezi.poreskauprava.gov.rs";
    private readonly string _folderPath;
    private string? _xm2PzarDbfPath;

    [ObservableProperty] private string _firmaNaziv = "FIRMA";
    [ObservableProperty] private string _firmaMesto = string.Empty;
    [ObservableProperty] private string _poruka = string.Empty;
    [ObservableProperty] private PppPzarParametar _parametar = new();

    public PppViewModel(string folderPath)
    {
        _folderPath = folderPath ?? string.Empty;
        Ucitaj();
    }

    [RelayCommand]
    private void Sacuvaj() => SacuvajNaDisk(true);

    [RelayCommand]
    private void Osvezi() => Ucitaj();

    public void SacuvajNaDisk(bool prijaviPoruku = true)
    {
        try
        {
            if (Parametar is null)
                return;

            _xm2PzarDbfPath ??= PronadjiDbf(_folderPath, "xm2pzar.dbf");
            if (string.IsNullOrWhiteSpace(_xm2PzarDbfPath))
                _xm2PzarDbfPath = PronadjiIliKreirajXm2PzarDbf(Parametar);

            if (string.IsNullOrWhiteSpace(_xm2PzarDbfPath) || !File.Exists(_xm2PzarDbfPath))
            {
                if (prijaviPoruku)
                    Poruka = "Nije pronađena putanja do xm2pzar.dbf.";
                return;
            }

            var schema = DbfTableWriter.LoadSchema(_xm2PzarDbfPath);
            var postojece = DbfReader.CitajSveZapise(_xm2PzarDbfPath);

            var rows = new List<Dictionary<string, object?>>();
            if (postojece.Count == 0)
            {
                rows.Add(BuildRow(Parametar, null));
            }
            else
            {
                rows.Add(BuildRow(Parametar, postojece[0]));
                for (var i = 1; i < postojece.Count; i++)
                    rows.Add(new Dictionary<string, object?>(postojece[i], StringComparer.OrdinalIgnoreCase));
            }

            DbfTableWriter.WriteTable(
                _xm2PzarDbfPath,
                schema,
                rows,
                static (r, fieldName) => r.TryGetValue(fieldName, out var value) ? value : null);

            if (prijaviPoruku)
                Poruka = "Parametri su sačuvani u xm2pzar.dbf.";
        }
        catch (Exception ex)
        {
            if (prijaviPoruku)
                Poruka = $"Greska pri cuvanju xm2pzar.dbf: {ex.Message}";
        }
    }

    private void Ucitaj()
    {
        var firma = UcitajFirmuInfo();
        FirmaNaziv = string.IsNullOrWhiteSpace(firma.Naziv) ? "FIRMA" : firma.Naziv.Trim();
        FirmaMesto = firma.Mesto.Trim();

        var podrazumevano = BuildPodrazumevaniParametar(firma);
        _xm2PzarDbfPath = PronadjiIliKreirajXm2PzarDbf(podrazumevano);

        if (string.IsNullOrWhiteSpace(_xm2PzarDbfPath) || !File.Exists(_xm2PzarDbfPath))
        {
            Parametar = podrazumevano;
            Poruka = "xm2pzar.dbf nije pronađen; ucitane su podrazumevane vrednosti.";
            return;
        }

        try
        {
            var prviRed = DbfReader.CitajSveZapise(_xm2PzarDbfPath).FirstOrDefault();
            if (prviRed is null)
            {
                Parametar = podrazumevano;
                SacuvajNaDisk(false);
                Poruka = "xm2pzar.dbf je bio prazan; kreiran je inicijalni zapis.";
                return;
            }

            Parametar = BuildFromRow(prviRed, podrazumevano);
            Poruka = "Parametri PPP-PD su ucitani iz xm2pzar.dbf.";
        }
        catch (Exception ex)
        {
            Parametar = podrazumevano;
            Poruka = $"Greska pri citanju xm2pzar.dbf: {ex.Message}";
        }
    }

    private PppPzarParametar BuildPodrazumevaniParametar(FirmaPppInfo firma)
    {
        var ld = UcitajLdParamInfo();
        var godina = string.IsNullOrWhiteSpace(ld.Godina)
            ? DateTime.Today.Year.ToString(CultureInfo.InvariantCulture)
            : ld.Godina.Trim();

        var mesec = ld.Mesec is >= 1 and <= 12 ? ld.Mesec : DateTime.Today.Month;
        var godinaInt = ParseGodina(godina);
        var dana = ld.Dana > 0 ? ld.Dana : DateTime.DaysInMonth(godinaInt, mesec);
        var brojZap = IzracunajBrojZaposlenih();
        var konacna = string.IsNullOrWhiteSpace(ld.Konacna) ? "K" : ld.Konacna.Trim();
        var period = $"{godinaInt:0000}-{mesec:00}";

        return new PppPzarParametar
        {
            Deklaracija = "1",
            VrstaPrijave = "1",
            Godina = godina,
            IsplataZaMesec = mesec.ToString("00", CultureInfo.InvariantCulture),
            Konacna = konacna,
            DatumObaveze = DateTime.Today,
            DatumPlacanja = DateTime.Today,
            VrstaIzmene = string.Empty,
            IdentifikacijaIzmene = string.Empty,
            BrojResenja = string.Empty,
            OsnovPodnosenja = string.Empty,
            TipIsplate = string.Empty,
            VrstaIdIsplatioca = "0",
            PropisanaOsnovica = string.Empty,
            PibIliJmbg = firma.Pib,
            BrojDana = dana.ToString(CultureInfo.InvariantCulture),
            BrojZaposlenih = brojZap > 0 ? brojZap.ToString(CultureInfo.InvariantCulture) : string.Empty,
            FondSati = ld.FondSati > 0 ? ld.FondSati.ToString(CultureInfo.InvariantCulture) : string.Empty,
            JmbgPodnosioca = firma.JmbgPodnosioca,
            MaticniBrojFirme = firma.Maticni,
            NazivPrezimeIme = firma.Naziv,
            Sediste = firma.Mesto,
            Telefon = firma.Telefon,
            UlicaIBroj = firma.UlicaIBroj,
            Email = firma.Email,
            Link = DefaultLinkPut,
            SifraOpstine = firma.SifraOpstine,
            Period = period,
            RedniBrojIsplate = ld.RedniBrojIsplate > 0
                ? ld.RedniBrojIsplate.ToString(CultureInfo.InvariantCulture)
                : "1"
        };
    }

    private FirmaPppInfo UcitajFirmuInfo()
    {
        var path = PronadjiDbf(_folderPath, "firma.dbf");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new FirmaPppInfo();

        try
        {
            var red = DbfReader.CitajSveZapise(path).FirstOrDefault();
            if (red is null)
                return new FirmaPppInfo();

            var naziv = Str(red, "FIME");
            var mesto = Str(red, "FMES");
            var ulica = $"{Str(red, "FUL")} {Str(red, "FULBR")}".Trim();

            return new FirmaPppInfo
            {
                Naziv = naziv,
                Mesto = mesto,
                Pib = Str(red, "FPOR"),
                Maticni = Str(red, "FMAT"),
                JmbgPodnosioca = PrvaNeprazna(Str(red, "FMBSAV"), Str(red, "FJMBG")),
                Telefon = PrvaNeprazna(Str(red, "FTEL"), Str(red, "FTEL2")),
                UlicaIBroj = ulica,
                Email = Str(red, "FEMAIL"),
                SifraOpstine = Str(red, "FSDK")
            };
        }
        catch
        {
            return new FirmaPppInfo();
        }
    }

    private LdParamInfo UcitajLdParamInfo()
    {
        var path = PronadjiDbf(_folderPath, "ldparam.dbf");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new LdParamInfo();

        try
        {
            var red = DbfReader.CitajSveZapise(path).FirstOrDefault();
            if (red is null)
                return new LdParamInfo();

            var fond = Int(red, "CZAKON");
            if (fond <= 0)
                fond = Int(red, "CMES");

            return new LdParamInfo
            {
                Mesec = Int(red, "MESEC"),
                Godina = Str(red, "GODINA"),
                Dana = Int(red, "DANA"),
                FondSati = fond,
                Konacna = Str(red, "KONACNA"),
                RedniBrojIsplate = Int(red, "REDISPL")
            };
        }
        catch
        {
            return new LdParamInfo();
        }
    }

    private int IzracunajBrojZaposlenih()
    {
        var path = PronadjiDbf(_folderPath, "ldrad.dbf");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return 0;

        try
        {
            var zapisi = DbfReader.CitajSveZapise(path);
            if (zapisi.Count == 0)
                return 0;

            var aktivni = zapisi.Count(z =>
            {
                var neaktivan = Str(z, "NEAKTIVAN");
                return !string.Equals(neaktivan, "*", StringComparison.OrdinalIgnoreCase) &&
                       !string.Equals(neaktivan, "D", StringComparison.OrdinalIgnoreCase);
            });

            return aktivni > 0 ? aktivni : zapisi.Count;
        }
        catch
        {
            return 0;
        }
    }

    private string? PronadjiIliKreirajXm2PzarDbf(PppPzarParametar podrazumevano)
    {
        var path = PronadjiDbf(_folderPath, "xm2pzar.dbf");
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            return path;

        var rootFolder = !string.IsNullOrWhiteSpace(_folderPath) && Directory.Exists(_folderPath)
            ? _folderPath
            : Environment.CurrentDirectory;
        var ciljnaPutanja = Path.Combine(rootFolder, "xm2pzar.dbf");

        try
        {
            var schema = UcitajSemuXm2Pzar(ciljnaPutanja);
            var red = BuildRow(podrazumevano, null);

            DbfTableWriter.WriteTable(
                ciljnaPutanja,
                schema,
                new[] { red },
                static (r, fieldName) => r.TryGetValue(fieldName, out var value) ? value : null);

            return ciljnaPutanja;
        }
        catch
        {
            return null;
        }
    }

    private static DbfTableWriter.DbfSchema UcitajSemuXm2Pzar(string ciljnaPutanja)
    {
        if (File.Exists(ciljnaPutanja))
            return DbfTableWriter.LoadSchema(ciljnaPutanja);

        foreach (var root in KandidatiZaRoot())
        {
            var kandidati = new[]
            {
                Path.Combine(root, "newproject", "templates", "F1", "xm2pzar.dbf"),
                Path.Combine(root, "newproject", "instalacije", "AlgoritamOffice", "templates", "F1", "xm2pzar.dbf"),
                Path.Combine(root, "old-project", "F1", "xm2pzar.dbf"),
                Path.Combine(root, "old-project", "databaze", "xm2pzar.dbf"),
                Path.Combine(root, "old-project", "databaze", "xm2pzar.DBF")
            };

            foreach (var kandidat in kandidati)
            {
                if (File.Exists(kandidat))
                    return DbfTableWriter.LoadSchema(kandidat);
            }
        }

        throw new FileNotFoundException("Sema za xm2pzar.dbf nije pronađena.");
    }

    private static IEnumerable<string> KandidatiZaRoot()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        DodajParents(roots, AppContext.BaseDirectory);
        DodajParents(roots, Environment.CurrentDirectory);
        return roots;

        static void DodajParents(HashSet<string> target, string? startPath)
        {
            if (string.IsNullOrWhiteSpace(startPath))
                return;

            var info = new DirectoryInfo(startPath);
            while (info != null)
            {
                target.Add(info.FullName);
                info = info.Parent;
            }
        }
    }

    private static PppPzarParametar BuildFromRow(Dictionary<string, object?> row, PppPzarParametar fallback)
    {
        var model = new PppPzarParametar
        {
            Deklaracija = PrvaNeprazna(Str(row, "DEKLARAC"), fallback.Deklaracija),
            VrstaPrijave = PrvaNeprazna(Str(row, "VRSTAPRIJ"), fallback.VrstaPrijave),
            Godina = PrvaNeprazna(Str(row, "GODINA"), fallback.Godina),
            IsplataZaMesec = PrvaNeprazna(Str(row, "ISPZAMES"), fallback.IsplataZaMesec),
            Konacna = PrvaNeprazna(Str(row, "KONACNA"), fallback.Konacna),
            DatumObaveze = Dat(row, "DATUMOBAV") ?? fallback.DatumObaveze,
            DatumPlacanja = Dat(row, "DATUMPLAC") ?? fallback.DatumPlacanja,
            VrstaIzmene = PrvaNeprazna(Str(row, "VRSTAIZM"), fallback.VrstaIzmene),
            IdentifikacijaIzmene = PrvaNeprazna(Str(row, "IDENTIFIK"), fallback.IdentifikacijaIzmene),
            BrojResenja = PrvaNeprazna(Str(row, "BROJRES"), fallback.BrojResenja),
            OsnovPodnosenja = PrvaNeprazna(Str(row, "OSNOV"), fallback.OsnovPodnosenja),
            TipIsplate = PrvaNeprazna(Str(row, "TIPISP"), fallback.TipIsplate),
            VrstaIdIsplatioca = PrvaNeprazna(Str(row, "VRSTAIDISP"), fallback.VrstaIdIsplatioca),
            PropisanaOsnovica = PrvaNeprazna(Str(row, "PROPISANAO"), fallback.PropisanaOsnovica),
            PibIliJmbg = PrvaNeprazna(Str(row, "PIB"), fallback.PibIliJmbg),
            BrojDana = PrvaNeprazna(Str(row, "DANA"), fallback.BrojDana),
            BrojZaposlenih = PrvaNeprazna(Str(row, "BROJZAPOS"), fallback.BrojZaposlenih),
            FondSati = PrvaNeprazna(Str(row, "FONDSATI"), fallback.FondSati),
            JmbgPodnosioca = PrvaNeprazna(Str(row, "JMBGPODNOS"), fallback.JmbgPodnosioca),
            MaticniBrojFirme = PrvaNeprazna(Str(row, "MATICNI"), fallback.MaticniBrojFirme),
            NazivPrezimeIme = PrvaNeprazna(Str(row, "NAZIV"), fallback.NazivPrezimeIme),
            Sediste = PrvaNeprazna(Str(row, "SEDISTE"), fallback.Sediste),
            Telefon = PrvaNeprazna(Str(row, "TELEFON"), fallback.Telefon),
            UlicaIBroj = PrvaNeprazna(Str(row, "ULICAIBR"), fallback.UlicaIBroj),
            Email = PrvaNeprazna(Str(row, "EMAIL"), fallback.Email),
            Link = PrvaNeprazna(Str(row, "LINKPUT"), fallback.Link),
            SifraOpstine = PrvaNeprazna(Str(row, "OPSTINA"), fallback.SifraOpstine),
            Period = PrvaNeprazna(Str(row, "PERIOD"), fallback.Period),
            RedniBrojIsplate = PrvaNeprazna(NumAsText(row, "REDISPL"), fallback.RedniBrojIsplate)
        };

        if (string.IsNullOrWhiteSpace(model.Period))
            model.Period = KreirajPeriod(model.Godina, model.IsplataZaMesec);

        return model;
    }

    private static Dictionary<string, object?> BuildRow(PppPzarParametar p, Dictionary<string, object?>? seed)
    {
        var row = seed is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(seed, StringComparer.OrdinalIgnoreCase);

        row["DEKLARAC"] = Safe(p.Deklaracija);
        row["VRSTAPRIJ"] = Safe(p.VrstaPrijave);
        row["GODINA"] = Safe(p.Godina);
        row["ISPZAMES"] = Safe(p.IsplataZaMesec);
        row["KONACNA"] = Safe(p.Konacna);
        row["DATUMOBAV"] = p.DatumObaveze;
        row["DATUMPLAC"] = p.DatumPlacanja;
        row["VRSTAIZM"] = Safe(p.VrstaIzmene);
        row["IDENTIFIK"] = Safe(p.IdentifikacijaIzmene);
        row["BROJRES"] = Safe(p.BrojResenja);
        row["OSNOV"] = Safe(p.OsnovPodnosenja);
        row["TIPISP"] = Safe(p.TipIsplate);
        row["VRSTAIDISP"] = Safe(p.VrstaIdIsplatioca);
        row["PROPISANAO"] = Safe(p.PropisanaOsnovica);
        row["PIB"] = Safe(p.PibIliJmbg);
        row["DANA"] = Safe(p.BrojDana);
        row["BROJZAPOS"] = Safe(p.BrojZaposlenih);
        row["FONDSATI"] = Safe(p.FondSati);
        row["JMBGPODNOS"] = Safe(p.JmbgPodnosioca);
        row["MATICNI"] = Safe(p.MaticniBrojFirme);
        row["NAZIV"] = Safe(p.NazivPrezimeIme);
        row["SEDISTE"] = Safe(p.Sediste);
        row["TELEFON"] = Safe(p.Telefon);
        row["ULICAIBR"] = Safe(p.UlicaIBroj);
        row["EMAIL"] = Safe(p.Email);
        row["LINKPUT"] = Safe(p.Link);
        row["OPSTINA"] = Safe(p.SifraOpstine);
        row["REDISPL"] = Dec(p.RedniBrojIsplate);
        row["PERIOD"] = KreirajPeriod(p.Godina, p.IsplataZaMesec);

        return row;
    }

    private static string? PronadjiDbf(string folderPath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return null;

        var kandidati = new List<string>();
        if (Directory.Exists(folderPath))
        {
            kandidati.Add(folderPath);
            kandidati.Add(Path.Combine(folderPath, "zarade"));
            kandidati.Add(Path.Combine(folderPath, "data00"));
            kandidati.Add(Path.Combine(folderPath, "01"));
            kandidati.Add(Path.Combine(folderPath, "data01"));
        }

        var parent = Directory.Exists(folderPath) ? Directory.GetParent(folderPath)?.FullName : null;
        if (!string.IsNullOrWhiteSpace(parent))
        {
            kandidati.Add(parent);
            kandidati.Add(Path.Combine(parent, "zarade"));
            kandidati.Add(Path.Combine(parent, "data00"));
            kandidati.Add(Path.Combine(parent, "01"));
            kandidati.Add(Path.Combine(parent, "data01"));
        }

        foreach (var kandidat in kandidati.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var found = PronadjiDbfCaseInsensitive(kandidat, fileName);
            if (!string.IsNullOrWhiteSpace(found))
                return found;
        }

        return null;
    }

    private static string? PronadjiDbfCaseInsensitive(string? folderPath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return null;

        var exact = Path.Combine(folderPath, fileName);
        if (File.Exists(exact))
            return exact;

        return Directory.GetFiles(folderPath, "*.dbf", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) ? (v?.ToString() ?? string.Empty).Trim() : string.Empty;

    private static int Int(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null)
            return 0;

        if (v is decimal d) return (int)d;
        if (v is int i) return i;
        return int.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    private static decimal Dec(string text)
        => decimal.TryParse((text ?? string.Empty).Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
            ? d
            : (decimal.TryParse((text ?? string.Empty).Trim(), out d) ? d : 0m);

    private static DateTime? Dat(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is DateTime dt ? dt : null;

    private static string NumAsText(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null)
            return string.Empty;

        return v switch
        {
            decimal d => d.ToString("0", CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            _ => (v.ToString() ?? string.Empty).Trim()
        };
    }

    private static string PrvaNeprazna(string prvi, string drugi)
        => !string.IsNullOrWhiteSpace(prvi) ? prvi : drugi;

    private static string Safe(string value)
        => (value ?? string.Empty).Trim();

    private static int ParseGodina(string? godinaText)
        => int.TryParse((godinaText ?? string.Empty).Trim(), out var g) && g >= 1900 && g <= 2200
            ? g
            : DateTime.Today.Year;

    private static string KreirajPeriod(string? godinaText, string? mesecText)
    {
        var godina = ParseGodina(godinaText);
        var mesec = int.TryParse((mesecText ?? string.Empty).Trim(), out var m) && m >= 1 && m <= 12 ? m : 1;
        return $"{godina:0000}-{mesec:00}";
    }

    private sealed class FirmaPppInfo
    {
        public string Naziv { get; init; } = string.Empty;
        public string Mesto { get; init; } = string.Empty;
        public string Pib { get; init; } = string.Empty;
        public string Maticni { get; init; } = string.Empty;
        public string JmbgPodnosioca { get; init; } = string.Empty;
        public string Telefon { get; init; } = string.Empty;
        public string UlicaIBroj { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string SifraOpstine { get; init; } = string.Empty;
    }

    private sealed class LdParamInfo
    {
        public int Mesec { get; init; }
        public string Godina { get; init; } = string.Empty;
        public int Dana { get; init; }
        public int FondSati { get; init; }
        public string Konacna { get; init; } = string.Empty;
        public int RedniBrojIsplate { get; init; }
    }
}

public partial class PppPzarParametar : ObservableObject
{
    [ObservableProperty] private string _deklaracija = string.Empty;
    [ObservableProperty] private string _vrstaPrijave = string.Empty;
    [ObservableProperty] private string _godina = string.Empty;
    [ObservableProperty] private string _isplataZaMesec = string.Empty;
    [ObservableProperty] private string _konacna = string.Empty;
    [ObservableProperty] private DateTime? _datumObaveze;
    [ObservableProperty] private DateTime? _datumPlacanja;
    [ObservableProperty] private string _vrstaIzmene = string.Empty;
    [ObservableProperty] private string _identifikacijaIzmene = string.Empty;
    [ObservableProperty] private string _brojResenja = string.Empty;
    [ObservableProperty] private string _osnovPodnosenja = string.Empty;
    [ObservableProperty] private string _tipIsplate = string.Empty;
    [ObservableProperty] private string _vrstaIdIsplatioca = string.Empty;
    [ObservableProperty] private string _propisanaOsnovica = string.Empty;
    [ObservableProperty] private string _pibIliJmbg = string.Empty;
    [ObservableProperty] private string _brojDana = string.Empty;
    [ObservableProperty] private string _brojZaposlenih = string.Empty;
    [ObservableProperty] private string _fondSati = string.Empty;
    [ObservableProperty] private string _jmbgPodnosioca = string.Empty;
    [ObservableProperty] private string _maticniBrojFirme = string.Empty;
    [ObservableProperty] private string _nazivPrezimeIme = string.Empty;
    [ObservableProperty] private string _sediste = string.Empty;
    [ObservableProperty] private string _telefon = string.Empty;
    [ObservableProperty] private string _ulicaIBroj = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _link = string.Empty;
    [ObservableProperty] private string _sifraOpstine = string.Empty;
    [ObservableProperty] private string _period = string.Empty;
    [ObservableProperty] private string _redniBrojIsplate = string.Empty;
}
