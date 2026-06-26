using Algoritam.Domain.Entities;
using Algoritam.Infrastructure.Dbf;
using System.Globalization;
using System.IO;

namespace Algoritam.WPF.ViewModels;

internal static class LdDbfMutator
{
    internal readonly record struct LdDbfMutatorResult(int Fajlovi, int Obradjeno, int Izmenjeno);
    private readonly record struct LdFajlKandidat(string Putanja, int Isplata, string Vrsta);

    private static readonly (string Prefix, int Isplata, string Vrsta)[] Tipovi =
    [
        ("LD", 1, "A"),
        ("LDP", 2, "P"),
        ("LDB", 3, "B"),
        ("LDI", 3, "B"),
        ("LDR", 3, "B"),
    ];

    public static Task<LdDbfMutatorResult> PostaviArhivuPoStavkamaAsync(
        string folderPath,
        IReadOnlyCollection<LdObracunStavka> stavke,
        string arhivaVrednost)
    {
        return Task.Run(() =>
        {
            var cilj = NapraviMultiskup(stavke.Select(NapraviKljuc));
            if (cilj.Count == 0)
                return default(LdDbfMutatorResult);

            var vrednost = (arhivaVrednost ?? string.Empty).Trim();
            return ObradiLdFajlove(folderPath, (Dictionary<string, object?> row, LdFajlKandidat fajl, ref bool keep) =>
            {
                if (!PotrosiKljuc(cilj, NapraviKljuc(row, fajl.Isplata, fajl.Vrsta)))
                    return false;

                if (!row.TryGetValue("ARHIVA", out var postojece) ||
                    !string.Equals((postojece?.ToString() ?? string.Empty).Trim(), vrednost, StringComparison.Ordinal))
                {
                    row["ARHIVA"] = vrednost;
                    return true;
                }

                return false;
            });
        });
    }

    public static Task<LdDbfMutatorResult> ObrisiStavkeAsync(
        string folderPath,
        IReadOnlyCollection<LdObracunStavka> stavke)
    {
        return Task.Run(() =>
        {
            var cilj = NapraviMultiskup(stavke.Select(NapraviKljuc));
            if (cilj.Count == 0)
                return default(LdDbfMutatorResult);

            return ObradiLdFajlove(folderPath, (Dictionary<string, object?> row, LdFajlKandidat fajl, ref bool keep) =>
            {
                if (!PotrosiKljuc(cilj, NapraviKljuc(row, fajl.Isplata, fajl.Vrsta)))
                    return false;

                keep = false;
                return true;
            });
        });
    }

    public static Task<LdDbfMutatorResult> ObrisiPoUslovuAsync(
        string folderPath,
        Func<Dictionary<string, object?>, bool> trebaObrisati)
    {
        return Task.Run(() =>
        {
            if (trebaObrisati == null)
                return default(LdDbfMutatorResult);

            return ObradiLdFajlove(folderPath, (Dictionary<string, object?> row, LdFajlKandidat _, ref bool keep) =>
            {
                if (!trebaObrisati(row))
                    return false;

                keep = false;
                return true;
            });
        });
    }

    private static LdDbfMutatorResult ObradiLdFajlove(
        string folderPath,
        MutacijaZapisa mutacija)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return default;

        var fajlovi = PronadjiLdFajlove(folderPath);
        var obradjeno = 0;
        var izmenjeno = 0;
        var izmenjeniFajlovi = 0;

        foreach (var fajl in fajlovi)
        {
            var putanja = fajl.Putanja;
            var zapisi = DbfReader.CitajSveZapise(putanja);
            if (zapisi.Count == 0)
                continue;

            var ostali = new List<Dictionary<string, object?>>(zapisi.Count);
            var fajlIzmenjen = false;

            foreach (var row in zapisi)
            {
                obradjeno++;
                var zadrzi = true;
                if (mutacija(row, fajl, ref zadrzi))
                {
                    izmenjeno++;
                    fajlIzmenjen = true;
                }

                if (zadrzi)
                    ostali.Add(row);
            }

            if (!fajlIzmenjen)
                continue;

            var schema = DbfTableWriter.LoadSchema(putanja);
            DbfTableWriter.WriteTable(
                putanja,
                schema,
                ostali,
                static (r, fieldName) => r.TryGetValue(fieldName, out var v) ? v : null);

            izmenjeniFajlovi++;
        }

        return new LdDbfMutatorResult(izmenjeniFajlovi, obradjeno, izmenjeno);
    }

    private static List<LdFajlKandidat> PronadjiLdFajlove(string folderPath)
    {
        var rezultat = new List<LdFajlKandidat>();
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (naziv, isplata, vrsta) in EnumerirajKandidate())
        {
            var putanja = LdObracunDbfReader.PronadjiDbf(folderPath, naziv);
            if (string.IsNullOrWhiteSpace(putanja) || !set.Add(putanja))
                continue;

            rezultat.Add(new LdFajlKandidat(putanja, isplata, vrsta));
        }

        return rezultat;
    }

    private static IEnumerable<(string FileName, int Isplata, string Vrsta)> EnumerirajKandidate()
    {
        foreach (var (prefix, isplata, vrsta) in Tipovi)
        {
            yield return ($"{prefix}.dbf", isplata, vrsta);
            yield return ($"{prefix}0.dbf", isplata, vrsta);
            yield return ($"{prefix}00.dbf", isplata, vrsta);

            for (int i = 1; i <= 99; i++)
            {
                yield return ($"{prefix}{i}.dbf", isplata, vrsta);
                yield return ($"{prefix}{i:00}.dbf", isplata, vrsta);
            }
        }
    }

    private static Dictionary<string, int> NapraviMultiskup(IEnumerable<string> kljucevi)
    {
        var mapa = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var kljuc in kljucevi.Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            if (mapa.TryGetValue(kljuc, out var broj))
                mapa[kljuc] = broj + 1;
            else
                mapa[kljuc] = 1;
        }

        return mapa;
    }

    private static bool PotrosiKljuc(Dictionary<string, int> multiskup, string kljuc)
    {
        if (string.IsNullOrWhiteSpace(kljuc))
            return false;

        if (!multiskup.TryGetValue(kljuc, out var broj) || broj <= 0)
            return false;

        if (broj == 1)
            multiskup.Remove(kljuc);
        else
            multiskup[kljuc] = broj - 1;

        return true;
    }

    private static string NapraviKljuc(LdObracunStavka s)
    {
        if (s.Idbr != 0)
            return $"IDBR:{s.Idbr}";

        return string.Join("|",
            s.Broj,
            s.Mesec,
            (s.Godina ?? string.Empty).Trim().ToUpperInvariant(),
            s.Isplata,
            (s.Vrsta ?? string.Empty).Trim().ToUpperInvariant(),
            (s.Maticnibr ?? string.Empty).Trim().ToUpperInvariant(),
            (s.Idbroj ?? string.Empty).Trim().ToUpperInvariant(),
            (s.ImePrez ?? string.Empty).Trim().ToUpperInvariant(),
            DecString(s.Bruto),
            DecString(s.Neto),
            DecString(s.Zaisplatu));
    }

    private static string NapraviKljuc(Dictionary<string, object?> z, int defaultIsplata, string defaultVrsta)
    {
        var idbr = Long(z, "IDBR");
        if (idbr != 0)
            return $"IDBR:{idbr}";

        var isplata = Int(z, "ISPLATA");
        if (isplata <= 0)
            isplata = defaultIsplata;

        var vrsta = Str(z, "VRSTA");
        if (string.IsNullOrWhiteSpace(vrsta))
            vrsta = (defaultVrsta ?? string.Empty).Trim().ToUpperInvariant();

        return string.Join("|",
            Int(z, "BROJ"),
            Int(z, "MESEC"),
            Str(z, "GODINA"),
            isplata,
            vrsta,
            Str(z, "MATICNIBR"),
            Str(z, "IDBROJ"),
            Str(z, "IME_PREZ"),
            DecString(Dec(z, "BRUTO")),
            DecString(Dec(z, "NETO")),
            DecString(Dec(z, "ZAISPLATU")));
    }

    private static string DecString(decimal vrednost)
        => vrednost.ToString("0.##########", CultureInfo.InvariantCulture);

    private static string Str(Dictionary<string, object?> z, string k)
        => z.TryGetValue(k, out var v) && v != null
            ? (v.ToString() ?? string.Empty).Trim().ToUpperInvariant()
            : string.Empty;

    private static int Int(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0;
        if (v is decimal d) return (int)d;
        if (v is int i) return i;
        if (int.TryParse(v.ToString(), out var parsed)) return parsed;
        return 0;
    }

    private static long Long(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0L;
        if (v is decimal d) return (long)d;
        if (v is long l) return l;
        if (long.TryParse(v.ToString(), out var parsed)) return parsed;
        return 0L;
    }

    private static decimal Dec(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0m;
        if (v is decimal d) return d;
        if (decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        if (decimal.TryParse(v.ToString(), out parsed))
            return parsed;
        return 0m;
    }

    private delegate bool MutacijaZapisa(Dictionary<string, object?> row, LdFajlKandidat fajl, ref bool keep);
}
