using Algoritam.Infrastructure.Dbf;
using System.Globalization;
using System.IO;

namespace Algoritam.WPF.ViewModels;

internal static class PartneriDbfSupport
{
    public static List<PartnerStavka> UcitajPartnere(string folderPath)
    {
        var dbfPath = KreditiDbfSupport.PronadjiDbf(folderPath, "an0.dbf");
        if (dbfPath == null)
            return [];

        var zapisi = DbfReader.CitajSveZapise(dbfPath);
        return zapisi.Select(z =>
        {
            var stavka = new PartnerStavka
            {
                Sifra = Str(z, "SIFRA"),
                Naziv = Str(z, "NAZIV"),
                Naziv2 = Str(z, "NAZIV2"),
                Posta = Str(z, "POSTA"),
                Mesto = Str(z, "MESTO"),
                Ulica = Str(z, "ULICA"),
                Ulbroj = Str(z, "ULBROJ"),
                Telefon = Str(z, "TELEFON"),
                Telefon2 = Str(z, "TELEFON2"),
                Fax = Str(z, "FAX"),
                Email = Str(z, "EMAIL"),
                Lice1 = Str(z, "LICE1"),
                TelLice1 = Str(z, "TELLICE1"),
                Pib = Str(z, "PIB"),
                Pib2 = Str(z, "PIB2"),
                Maticni = Str(z, "MATICNI"),
                ZiroRac = Str(z, "ZIRORAC"),
                Drzava = Str(z, "DRZAVA"),
                Preneto = Str(z, "PRENETO"),
                IdBr = Long(z, "IDBR")
            };

            stavka.SetOriginalValues(z);
            return stavka;
        }).ToList();
    }

    public static void SacuvajPartnere(string folderPath, IReadOnlyCollection<PartnerStavka> stavke)
    {
        var targetPath = KreditiDbfSupport.PronadjiDbf(folderPath, "an0.dbf") ?? Path.Combine(folderPath, "an0.dbf");
        var schema = UcitajSemu(targetPath);

        DbfTableWriter.WriteTable(targetPath, schema, stavke.ToList(), ResolvePartnerValue);
    }

    private static DbfTableWriter.DbfSchema UcitajSemu(string targetPath)
    {
        if (File.Exists(targetPath))
            return DbfTableWriter.LoadSchema(targetPath);

        foreach (var root in KandidatiZaRoot())
        {
            var oldTemplate = Path.Combine(root, "old-project", "F1", "an0.dbf");
            if (File.Exists(oldTemplate))
                return DbfTableWriter.LoadSchema(oldTemplate);
        }

        throw new FileNotFoundException("Sema za an0.dbf nije pronađena.");
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

    private static object? ResolvePartnerValue(PartnerStavka row, string fieldName) => fieldName.ToUpperInvariant() switch
    {
        "SIFRA" => NormalizeText(row.Sifra),
        "NAZIV" => NormalizeText(row.Naziv),
        "NAZIV2" => NormalizeText(row.Naziv2),
        "POSTA" => NormalizeText(row.Posta),
        "MESTO" => NormalizeText(row.Mesto),
        "ULICA" => NormalizeText(row.Ulica),
        "ULBROJ" => NormalizeText(row.Ulbroj),
        "TELEFON" => NormalizeText(row.Telefon),
        "TELEFON2" => NormalizeText(row.Telefon2),
        "FAX" => NormalizeText(row.Fax),
        "EMAIL" => NormalizeText(row.Email),
        "LICE1" => NormalizeText(row.Lice1),
        "TELLICE1" => NormalizeText(row.TelLice1),
        "PIB" => NormalizeText(row.Pib),
        "PIB2" => NormalizeText(string.IsNullOrWhiteSpace(row.Pib2) ? row.Sifra : row.Pib2),
        "MATICNI" => NormalizeText(row.Maticni),
        "ZIRORAC" => NormalizeText(row.ZiroRac),
        "DRZAVA" => NormalizeText(row.Drzava),
        "PRENETO" => NormalizeText(string.IsNullOrWhiteSpace(row.Preneto) ? " " : row.Preneto),
        "IDBR" => row.IdBr,
        _ => row.TryGetOriginalValue(fieldName, out var original) ? original : null
    };

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;

    private static long Long(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v == null)
            return 0L;

        if (v is long l) return l;
        if (v is int i) return i;
        if (v is decimal d) return (long)d;

        return long.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), out var parsed)
            ? parsed
            : 0L;
    }

    private static string NormalizeText(string? value)
    {
        return (value ?? string.Empty).Trim();
    }
}
