using System.IO;
using System.Text;

namespace OsnovnaSredstva.Services.Dbf;

internal static class DbfKreator
{
    internal record PoljeDbf(string Ime, char Tip, byte Duzina, byte Decimale);

    // dBASE III+ header + 0 records + EOF
    internal static void KreirajPrazanDbf(string putanja, IReadOnlyList<PoljeDbf> polja)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(putanja)!);

        int headerSize = 32 + polja.Count * 32 + 1;
        int recordSize = 1 + polja.Sum(p => p.Duzina);

        using var fs  = new FileStream(putanja, FileMode.Create, FileAccess.Write, FileShare.None);
        using var bw  = new BinaryWriter(fs, Encoding.ASCII);
        var now = DateTime.Now;

        // ─── Glavni header (32 bajta) ─────────────────────────────────────────
        bw.Write((byte)0x03);                     // version dBASE III
        bw.Write((byte)(now.Year - 1900));
        bw.Write((byte)now.Month);
        bw.Write((byte)now.Day);
        bw.Write((uint)0);                        // broj zapisa = 0
        bw.Write((ushort)headerSize);
        bw.Write((ushort)recordSize);
        bw.Write(new byte[16]);                   // rezervisano
        bw.Write((byte)0);
        bw.Write((byte)0xC8);                     // code page CP1250
        bw.Write((ushort)0);

        // ─── Deskriptori polja (32 bajta svaki) ──────────────────────────────
        foreach (var p in polja)
        {
            var nameBytes = new byte[11];
            var ascii     = Encoding.ASCII.GetBytes(p.Ime.ToUpperInvariant());
            Array.Copy(ascii, nameBytes, Math.Min(ascii.Length, 10));
            bw.Write(nameBytes);
            bw.Write((byte)p.Tip);
            bw.Write(new byte[4]);                // adresa polja (rezervisano)
            bw.Write(p.Duzina);
            bw.Write(p.Decimale);
            bw.Write(new byte[14]);               // rezervisano
        }

        bw.Write((byte)0x0D);                     // terminator headera
        bw.Write((byte)0x1A);                     // EOF
    }

    // ─── Šeme svih tabela ────────────────────────────────────────────────────

    private static readonly PoljeDbf[] _baznaOsPolja =
    [
        new("OSIFRA",   'C', 6,  0),
        new("NAZ",      'C', 50, 0),
        new("DATNAB",   'D', 8,  0),
        new("BRNAL",    'C', 10, 0),
        new("KONTO",    'C', 10, 0),
        new("VRSTA",    'C', 3,  0),
        new("AG",       'C', 3,  0),
        new("AGPOD",    'C', 3,  0),
        new("INVBROJ",  'C', 15, 0),
        new("MESTO",    'C', 5,  0),
        new("NAB0",     'N', 14, 2),
        new("ISP0",     'N', 14, 2),
        new("SAD0",     'N', 14, 2),
        new("KOM",      'N', 14, 2),
        new("CENA",     'N', 14, 2),
        new("STOPAOT",  'N', 8,  3),
        new("OSNOVKOR", 'C', 3,  0),
        new("IZVOR",    'C', 3,  0),
        new("PRENETO",  'C', 1,  0),
        new("IDBR",     'N', 6,  0),
    ];

    private static readonly PoljeDbf[] _extra0s0Polja =
    [
        new("MP",        'C', 5,  0),
        new("NOMENKL",   'C', 10, 0),
        new("NAB02",     'N', 14, 2),
        new("ISP02",     'N', 14, 2),
        new("SAD02",     'N', 14, 2),
        new("PROCGOD",   'N', 6,  2),
        new("STOPAOT2",  'N', 8,  3),
        new("GRUPA",     'C', 5,  0),
        new("OPER",      'C', 5,  0),
        new("DATSTARTAM",'D', 8,  0),
        new("IZNOSULAG", 'N', 14, 2),
        new("DATULAG",   'D', 8,  0),
        new("NACINOB",   'C', 5,  0),
        new("POLJE1",    'N', 14, 2),
        new("POLJE2",    'N', 14, 2),
    ];

    private static readonly PoljeDbf[] _extraEvidencijaPolja =
    [
        new("MP",        'C', 5,  0),
        new("NOMENKL",   'C', 10, 0),
        new("NAB02",     'N', 14, 2),
        new("ISP02",     'N', 14, 2),
        new("SAD02",     'N', 14, 2),
        new("PROCGOD",   'N', 6,  2),
        new("GRUPA",     'C', 5,  0),
        new("OPER",      'C', 5,  0),
        new("DATSTARTAM",'D', 8,  0),
        new("IZNOSULAG", 'N', 14, 2),
        new("DATULAG",   'D', 8,  0),
        new("NACINOB",   'C', 5,  0),
        new("POLJE1",    'N', 14, 2),
        new("POLJE2",    'N', 14, 2),
        new("AMORT",     'N', 14, 2),
        new("ISP",       'N', 14, 2),
        new("SAD",       'N', 14, 2),
        new("NAB",       'N', 14, 2),
        new("NAB2",      'N', 14, 2),
        new("ISP2",      'N', 14, 2),
        new("AMORT2",    'N', 14, 2),
        new("SAD2",      'N', 14, 2),
        new("STOPAOT2",  'N', 8,  3),
        new("DATUM0",    'D', 8,  0),
        new("DATUM1",    'D', 8,  0),
        new("DATPROD",   'D', 8,  0),
        new("PAM",       'N', 14, 2),
        new("RAM",       'N', 14, 2),
        new("OBEZVREDJ", 'N', 14, 2),
        new("BRMES",     'N', 2,  0),
    ];

    internal static IReadOnlyList<PoljeDbf> SemaOs0()
        => [.. _baznaOsPolja, .. _extra0s0Polja];

    internal static IReadOnlyList<PoljeDbf> SemaOsEvidencija()
        => [.. _baznaOsPolja, .. _extraEvidencijaPolja];

    internal static IReadOnlyList<PoljeDbf> SemaOsoa() =>
    [
        new("AG",      'C', 3,  0),
        new("POCETNO", 'N', 14, 2),
        new("NABAVKA", 'N', 14, 2),
        new("PRODAJA", 'N', 14, 2),
        new("NEOTPIS", 'N', 14, 2),
        new("AGSTOPA", 'N', 8,  3),
        new("AMORT2",  'N', 14, 2),
        new("SAD2",    'N', 14, 2),
        new("PRENETO", 'C', 1,  0),
        new("NUMRED",  'N', 6,  0),
        new("IDBR",    'N', 6,  0),
    ];

    internal static IReadOnlyList<PoljeDbf> SemaOspodaci() =>
    [
        new("EDAT0",  'D', 8,  0),
        new("EDAT1",  'D', 8,  0),
        new("EMES",   'N', 2,  0),
        new("BRNAL",  'C', 10, 0),
        new("DATDOK", 'D', 8,  0),
        new("KONAM",  'C', 10, 0),
    ];

    internal static IReadOnlyList<PoljeDbf> SemaAn0() =>
    [
        new("SIFRA",    'C', 6,  0),
        new("NAZIV",    'C', 50, 0),
        new("NAZIV2",   'C', 50, 0),
        new("POSTA",    'C', 6,  0),
        new("MESTO",    'C', 30, 0),
        new("ULICA",    'C', 30, 0),
        new("ULBROJ",   'C', 10, 0),
        new("TELEFON",  'C', 20, 0),
        new("TELEFON2", 'C', 20, 0),
        new("FAX",      'C', 20, 0),
        new("EMAIL",    'C', 50, 0),
        new("LICE1",    'C', 30, 0),
        new("TELLICE1", 'C', 20, 0),
        new("PIB",      'C', 9,  0),
        new("PIB2",     'C', 9,  0),
        new("MATICNI",  'C', 8,  0),
        new("ZIRORAC",  'C', 18, 0),
        new("DRZAVA",   'C', 15, 0),
        new("PRENETO",  'C', 1,  0),
        new("IDBR",     'N', 6,  0),
    ];

    internal static IReadOnlyList<PoljeDbf> SemaOsVrsta() =>
    [
        new("VRSTA",   'C', 3,  0),
        new("NAZIV",   'C', 50, 0),
        new("PRENETO", 'C', 1,  0),
        new("IDBR",    'N', 6,  0),
    ];

    internal static IReadOnlyList<PoljeDbf> SemaOsAg() =>
    [
        new("AG",      'C', 3,  0),
        new("AGSTOPA", 'N', 8,  3),
        new("OPIS",    'C', 50, 0),
        new("VRSTA",   'C', 3,  0),
        new("PRENETO", 'C', 1,  0),
        new("IDBR",    'N', 6,  0),
    ];

    internal static IReadOnlyList<PoljeDbf> SemaOsAgPod() =>
    [
        new("AGPOD",   'C', 3,  0),
        new("AG",      'C', 3,  0),
        new("OPIS",    'C', 50, 0),
        new("PRENETO", 'C', 1,  0),
        new("IDBR",    'N', 6,  0),
    ];

    internal static IReadOnlyList<PoljeDbf> SemaOsIzvorF() =>
    [
        new("IZVOR",   'C', 3,  0),
        new("NAZIV",   'C', 50, 0),
        new("PRENETO", 'C', 1,  0),
        new("IDBR",    'N', 6,  0),
    ];

    internal static IReadOnlyList<PoljeDbf> SemaOsOsnK() =>
    [
        new("OSNOVKOR", 'C', 3,  0),
        new("NAZIV",    'C', 50, 0),
        new("PRENETO",  'C', 1,  0),
        new("IDBR",     'N', 6,  0),
    ];

    internal static IReadOnlyList<PoljeDbf> SemaMesta() =>
    [
        new("MP",       'C', 5,  0),
        new("POSTA",    'C', 6,  0),
        new("MESTO",    'C', 30, 0),
        new("ZIRO1",    'C', 18, 0),
        new("ZIRO2",    'C', 18, 0),
        new("PORBROJ",  'C', 10, 0),
        new("PORBROJP", 'C', 10, 0),
        new("REGSOC",   'C', 10, 0),
        new("POR",      'N', 8,  4),
        new("ZDR",      'N', 8,  4),
        new("PIO",      'N', 8,  4),
        new("NEZ",      'N', 8,  4),
        new("VRSTA",    'C', 3,  0),
        new("PRENETO",  'C', 1,  0),
    ];

    internal static IReadOnlyList<PoljeDbf> SemaOspopis() =>
    [
        new("MESTO",  'C', 5,  0),
        new("MTR",    'N', 3,  0),
        new("KONTO",  'C', 10, 0),
        new("AG",     'C', 3,  0),
        new("AGPOD",  'C', 3,  0),
        new("GRUPA",  'C', 5,  0),
    ];

    internal static IReadOnlyList<PoljeDbf> SemaKonto() =>
    [
        new("KONTO",   'C', 10, 0),
        new("OPIS",    'C', 50, 0),
        new("PRENETO", 'C', 1,  0),
        new("IDBR",    'N', 6,  0),
    ];
}
