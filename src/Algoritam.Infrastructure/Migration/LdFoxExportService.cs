using Algoritam.Application;
using Algoritam.Domain.Entities;
using Algoritam.Infrastructure.Data;
using Algoritam.Infrastructure.Dbf;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Algoritam.Infrastructure.Migration;

/// <summary>
/// Izvoz podataka zarada iz SQLite (LdObracunStavke) u FOX LD* DBF tabele.
/// </summary>
public class LdFoxExportService
{
    private static readonly Dictionary<string, PropertyInfo> LdObracunPropMap = BuildPropMap<LdObracunStavka>();
    private static readonly Dictionary<string, PropertyInfo> LdParametarPropMap = BuildPropMap<LdParametar>();
    private static readonly Dictionary<string, PropertyInfo> RadnikPropMap = BuildPropMap<Radnik>();

    private static readonly Dictionary<string, string> RadnikFieldAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["IME_PREZ"] = "ImePrezime",
        ["MATICNIBR"] = "MaticniBroj",
        ["EVIDBROJ"] = "EvidencijskiBroj",
        ["OPSTINAR"] = "OpstinaRada",
        ["PREBIVAL"] = "Prebivaliste",
        ["RADNOMES"] = "RadnoMesto",
        ["RMESTO"] = "RadnoMestoDetalj",
        ["PJ"] = "PoslovnaJedinica",
        ["SIFRAORG"] = "SifraOrganizacije",
        ["IZVORFIN"] = "IzvorFinansiranja",
        ["GRUPAVIRM"] = "GrupaVirmana",
        ["IDPROFC"] = "IdProfesionalniKod",
        ["IDPODSEK"] = "IdPodsektor",
        ["IDLOKAC"] = "IdLokacija",
        ["IDLOKACP"] = "IdLokacijaPod",
        ["DOK"] = "Dokument",
        ["MP"] = "MestoPrimanja",
        ["SKOSPREMA"] = "SkolskaSprema",
        ["SIFRAZANIM"] = "SifraZanimanja",
        ["VRSTAZAP"] = "VrstaZaposlenja",
        ["VRSTAPRIM"] = "VrstaPrimanja",
        ["OZNVRPRIH"] = "OznakaVrstePrihoda",
        ["OZNOLAKS"] = "OznakaOlaksica",
        ["OZNBEN"] = "OznakaBeneficije",
        ["TIPSLUZB"] = "TipSluzbe",
        ["PLATNAGR"] = "PlatnaGrupa",
        ["GODNAPRED"] = "GodinaNapredovanja",
        ["GRNAMEST"] = "GrupaNamestenja",
        ["PROCANGAZ"] = "ProcenatAngazovanja",
        ["DATPRI"] = "DatumPrijave",
        ["DATUGOVOR"] = "DatumUgovora",
        ["DATZASNIV"] = "DatumZasnivanja",
        ["DATZAPOS"] = "DatumZaposlenja",
        ["DATOTKAZ"] = "DatumOtkaza",
        ["UGOVDAT0"] = "UgovorOd",
        ["UGOVDAT1"] = "UgovorDo",
        ["PRODDAT0"] = "ProduzenjeOd",
        ["PRODDAT1"] = "ProduzenjeDo",
        ["DATNEZAP"] = "DatumNezaposlenosti",
        ["DATMIN"] = "DatumMinulogRada",
        ["KOEF"] = "Koeficijent",
        ["KOEFDOD"] = "KoeficijentDodatni",
        ["KOEFUKUP"] = "KoeficijentUkupni",
        ["PROCUVEC"] = "ProcenatUvecanja",
        ["STARTBOD"] = "StartniBodovi",
        ["BENPROC"] = "BeneficiraniProcenat",
        ["BENSTAZ"] = "BeneficiraniStaz",
        ["STAZJUBIL"] = "StazJubilej",
        ["MINPROC"] = "ProcenatMinulogRada",
        ["SIND1PROC"] = "SindikatProcenat1",
        ["SIND2PROC"] = "SindikatProcenat2",
        ["SOLPROC"] = "SolidarnostProcenat",
        ["ALIMPROC"] = "AlimentacijaProcenat",
        ["KOLKOR"] = "KolektivniKorak",
        ["DINSAT1"] = "DinarskaSatnica1",
        ["DINSAT2"] = "DinarskaSatnica2",
        ["DINSAT3"] = "DinarskaSatnica3",
        ["DINSATSVE"] = "DinarskaSatnicaUkupno",
        ["CASSAT1"] = "CasovnaSatnica1",
        ["CASSAT2"] = "CasovnaSatnica2",
        ["CASSAT3"] = "CasovnaSatnica3",
        ["CASSATSVE"] = "CasovnaSatnicaUkupno",
        ["STIMIN"] = "StimulacijaMin",
        ["STIMGOD"] = "StimulacijaGodisnja",
        ["STIM1"] = "Stimulacija1",
        ["STIM2"] = "Stimulacija2",
        ["STIM3"] = "Stimulacija3",
        ["DESTIM1"] = "Destimulacija1",
        ["DESTIM2"] = "Destimulacija2",
        ["DESTIM3"] = "Destimulacija3",
        ["FONDZ"] = "FondZarada",
        ["M4MES"] = "M4Mesec",
        ["DATOSIG0"] = "DatumOsiguranjaOd",
        ["DATOSIG1"] = "DatumOsiguranjaDo",
        ["REGSOC"] = "RegBrojSocijalno",
        ["OSNOVOSIG"] = "OsnovOsiguranja",
        ["GODUK"] = "GodisnjeDanaUkupno",
        ["GODISKOR"] = "GodisnjeDanaIskorisceno",
        ["GODNEISKOR"] = "GodisnjeDanaNeiskorisceno",
        ["SAMSIF"] = "SamodoprSifra",
        ["SAMOPROC"] = "SamodoprProcenat",
        ["PROCUMANJ"] = "ProcenatUmanjenja",
        ["PORUMANJ"] = "PorskoUmanjenje",
        ["DOPUMANJ"] = "DoprinosnoUmanjenje",
        ["PIOUMANJR"] = "PioUmanjenjeRadnik",
        ["PIOUMANJF"] = "PioUmanjenjeFirma",
        ["MFP3PROC"] = "Mfp3Procenat",
        ["MFP8NEPUN"] = "Mfp8Nepuno",
        ["MFP9NAJOSN"] = "Mfp9NajnizaOsnova",
        ["MFP10DVEZ"] = "Mfp10DvaVezana",
        ["TOPLI"] = "ToploObrok",
        ["PRIPRAV"] = "Pripravnik",
        ["POROLAKS"] = "PoreskeOlaksice",
        ["OBUCBRNR"] = "ObukaBrojNaredbe",
        ["OBUCPP"] = "ObukaPp",
        ["SANITARNI"] = "SanitarniPregled",
        ["PRIPUG"] = "PripravnickiUgovor",
        ["PRIPDAT"] = "PripravnickiDatum",
        ["NAPOMENA"] = "Napomena1",
    };

    public async Task<LdFoxExportResult> IzveziAsync(string firmaFolderPath, string dbPath)
    {
        if (string.IsNullOrWhiteSpace(firmaFolderPath) || !Directory.Exists(firmaFolderPath))
            return LdFoxExportResult.Fail("Folder firme nije pronadjen.");

        if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
            return LdFoxExportResult.Fail("SQLite baza nije pronadjena.");

        using var ctx = new FirmaDbContext(dbPath);
        await LdObracunSchemaBootstrapper.EnsureAsync(ctx);

        var stavke = await ctx.LdObracunStavke.AsNoTracking().ToListAsync();
        LdParametar? ldParametar = null;
        if (await PostojiTabelaAsync(ctx, "LdParametri"))
            ldParametar = await ctx.LdParametri.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync();

        var radnici = new List<Radnik>();
        if (await PostojiTabelaAsync(ctx, "Radnici"))
            radnici = await ctx.Radnici.AsNoTracking().OrderBy(x => x.Broj).ToListAsync();

        if (stavke.Count == 0 && ldParametar is null && radnici.Count == 0)
            return LdFoxExportResult.Fail("Nema podataka za izvoz.");

        var backupPath = NapraviBackupPostojecihLdTabela(firmaFolderPath);

        var groups = GroupByTarget(stavke);
        if (stavke.Count > 0)
            await DodajAliasTabeleTrenutnogMeseca(ctx, stavke, groups);

        var templateF1 = FoxWorkspaceSupport.FindTemplateF1(Directory.GetParent(firmaFolderPath)?.FullName);
        var schemaCache = new Dictionary<string, DbfTableWriter.DbfSchema>(StringComparer.OrdinalIgnoreCase);
        var upisaniFajlovi = new List<string>();

        Dictionary<string, string> templateByTarget;
        try
        {
            templateByTarget = ResolveTemplatePaths(groups, ldParametar, radnici, firmaFolderPath, templateF1);
        }
        catch (Exception ex)
        {
            return LdFoxExportResult.Fail(ex.Message);
        }

        foreach (var kvp in groups.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            var fileName = kvp.Key;
            var rows = kvp.Value;
            if (rows.Count == 0)
                continue;

            var targetPath = Path.Combine(firmaFolderPath, fileName);
            var templatePath = templateByTarget[fileName];

            var schema = GetOrLoadSchema(schemaCache, templatePath);

            DbfTableWriter.WriteTable(targetPath, schema, rows, ResolveValue);
            ObrisiStariIndex(targetPath);
            upisaniFajlovi.Add(targetPath);
        }

        if (ldParametar is not null)
        {
            const string fileName = "LDPARAM.DBF";
            var targetPath = Path.Combine(firmaFolderPath, fileName);
            var templatePath = templateByTarget[fileName];
            var schema = GetOrLoadSchema(schemaCache, templatePath);
            var baseRow = TryUcitajPostojeciRed(targetPath);
            var row = BuildEntityRow(ldParametar, schema, LdParametarPropMap, aliases: null, seed: baseRow);

            DbfTableWriter.WriteTable(
                targetPath,
                schema,
                [row],
                static (r, fieldName) => r.TryGetValue(fieldName, out var value) ? value : null);
            ObrisiStariIndex(targetPath);
            upisaniFajlovi.Add(targetPath);
        }

        if (radnici.Count > 0)
        {
            const string fileName = "LDRAD.DBF";
            var targetPath = Path.Combine(firmaFolderPath, fileName);
            var templatePath = templateByTarget[fileName];
            var schema = GetOrLoadSchema(schemaCache, templatePath);
            var existingByBroj = UcitajPostojeceRadnikePoBroju(targetPath);
            var radniciZaIzvoz = NormalizujRadnikeZaIzvoz(radnici);
            var rows = new List<Dictionary<string, object?>>(radniciZaIzvoz.Count);
            var usedBrojevi = new HashSet<int>();
            var nextAutoBroj = radniciZaIzvoz
                .Select(r =>
                {
                    var evid = int.TryParse(r.EvidencijskiBroj, out var ev) ? ev : 0;
                    return Math.Max(r.Broj, evid);
                })
                .DefaultIfEmpty(0)
                .Max() + 1;

            foreach (var r in radniciZaIzvoz)
            {
                if (string.Equals(r.Brisanje, "D", StringComparison.OrdinalIgnoreCase))
                    continue;

                var broj = r.Broj;
                if (broj <= 0 && int.TryParse(r.EvidencijskiBroj, out var evidBroj) && evidBroj > 0)
                    broj = evidBroj;

                if (broj <= 0 || usedBrojevi.Contains(broj))
                {
                    while (usedBrojevi.Contains(nextAutoBroj))
                        nextAutoBroj++;
                    broj = nextAutoBroj++;
                }

                usedBrojevi.Add(broj);

                existingByBroj.TryGetValue(broj, out var seed);
                var row = BuildEntityRow(r, schema, RadnikPropMap, RadnikFieldAliases, seed);
                row["BROJ"] = broj;
                row["IME_PREZ"] = string.IsNullOrWhiteSpace(r.ImePrezime)
                    ? $"{r.Prezime} {r.Ime}".Trim()
                    : r.ImePrezime.Trim();
                row["PREZIME"] = r.Prezime ?? string.Empty;
                row["IME"] = r.Ime ?? string.Empty;
                row["EVIDBROJ"] = string.IsNullOrWhiteSpace(r.EvidencijskiBroj)
                    ? broj.ToString()
                    : r.EvidencijskiBroj;
                if (string.IsNullOrWhiteSpace(Convert.ToString(row.GetValueOrDefault("BRISANJE"))))
                    row["BRISANJE"] = "N";

                rows.Add(row);
            }

            if (rows.Count > 0)
            {
                DbfTableWriter.WriteTable(
                    targetPath,
                    schema,
                    rows,
                    static (r, fieldName) => r.TryGetValue(fieldName, out var value) ? value : null);
                ObrisiStariIndex(targetPath);
                upisaniFajlovi.Add(targetPath);
            }
        }

        return LdFoxExportResult.Ok(
            stavke.Count,
            upisaniFajlovi.Count,
            backupPath,
            upisaniFajlovi);
    }

    private static List<Radnik> NormalizujRadnikeZaIzvoz(List<Radnik> radnici)
    {
        return radnici
            .Where(r => !string.Equals(r.Brisanje, "D", StringComparison.OrdinalIgnoreCase))
            .GroupBy(OdrediKljucRadnikaZaIzvoz, StringComparer.OrdinalIgnoreCase)
            .Select(IzaberiNajboljiZapisRadnika)
            .ToList();
    }

    private static string OdrediKljucRadnikaZaIzvoz(Radnik r)
    {
        if (!string.IsNullOrWhiteSpace(r.EvidencijskiBroj))
            return $"E:{r.EvidencijskiBroj.Trim()}";

        if (r.Broj > 0)
            return $"B:{r.Broj}";

        var prezime = r.Prezime?.Trim() ?? string.Empty;
        var ime = r.Ime?.Trim() ?? string.Empty;
        var maticni = r.MaticniBroj?.Trim() ?? string.Empty;
        return $"N:{prezime}|{ime}|{maticni}";
    }

    private static Radnik IzaberiNajboljiZapisRadnika(IGrouping<string, Radnik> grupa)
    {
        return grupa
            .OrderByDescending(r => r.Broj > 0)
            .ThenByDescending(r => r.Broj)
            .ThenByDescending(r => r.Id)
            .First();
    }

    private static Dictionary<string, List<LdObracunStavka>> GroupByTarget(List<LdObracunStavka> stavke)
    {
        var result = new Dictionary<string, List<LdObracunStavka>>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in stavke)
        {
            var file = OdrediNazivFajla(s);
            if (!result.TryGetValue(file, out var lista))
            {
                lista = [];
                result[file] = lista;
            }
            lista.Add(s);
        }
        return result;
    }

    private static async Task DodajAliasTabeleTrenutnogMeseca(
        FirmaDbContext ctx,
        List<LdObracunStavka> stavke,
        Dictionary<string, List<LdObracunStavka>> groups)
    {
        var aktivanMesec = await ctx.LdParametri
            .AsNoTracking()
            .Select(x => x.Mesec)
            .FirstOrDefaultAsync();

        if (aktivanMesec is < 1 or > 99)
            return;

        DodajAlias(groups, "LD.DBF", stavke.Where(s => s.Isplata == 1 && s.Mesec == aktivanMesec).ToList());
        DodajAlias(groups, "LDP.DBF", stavke.Where(s => s.Isplata == 2 && s.Mesec == aktivanMesec).ToList());
        DodajAlias(groups, "LDB.DBF", stavke.Where(s => s.Isplata == 3 && s.Mesec == aktivanMesec).ToList());
    }

    private static void DodajAlias(Dictionary<string, List<LdObracunStavka>> groups, string alias, List<LdObracunStavka> rows)
    {
        if (rows.Count == 0)
            return;

        groups[alias] = rows;
    }

    private static string OdrediNazivFajla(LdObracunStavka s)
    {
        var prefix = s.Isplata switch
        {
            2 => "LDP",
            3 => OdrediTrecuVrstuPrefix(s.Vrsta),
            _ => "LD"
        };

        var mesec = s.Mesec;
        if (mesec is >= 1 and <= 99)
            return $"{prefix}{mesec}.DBF";

        return $"{prefix}.DBF";
    }

    private static string OdrediTrecuVrstuPrefix(string? vrsta)
    {
        var ozn = (vrsta ?? string.Empty).Trim().ToUpperInvariant();
        return ozn switch
        {
            "I" => "LDI",
            "R" => "LDR",
            _ => "LDB"
        };
    }

    private static Dictionary<string, string> ResolveTemplatePaths(
        Dictionary<string, List<LdObracunStavka>> groups,
        LdParametar? ldParametar,
        List<Radnik> radnici,
        string firmaFolderPath,
        string? templateF1)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in groups.Where(g => g.Value.Count > 0).Select(g => g.Key))
        {
            var template = PronadjiTemplatePath(file, firmaFolderPath, templateF1);
            if (template is null)
                throw new InvalidOperationException($"Template za {file} nije pronadjen.");
            result[file] = template;
        }

        if (ldParametar is not null)
        {
            var template = PronadjiTemplatePath(
                "LDPARAM.DBF",
                firmaFolderPath,
                templateF1,
                ["LDPARAM.DBF", "LDPAR.DBF", "LDPAR2.DBF"]);

            if (template is null)
                throw new InvalidOperationException("Template za LDPARAM.DBF nije pronadjen.");

            result["LDPARAM.DBF"] = template;
        }

        if (radnici.Count > 0)
        {
            var template = PronadjiTemplatePath(
                "LDRAD.DBF",
                firmaFolderPath,
                templateF1,
                ["LDRAD.DBF"]);

            if (template is null)
                throw new InvalidOperationException("Template za LDRAD.DBF nije pronadjen.");

            result["LDRAD.DBF"] = template;
        }

        return result;
    }

    private static DbfTableWriter.DbfSchema GetOrLoadSchema(
        Dictionary<string, DbfTableWriter.DbfSchema> schemaCache,
        string templatePath)
    {
        if (!schemaCache.TryGetValue(templatePath, out var schema))
        {
            schema = DbfTableWriter.LoadSchema(templatePath);
            schemaCache[templatePath] = schema;
        }

        return schema;
    }

    private static string? PronadjiTemplatePath(
        string targetFileName,
        string firmaFolderPath,
        string? templateF1,
        IEnumerable<string>? specificCandidates = null)
    {
        var directTarget = Path.Combine(firmaFolderPath, targetFileName);
        if (File.Exists(directTarget))
            return directTarget;

        var candidates = specificCandidates?.ToList() ?? KandidatiTemplatea(targetFileName).ToList();
        foreach (var candidate in candidates)
        {
            var local = Path.Combine(firmaFolderPath, candidate);
            if (File.Exists(local))
                return local;

            if (!string.IsNullOrWhiteSpace(templateF1))
            {
                var template = Path.Combine(templateF1!, candidate);
                if (File.Exists(template))
                    return template;
            }
        }

        return null;
    }

    private static IEnumerable<string> KandidatiTemplatea(string targetFileName)
    {
        var upper = targetFileName.ToUpperInvariant();
        if (upper.StartsWith("LDP"))
            return ["LDP.DBF", "LD.DBF", "LD0.DBF", "LD00.DBF"];
        if (upper.StartsWith("LDB"))
            return ["LDB.DBF", "LD.DBF", "LD0.DBF", "LD00.DBF"];
        if (upper.StartsWith("LDI"))
            return ["LDI.DBF", "LD.DBF", "LD0.DBF", "LD00.DBF"];
        if (upper.StartsWith("LDR"))
            return ["LDR.DBF", "LD.DBF", "LD0.DBF", "LD00.DBF"];
        return ["LD.DBF", "LD0.DBF", "LD00.DBF"];
    }

    private static object? ResolveValue(LdObracunStavka row, string fieldName)
    {
        if (string.Equals(fieldName, "PRENETO", StringComparison.OrdinalIgnoreCase))
            return "N";

        if (LdObracunPropMap.TryGetValue(Normalize(fieldName), out var prop))
            return prop.GetValue(row);

        return null;
    }

    private static Dictionary<string, object?> BuildEntityRow<T>(
        T entity,
        DbfTableWriter.DbfSchema schema,
        Dictionary<string, PropertyInfo> propMap,
        Dictionary<string, string>? aliases,
        Dictionary<string, object?>? seed = null)
    {
        var row = seed is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(seed, StringComparer.OrdinalIgnoreCase);

        foreach (var field in schema.Fields)
        {
            if (TryResolveProperty(entity!, field.Name, propMap, aliases, out var value))
                row[field.Name] = value;
        }

        return row;
    }

    private static bool TryResolveProperty<T>(
        T entity,
        string fieldName,
        Dictionary<string, PropertyInfo> propMap,
        Dictionary<string, string>? aliases,
        out object? value)
    {
        value = null;
        PropertyInfo? prop = null;

        if (aliases is not null && aliases.TryGetValue(fieldName, out var propName))
            propMap.TryGetValue(Normalize(propName), out prop);

        if (prop is null)
            propMap.TryGetValue(Normalize(fieldName), out prop);

        if (prop is null)
            return false;

        value = prop.GetValue(entity);
        return true;
    }

    private static Dictionary<int, Dictionary<string, object?>> UcitajPostojeceRadnikePoBroju(string targetPath)
    {
        var result = new Dictionary<int, Dictionary<string, object?>>();
        if (!File.Exists(targetPath))
            return result;

        foreach (var row in DbfReader.CitajSveZapise(targetPath))
        {
            if (!TryParseInt(row.GetValueOrDefault("BROJ"), out var broj))
                continue;

            result[broj] = new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase);
        }

        return result;
    }

    private static Dictionary<string, object?>? TryUcitajPostojeciRed(string targetPath)
    {
        if (!File.Exists(targetPath))
            return null;

        var first = DbfReader.CitajSveZapise(targetPath).FirstOrDefault();
        return first is null
            ? null
            : new Dictionary<string, object?>(first, StringComparer.OrdinalIgnoreCase);
    }

    private static bool TryParseInt(object? value, out int number)
    {
        number = 0;
        if (value is null)
            return false;

        if (value is decimal dec)
        {
            number = (int)dec;
            return true;
        }

        if (value is int i)
        {
            number = i;
            return true;
        }

        var s = Convert.ToString(value);
        return int.TryParse(s, out number);
    }

    private static Dictionary<string, PropertyInfo> BuildPropMap<T>()
        => typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => Normalize(p.Name), p => p, StringComparer.OrdinalIgnoreCase);

    private static async Task<bool> PostojiTabelaAsync(FirmaDbContext ctx, string tableName)
    {
        var q = "SELECT COUNT(1) AS Value FROM sqlite_master WHERE type='table' AND name = {0}";
        var count = await ctx.Database.SqlQueryRaw<long>(q, tableName).SingleAsync();
        return count > 0;
    }

    private static string Normalize(string value)
        => new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    private static string NapraviBackupPostojecihLdTabela(string firmaFolderPath)
    {
        var root = Path.Combine(
            ZaradePaths.GetFirmaZaradeFolder(firmaFolderPath),
            "_fox_export_backups");
        Directory.CreateDirectory(root);

        var backupDir = Path.Combine(root, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(backupDir);

        var copied = 0;
        foreach (var src in Directory.GetFiles(firmaFolderPath, "LD*.*", SearchOption.TopDirectoryOnly))
        {
            var ext = Path.GetExtension(src);
            if (!ext.Equals(".dbf", StringComparison.OrdinalIgnoreCase)
                && !ext.Equals(".cdx", StringComparison.OrdinalIgnoreCase)
                && !ext.Equals(".fpt", StringComparison.OrdinalIgnoreCase)
                && !ext.Equals(".dbt", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dst = Path.Combine(backupDir, Path.GetFileName(src));
            File.Copy(src, dst, overwrite: true);
            copied++;
        }

        return copied == 0 ? string.Empty : backupDir;
    }

    private static void ObrisiStariIndex(string dbfPath)
    {
        var cdx = Path.ChangeExtension(dbfPath, ".cdx");
        if (File.Exists(cdx))
            File.Delete(cdx);
    }
}

public sealed record LdFoxExportResult(
    bool Success,
    string Message,
    int BrojStavki,
    int BrojFajlova,
    string BackupPath,
    IReadOnlyList<string> UpisaniFajlovi)
{
    public static LdFoxExportResult Fail(string message)
        => new(false, message, 0, 0, string.Empty, []);

    public static LdFoxExportResult Ok(int stavke, int fajlovi, string backupPath, IReadOnlyList<string> upisani)
        => new(true, "Izvoz uspesan.", stavke, fajlovi, backupPath, upisani);
}
