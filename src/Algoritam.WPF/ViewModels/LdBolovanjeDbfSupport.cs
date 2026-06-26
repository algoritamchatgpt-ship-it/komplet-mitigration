using Algoritam.Infrastructure.Dbf;
using System.Globalization;
using System.IO;

namespace Algoritam.WPF.ViewModels;

internal static class LdBolovanjeDbfSupport
{
    public static string? PronadjiDbf(string folderPath, string fileName)
        => KreditiDbfSupport.PronadjiDbf(folderPath, fileName);

    public static string? PronadjiPrviDbf(string folderPath, params string[] fileNames)
    {
        foreach (var fileName in fileNames)
        {
            var putanja = PronadjiDbf(folderPath, fileName);
            if (!string.IsNullOrWhiteSpace(putanja))
                return putanja;
        }

        return null;
    }

    public static DbfTableWriter.DbfSchema UcitajSemu(string targetPath, string fileName)
    {
        if (File.Exists(targetPath))
            return DbfTableWriter.LoadSchema(targetPath);

        foreach (var root in KandidatiZaRoot())
        {
            var convertTemplate = Path.Combine(root, "src", "Algoritam.WPF", "convert to sql", fileName);
            if (File.Exists(convertTemplate))
                return DbfTableWriter.LoadSchema(convertTemplate);

            var convertTemplateNested = Path.Combine(root, "newproject", "src", "Algoritam.WPF", "convert to sql", fileName);
            if (File.Exists(convertTemplateNested))
                return DbfTableWriter.LoadSchema(convertTemplateNested);

            var oldTemplate = Path.Combine(root, "old-project", "F1", fileName);
            if (File.Exists(oldTemplate))
                return DbfTableWriter.LoadSchema(oldTemplate);

            var oldTemplateDb = Path.Combine(root, "old-project", "databaze", fileName);
            if (File.Exists(oldTemplateDb))
                return DbfTableWriter.LoadSchema(oldTemplateDb);
        }

        throw new FileNotFoundException($"Sema za {fileName} nije pronađena.");
    }

    public static void SacuvajTabelu<T>(
        string folderPath,
        string fileName,
        IReadOnlyList<T> rows,
        Func<T, string, object?> valueResolver)
    {
        var targetPath = PronadjiDbf(folderPath, fileName) ?? Path.Combine(folderPath, fileName);
        var schema = UcitajSemu(targetPath, fileName);
        DbfTableWriter.WriteTable(targetPath, schema, rows, valueResolver);
    }

    public static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;

    public static decimal Dec(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;

    public static int Int(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? (int)d : 0;

    public static long Long(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? (long)d : 0L;

    public static DateTime? Dat(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null)
            return null;

        return v switch
        {
            DateTime dt => dt,
            string s when DateTime.TryParse(s, out var p) => p,
            _ => null
        };
    }

    public static string NormalizeText(string? value)
        => (value ?? string.Empty).Trim();

    public static decimal Round(decimal value, int decimals)
        => Math.Round(value, decimals, MidpointRounding.AwayFromZero);

    public static int MesecIzNaziva(string? nazivMeseca)
    {
        var n = NormalizeText(nazivMeseca).ToUpperInvariant();
        if (n.StartsWith("JAN")) return 1;
        if (n.StartsWith("FEB")) return 2;
        if (n.StartsWith("MAR")) return 3;
        if (n.StartsWith("APR")) return 4;
        if (n.StartsWith("MAJ")) return 5;
        if (n.StartsWith("JUN")) return 6;
        if (n.StartsWith("JUL")) return 7;
        if (n.StartsWith("AVG")) return 8;
        if (n.StartsWith("SEP")) return 9;
        if (n.StartsWith("OKT")) return 10;
        if (n.StartsWith("NOV")) return 11;
        if (n.StartsWith("DEC")) return 12;
        return 0;
    }

    public static bool OdgovaraPeriodu(
        Dictionary<string, object?> zapis,
        int mesec,
        int isplata,
        string? godina)
    {
        if (mesec > 0 && Int(zapis, "MESEC") != mesec)
            return false;

        if (isplata > 0)
        {
            var isplataZapisa = Int(zapis, "ISPLATA");
            // ISPLATA=0 u nekim tabelama predstavlja "sve".
            if (isplataZapisa > 0 && isplataZapisa != isplata)
                return false;
        }

        return GodinaSePoklapa(Str(zapis, "GODINA"), godina);
    }

    public static bool GodinaSePoklapa(string? godinaZapisa, string? trazenaGodina)
    {
        var trazena = NormalizeText(trazenaGodina);
        if (string.IsNullOrWhiteSpace(trazena))
            return true;

        var zapis = NormalizeText(godinaZapisa);
        if (string.IsNullOrWhiteSpace(zapis))
            return true;

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

    public static string Format2(decimal value)
        => value.ToString("N2", CultureInfo.CurrentCulture);

    private static IEnumerable<string> KandidatiZaRoot()
    {
        var kandidati = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        static void DodajPutanje(HashSet<string> set, string? startPath)
        {
            if (string.IsNullOrWhiteSpace(startPath))
                return;

            var dir = new DirectoryInfo(startPath);
            while (dir is not null)
            {
                set.Add(dir.FullName);
                dir = dir.Parent;
            }
        }

        DodajPutanje(kandidati, AppContext.BaseDirectory);
        DodajPutanje(kandidati, Environment.CurrentDirectory);

        return kandidati;
    }
}
