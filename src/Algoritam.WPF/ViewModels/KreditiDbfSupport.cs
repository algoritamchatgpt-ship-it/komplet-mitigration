using Algoritam.Infrastructure.Dbf;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Algoritam.WPF.ViewModels;

internal sealed class KreditRadnikInfo
{
    public int Broj { get; init; }
    public string ImePrez { get; init; } = string.Empty;
    public string EvidBroj { get; init; } = string.Empty;
    public string MaticniBr { get; init; } = string.Empty;
    public int Grupa { get; init; }
}

internal sealed class KreditPartnerInfo
{
    public string Sifra { get; init; } = string.Empty;
    public string Naziv { get; init; } = string.Empty;
    public string Mesto { get; init; } = string.Empty;
    public string ZiroRac { get; init; } = string.Empty;
}

internal readonly record struct KreditRasknjizavanjeRezultat(bool Uspeh, string Poruka, int BrojStavki);
internal readonly record struct ParametarObracunaInfo(int Mesec, int Isplata, string Godina, DateTime? DatumIsplate);

internal static class KreditiDbfSupport
{
    public static List<KreditStavka> UcitajKredite(string folderPath)
    {
        var dbfPath = PronadjiDbf(folderPath, "ldkred.dbf");
        if (dbfPath == null)
            return [];

        var zapisi = DbfReader.CitajSveZapise(dbfPath);
        return zapisi.Select((z, index) => new KreditStavka
        {
            Kredit = Int(z, "KREDIT"),
            Broj = Int(z, "BROJ"),
            Sifra = Str(z, "SIFRA"),
            Partija = Str(z, "PARTIJA"),
            Iznos = Dec(z, "IZNOS"),
            Koliko = Int(z, "KOLIKO"),
            PrvaRata = Dec(z, "PRVARATA"),
            OstaleRate = Dec(z, "OSTALERATE"),
            ZaObaviti = NormalizeMarker(Str(z, "ZADOBITAK")),
            AktivnaRata = Dec(z, "AKTIVRATA"),
            AkontRata = Dec(z, "AKONTRATA"),
            Ostatak = Dec(z, "OSTATAK"),
            Odbijeno = Dec(z, "ODBIJENO"),
            EvidBroj = Str(z, "EVIDBROJ"),
            Modelo = Str(z, "MODELO"),
            DatDok = Dat(z, "DATDOK"),
            Grupa = Int(z, "GRUPA"),
            Arhiva = NormalizeMarker(Str(z, "ARHIVA"), allowSpace: true),
            Arhiva2 = NormalizeMarker(Str(z, "ARHIVA2"), allowSpace: true),
            Preneto = NormalizeMarker(Str(z, "PRENETO"), allowSpace: true),
            IdBr = Long(z, "IDBR"),
            Numred = index + 1
        }).ToList();
    }

    public static List<KreditOtplataStavka> UcitajSveOtplate(string folderPath)
    {
        var dbfPath = PronadjiDbf(folderPath, "ldkredr.dbf");
        if (dbfPath == null)
            return [];

        var zapisi = DbfReader.CitajSveZapise(dbfPath);
        return zapisi.Select((z, index) => new KreditOtplataStavka
        {
            Kredit = Int(z, "KREDIT"),
            Broj = Int(z, "BROJ"),
            Sifra = Str(z, "SIFRA"),
            DatDok = Dat(z, "DATDOK"),
            Dug = Dec(z, "DUG"),
            Iznos = Dec(z, "IZNOS"),
            Saldo = Dec(z, "SALDO"),
            BrNal = Str(z, "BRNAL"),
            Dev = Str(z, "DEV"),
            DevKurs = Dec(z, "DEVKURS"),
            DevDug = Dec(z, "DEVDUG"),
            DevPot = Dec(z, "DEVPOT"),
            DevSaldo = Dec(z, "DEVSALDO"),
            Mesec = Int(z, "MESEC"),
            Arhiva = NormalizeMarker(Str(z, "ARHIVA"), allowSpace: true),
            Arhiva2 = NormalizeMarker(Str(z, "ARHIVA2"), allowSpace: true),
            Preneto = NormalizeMarker(Str(z, "PRENETO"), allowSpace: true),
            IdBr = Long(z, "IDBR"),
            Numred = index + 1
        }).ToList();
    }

    public static IReadOnlyDictionary<int, KreditRadnikInfo> UcitajRadnike(string folderPath)
    {
        var dbfPath = PronadjiDbf(folderPath, "ldrad.dbf");
        if (dbfPath == null)
            return new Dictionary<int, KreditRadnikInfo>();

        var zapisi = DbfReader.CitajSveZapise(dbfPath);
        return zapisi
            .Select(z => new KreditRadnikInfo
            {
                Broj = Int(z, "BROJ"),
                ImePrez = Str(z, "IME_PREZ"),
                EvidBroj = Str(z, "EVIDBROJ"),
                MaticniBr = Str(z, "MATICNIBR"),
                Grupa = Int(z, "GRUPA")
            })
            .Where(x => x.Broj > 0)
            .GroupBy(x => x.Broj)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public static IReadOnlyDictionary<string, KreditPartnerInfo> UcitajPartnere(string folderPath)
    {
        var dbfPath = PronadjiDbf(folderPath, "an0.dbf");
        if (dbfPath == null)
            return new Dictionary<string, KreditPartnerInfo>(StringComparer.OrdinalIgnoreCase);

        var zapisi = DbfReader.CitajSveZapise(dbfPath);
        return zapisi
            .Select(z => new KreditPartnerInfo
            {
                Sifra = Str(z, "SIFRA"),
                Naziv = Str(z, "NAZIV"),
                Mesto = Str(z, "MESTO"),
                ZiroRac = Str(z, "ZIRORAC")
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Sifra))
            .GroupBy(x => x.Sifra, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    public static void SacuvajKredite(string folderPath, IReadOnlyCollection<KreditStavka> stavke)
    {
        var targetPath = PronadjiDbf(folderPath, "ldkred.dbf") ?? Path.Combine(folderPath, "ldkred.dbf");
        var schema = UcitajSemu(targetPath, "ldkred.dbf");

        var sortirano = stavke
            .OrderBy(x => x.Numred)
            .ThenBy(x => x.Kredit)
            .ThenBy(x => x.Broj)
            .ToList();

        DbfTableWriter.WriteTable(targetPath, schema, sortirano, ResolveKreditValue);
    }

    public static void SacuvajOtplate(string folderPath, IReadOnlyCollection<KreditOtplataStavka> stavke)
    {
        var targetPath = PronadjiDbf(folderPath, "ldkredr.dbf") ?? Path.Combine(folderPath, "ldkredr.dbf");
        var schema = UcitajSemu(targetPath, "ldkredr.dbf");

        var sortirano = stavke
            .OrderBy(x => x.Kredit)
            .ThenBy(x => x.DatDok)
            .ThenBy(x => x.Broj)
            .ThenBy(x => x.Numred)
            .ToList();

        ReizracunajSaldo(sortirano);
        DbfTableWriter.WriteTable(targetPath, schema, sortirano, ResolveOtplataValue);
    }

    public static void ReizracunajSaldo(IList<KreditOtplataStavka> stavke)
    {
        foreach (var grupa in stavke.GroupBy(x => x.Kredit).Where(g => g.Key > 0))
        {
            decimal saldo = 0m;
            decimal devSaldo = 0m;

            foreach (var stavka in grupa
                .OrderBy(x => x.DatDok)
                .ThenBy(x => x.Broj)
                .ThenBy(x => x.Numred))
            {
                saldo += stavka.Dug - stavka.Iznos;
                devSaldo += stavka.DevDug - stavka.DevPot;
                stavka.Saldo = saldo;
                stavka.DevSaldo = devSaldo;
            }
        }
    }

    public static KreditRasknjizavanjeRezultat IzvrsiRasknjizavanje(
        string folderPath,
        bool zaAkontaciju,
        DateTime datumDokumenta,
        IReadOnlyDictionary<int, KreditRadnikInfo> radnici)
    {
        var datum = datumDokumenta.Date;
        var krediti = UcitajKredite(folderPath);
        if (krediti.Count == 0)
            return new KreditRasknjizavanjeRezultat(false, "Nema kredita za rasknjizavanje.", 0);

        var otplate = UcitajSveOtplate(folderPath);

        // 1) Uskladi trenutno stanje kredita (zaduzenja/otplate) pre novog knjizenja.
        SrediKredite(krediti, otplate, radnici);

        var param = UcitajParametarObracuna(folderPath);
        var mesec = param?.Mesec is > 0 and <= 12 ? param.Value.Mesec : datum.Month;

        // 2) Izvor istine je platni spisak za aktivni period:
        //    KREDITI za konacnu isplatu, KREDITIA za akontaciju.
        var obustavePoRadniku = UcitajObustaveIzPlatnogSpiska(folderPath, param, zaAkontaciju);

        var planPoKreditu = RaspodeliObustavePoKreditima(krediti, obustavePoRadniku, zaAkontaciju);

        var dodato = 0;
        var vecEvidentirano = 0;

        foreach (var kredit in krediti.OrderBy(k => k.Broj).ThenBy(k => k.Numred).ThenBy(k => k.Kredit))
        {
            if (!planPoKreditu.TryGetValue(kredit.Kredit, out var planirano) || planirano <= 0m)
                continue;

            // Idempotentno za isti datum: dodaje samo razliku koja jos nije knjizena.
            var vecKnjizenoZaDatum = otplate
                .Where(x => x.Kredit == kredit.Kredit && x.DatDok.Date == datum)
                .Sum(x => x.Iznos);

            var zaKnjizenje = planirano - vecKnjizenoZaDatum;
            if (zaKnjizenje <= 0m)
            {
                vecEvidentirano++;
                continue;
            }

            otplate.Add(new KreditOtplataStavka
            {
                Kredit = kredit.Kredit,
                Broj = kredit.Broj,
                Sifra = kredit.Sifra,
                DatDok = datum,
                Dug = 0m,
                Iznos = zaKnjizenje,
                BrNal = string.Empty,
                Dev = string.Empty,
                DevKurs = 0m,
                DevDug = 0m,
                DevPot = 0m,
                Mesec = mesec,
                Arhiva = " ",
                Arhiva2 = " ",
                Preneto = " ",
                IdBr = 0L,
                Numred = otplate.Count + 1
            });

            dodato++;
        }

        if (dodato == 0 && vecEvidentirano == 0)
        {
            var poruka = zaAkontaciju
                ? "Nema odbijenih rata za rasknjizavanje akontacije u aktivnom platnom spisku."
                : "Nema odbijenih rata za rasknjizavanje plate u aktivnom platnom spisku.";
            return new KreditRasknjizavanjeRezultat(true, poruka, 0);
        }

        SrediKredite(krediti, otplate, radnici);
        SacuvajOtplate(folderPath, otplate);
        SacuvajKredite(folderPath, krediti);

        var gotovo = zaAkontaciju
            ? $"Zavrseno je rasknjizavanje kredita na akontaciju. Evidentirano: {dodato}. Vec postojalo na datum: {vecEvidentirano}."
            : $"Zavrseno je rasknjizavanje kredita. Evidentirano: {dodato}. Vec postojalo na datum: {vecEvidentirano}.";
        return new KreditRasknjizavanjeRezultat(true, gotovo, dodato);
    }

    public static int UcitajMesecObracuna(string folderPath, int fallbackMesec)
    {
        var param = UcitajParametarObracuna(folderPath);
        return param?.Mesec is > 0 and <= 12 ? param.Value.Mesec : fallbackMesec;
    }

    public static DateTime UcitajPredlozeniDatumRasknjizavanja(string folderPath)
    {
        var param = UcitajParametarObracuna(folderPath);
        if (param?.DatumIsplate is DateTime datum)
            return datum.Date;

        return DateTime.Today;
    }

    public static void SrediKredite(
        IList<KreditStavka> krediti,
        IList<KreditOtplataStavka> otplate,
        IReadOnlyDictionary<int, KreditRadnikInfo> radnici)
    {
        var otplatePoKreditu = otplate
            .GroupBy(x => x.Kredit)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.DatDok).ThenBy(x => x.Numred).ToList());

        foreach (var kredit in krediti)
        {
            otplatePoKreditu.TryGetValue(kredit.Kredit, out var grupa);
            grupa ??= [];

            if (!grupa.Any(x => x.Dug > 0m))
            {
                var zaduzivanje = new KreditOtplataStavka
                {
                    Kredit = kredit.Kredit,
                    Broj = kredit.Broj,
                    Sifra = kredit.Sifra,
                    DatDok = kredit.DatDok.Date,
                    Dug = kredit.Iznos,
                    Iznos = 0m,
                    BrNal = string.Empty,
                    Dev = string.Empty,
                    DevKurs = 0m,
                    DevDug = 0m,
                    DevPot = 0m,
                    Mesec = kredit.DatDok.Month,
                    Arhiva = " ",
                    Arhiva2 = " ",
                    Preneto = " ",
                    IdBr = 0L,
                    Numred = otplate.Count + 1
                };

                otplate.Add(zaduzivanje);
                grupa.Add(zaduzivanje);
            }

            var ukupnoDug = grupa.Sum(x => x.Dug);
            var ukupnoUplaceno = grupa.Sum(x => x.Iznos);
            var ostatak = ukupnoDug - ukupnoUplaceno;

            kredit.Odbijeno = ukupnoUplaceno;
            kredit.Ostatak = ostatak;

            if (radnici.TryGetValue(kredit.Broj, out var radnik))
            {
                kredit.EvidBroj = radnik.EvidBroj;
                if (kredit.Grupa == 0)
                    kredit.Grupa = radnik.Grupa;
            }

            if (kredit.Ostatak <= 0m)
            {
                kredit.ZaObaviti = " ";
                kredit.AktivnaRata = 0m;
                kredit.AkontRata = 0m;
                kredit.Arhiva = "*";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(kredit.Arhiva) || kredit.Arhiva == "*")
                    kredit.Arhiva = " ";

                if (string.IsNullOrWhiteSpace(kredit.ZaObaviti))
                    kredit.ZaObaviti = "*";

                if (kredit.Ostatak <= kredit.AktivnaRata && kredit.Ostatak >= 0m)
                {
                    kredit.AktivnaRata = kredit.Ostatak;
                }

                if (kredit.Ostatak <= kredit.AkontRata && kredit.Ostatak >= 0m)
                {
                    kredit.AkontRata = kredit.Ostatak;
                }
            }
        }

        ReizracunajSaldo(otplate);
    }

    private static Dictionary<int, decimal> UcitajObustaveIzPlatnogSpiska(
        string folderPath,
        ParametarObracunaInfo? param,
        bool zaAkontaciju)
    {
        var putanjaLd = NadjiLdFajlZaPeriod(folderPath, param);
        if (string.IsNullOrWhiteSpace(putanjaLd) || !File.Exists(putanjaLd))
            return [];

        var ciljPolje = zaAkontaciju ? "KREDITIA" : "KREDITI";
        var trazeniMesec = param?.Mesec ?? 0;
        var trazenaGodina = (param?.Godina ?? string.Empty).Trim();
        var trazenaIsplata = param?.Isplata ?? 0;

        try
        {
            var zapisi = DbfReader.CitajSveZapise(putanjaLd);
            return zapisi
                .Where(z =>
                {
                    var broj = Int(z, "BROJ");
                    if (broj <= 0) return false;

                    var iznos = Dec(z, ciljPolje);
                    if (iznos <= 0m) return false;

                    if (trazeniMesec > 0 && Int(z, "MESEC") != trazeniMesec)
                        return false;

                    var godinaZapisa = Str(z, "GODINA");
                    if (!GodinaSePoklapa(godinaZapisa, trazenaGodina))
                        return false;

                    if (trazenaIsplata > 0)
                    {
                        var isplataZapisa = Int(z, "ISPLATA");
                        if (isplataZapisa > 0 && isplataZapisa != trazenaIsplata)
                            return false;
                    }

                    return true;
                })
                .GroupBy(z => Int(z, "BROJ"))
                .ToDictionary(g => g.Key, g => g.Sum(z => Dec(z, ciljPolje)));
        }
        catch
        {
            return [];
        }
    }

    private static Dictionary<int, decimal> RaspodeliObustavePoKreditima(
        IList<KreditStavka> krediti,
        Dictionary<int, decimal> obustavePoRadniku,
        bool zaAkontaciju)
    {
        var raspodela = new Dictionary<int, decimal>();
        var preostaloPoRadniku = new Dictionary<int, decimal>(obustavePoRadniku);

        foreach (var kredit in krediti.OrderBy(k => k.Broj).ThenBy(k => k.Numred).ThenBy(k => k.Kredit))
        {
            if (!JeAktivanKredit(kredit) || kredit.Broj <= 0)
                continue;

            if (!preostaloPoRadniku.TryGetValue(kredit.Broj, out var preostaloZaRadnika) || preostaloZaRadnika <= 0m)
                continue;

            var rata = zaAkontaciju ? kredit.AkontRata : kredit.AktivnaRata;
            if (rata <= 0m)
                continue;

            var limitPoDugu = kredit.Ostatak > 0m ? Math.Min(rata, kredit.Ostatak) : rata;
            var iznos = Math.Min(preostaloZaRadnika, limitPoDugu);
            if (iznos <= 0m)
                continue;

            raspodela[kredit.Kredit] = iznos;
            preostaloPoRadniku[kredit.Broj] = preostaloZaRadnika - iznos;
        }

        return raspodela;
    }

    private static bool JeAktivanKredit(KreditStavka kredit)
    {
        var aktivan = !string.IsNullOrWhiteSpace(kredit.ZaObaviti) && kredit.ZaObaviti.Trim() == "*";
        var nijeArhiviran = string.IsNullOrWhiteSpace(kredit.Arhiva) || kredit.Arhiva.Trim() != "*";
        return aktivan && nijeArhiviran;
    }

    private static string? NadjiLdFajlZaPeriod(string folderPath, ParametarObracunaInfo? param)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return null;

        var prefix = (param?.Isplata ?? 1) switch
        {
            2 => "LDP",
            3 => "LDB",
            _ => "LD"
        };

        var trazeniMesec = param?.Mesec ?? 0;
        var trazenaGodina = (param?.Godina ?? string.Empty).Trim();
        var trazenaIsplata = param?.Isplata ?? 0;
        string? fallbackMesec = null;

        var poseceni = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var kandidatiNaziva = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            $"{prefix}.dbf",
            $"{prefix}0.dbf",
            $"{prefix}00.dbf"
        };

        for (int i = 1; i <= 99; i++)
        {
            kandidatiNaziva.Add($"{prefix}{i}.dbf");
            kandidatiNaziva.Add($"{prefix}{i:00}.dbf");
        }

        foreach (var naziv in kandidatiNaziva)
        {
            var putanja = PronadjiDbf(folderPath, naziv);
            if (string.IsNullOrWhiteSpace(putanja) || !poseceni.Add(putanja))
                continue;

            if (trazeniMesec <= 0)
                return putanja;

            try
            {
                var zapisi = DbfReader.CitajSveZapise(putanja);
                var poMesecu = zapisi.Where(z => Int(z, "MESEC") == trazeniMesec).ToList();
                if (poMesecu.Count == 0)
                    continue;

                fallbackMesec ??= putanja;

                var periodPogodjen = poMesecu.Any(z =>
                    (trazenaIsplata <= 0 || Int(z, "ISPLATA") is 0 || Int(z, "ISPLATA") == trazenaIsplata) &&
                    GodinaSePoklapa(Str(z, "GODINA"), trazenaGodina));

                if (periodPogodjen)
                    return putanja;
            }
            catch
            {
                // ignore and continue with next candidate
            }
        }

        if (!string.IsNullOrWhiteSpace(fallbackMesec))
            return fallbackMesec;

        return PronadjiDbf(folderPath, $"{prefix}.dbf");
    }

    private static bool GodinaSePoklapa(string godinaZapisa, string trazenaGodina)
    {
        if (string.IsNullOrWhiteSpace(trazenaGodina))
            return true;

        if (string.IsNullOrWhiteSpace(godinaZapisa))
            return true;

        var zapis = godinaZapisa.Trim();
        var trazena = trazenaGodina.Trim();
        if (zapis.Equals(trazena, StringComparison.OrdinalIgnoreCase))
            return true;

        if (int.TryParse(zapis, out var godinaZapisInt) && int.TryParse(trazena, out var godinaTrazenaInt))
        {
            if (godinaZapisInt == godinaTrazenaInt)
                return true;

            return (godinaZapisInt % 100) == (godinaTrazenaInt % 100);
        }

        return false;
    }

    public static string? PronadjiDbf(string folderPath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return null;

        var exact = Path.Combine(folderPath, fileName);
        if (File.Exists(exact))
            return exact;

        var upper = Path.Combine(folderPath, fileName.ToUpperInvariant());
        if (File.Exists(upper))
            return upper;

        if (!Directory.Exists(folderPath))
            return null;

        return Directory.GetFiles(folderPath, "*.dbf", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }

    private static DbfTableWriter.DbfSchema UcitajSemu(string targetPath, string fileName)
    {
        if (File.Exists(targetPath))
            return DbfTableWriter.LoadSchema(targetPath);

        foreach (var root in KandidatiZaRoot())
        {
            var convertTemplate = Path.Combine(root, "newproject", "src", "Algoritam.WPF", "convert to sql", fileName);
            if (File.Exists(convertTemplate))
                return DbfTableWriter.LoadSchema(convertTemplate);

            var oldTemplate = Path.Combine(root, "old-project", "F1", fileName);
            if (File.Exists(oldTemplate))
                return DbfTableWriter.LoadSchema(oldTemplate);
        }

        throw new FileNotFoundException($"Sema za {fileName} nije pronađena.");
    }

    private static IEnumerable<string> KandidatiZaRoot()
    {
        var kandidati = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddParents(string? startPath)
        {
            if (string.IsNullOrWhiteSpace(startPath))
                return;

            var dir = new DirectoryInfo(startPath);
            while (dir != null)
            {
                kandidati.Add(dir.FullName);
                dir = dir.Parent;
            }
        }

        AddParents(AppContext.BaseDirectory);
        AddParents(Environment.CurrentDirectory);

        return kandidati;
    }

    private static ParametarObracunaInfo? UcitajParametarObracuna(string folderPath)
    {
        var dbfPath = PronadjiDbf(folderPath, "ldparam.dbf");
        if (dbfPath == null)
            return null;

        var zapis = DbfReader.CitajSveZapise(dbfPath).FirstOrDefault();
        if (zapis == null)
            return null;

        var mesec = Int(zapis, "MESEC");
        var isplata = Int(zapis, "ISPLATA");
        var godina = Str(zapis, "GODINA");
        var redispl = Int(zapis, "REDISPL");
        var datum = redispl switch
        {
            1 => DatNullable(zapis, "DAT1"),
            2 => DatNullable(zapis, "DAT2"),
            3 => DatNullable(zapis, "DAT3"),
            _ => DatNullable(zapis, "DAT4")
        };

        return new ParametarObracunaInfo(mesec, isplata, godina, datum);
    }

    private static object? ResolveKreditValue(KreditStavka row, string fieldName) => fieldName.ToUpperInvariant() switch
    {
        "KREDIT" => row.Kredit,
        "BROJ" => row.Broj,
        "SIFRA" => Safe(row.Sifra, 5),
        "PARTIJA" => Safe(row.Partija, 20),
        "IZNOS" => row.Iznos,
        "KOLIKO" => row.Koliko,
        "PRVARATA" => row.PrvaRata,
        "OSTALERATE" => row.OstaleRate,
        "ZADOBITAK" => SafeMarker(row.ZaObaviti),
        "AKTIVRATA" => row.AktivnaRata,
        "AKONTRATA" => row.AkontRata,
        "OSTATAK" => row.Ostatak,
        "ODBIJENO" => row.Odbijeno,
        "EVIDBROJ" => Safe(row.EvidBroj, 8),
        "MODELO" => Safe(row.Modelo, 2),
        "DATDOK" => row.DatDok,
        "GRUPA" => row.Grupa,
        "ARHIVA" => SafeMarker(row.Arhiva, allowSpace: true),
        "ARHIVA2" => SafeMarker(row.Arhiva2, allowSpace: true),
        "PRENETO" => SafeMarker(row.Preneto, allowSpace: true),
        "IDBR" => row.IdBr,
        _ => null
    };

    private static object? ResolveOtplataValue(KreditOtplataStavka row, string fieldName) => fieldName.ToUpperInvariant() switch
    {
        "KREDIT" => row.Kredit,
        "BROJ" => row.Broj,
        "SIFRA" => Safe(row.Sifra, 5),
        "DATDOK" => row.DatDok,
        "DUG" => row.Dug,
        "IZNOS" => row.Iznos,
        "SALDO" => row.Saldo,
        "BRNAL" => Safe(row.BrNal, 6),
        "DEV" => Safe(row.Dev, 3),
        "DEVKURS" => row.DevKurs,
        "DEVDUG" => row.DevDug,
        "DEVPOT" => row.DevPot,
        "DEVSALDO" => row.DevSaldo,
        "MESEC" => row.Mesec,
        "ARHIVA" => SafeMarker(row.Arhiva, allowSpace: true),
        "ARHIVA2" => SafeMarker(row.Arhiva2, allowSpace: true),
        "PRENETO" => SafeMarker(row.Preneto, allowSpace: true),
        "IDBR" => row.IdBr,
        _ => null
    };

    private static string NormalizeMarker(string value, bool allowSpace = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return allowSpace ? " " : string.Empty;

        var trimmed = value.Trim();
        if (trimmed == "*")
            return "*";

        return allowSpace ? " " : trimmed;
    }

    private static string Safe(string value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string SafeMarker(string value, bool allowSpace = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return allowSpace ? " " : string.Empty;

        return value.Trim() == "*" ? "*" : (allowSpace ? " " : value.Trim());
    }

    private static string Str(Dictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) && value is string text ? text.Trim() : string.Empty;

    private static int Int(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value == null)
            return 0;

        if (value is int intValue)
            return intValue;

        if (value is decimal decimalValue)
            return (int)decimalValue;

        return int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) ? parsed : 0;
    }

    private static long Long(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value == null)
            return 0L;

        if (value is long longValue)
            return longValue;

        if (value is decimal decimalValue)
            return (long)decimalValue;

        return long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) ? parsed : 0L;
    }

    private static decimal Dec(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value == null)
            return 0m;

        if (value is decimal decimalValue)
            return decimalValue;

        return decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0m;
    }

    private static DateTime Dat(Dictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) && value is DateTime date ? date : DateTime.Today;

    private static DateTime? DatNullable(Dictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) && value is DateTime date ? date : null;
}
