using Algoritam.Domain.Entities;
using System.IO;

namespace Algoritam.Infrastructure.Dbf;

/// <summary>
/// Cita sve LD*.dbf, LDP*.dbf, LDB*.dbf, LDI*.dbf, LDR*.dbf fajlove
/// iz foldera firme i vraca konsolidovanu listu LdObracunStavka.
/// Koristi se kao zamena za SQLite tabelu LdObracunStavke.
/// </summary>
public static class LdObracunDbfReader
{
    private static readonly (string Prefix, int Isplata, string Vrsta)[] Tipovi =
    [
        ("LD",  1, "A"),
        ("LDP", 2, "P"),
        ("LDB", 3, "B"),
        ("LDI", 3, "B"),
        ("LDR", 3, "B"),
    ];

    /// <summary>
    /// Cita sve LD*.dbf fajlove i vraca listu LdObracunStavka (bez filtera).
    /// </summary>
    public static List<LdObracunStavka> CitajSve(string folderPath)
    {
        var rezultat = new List<LdObracunStavka>();
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return rezultat;


        var poseceniFajlovi = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (fileName, isplata, vrsta) in EnumerirajKandidate())
        {
            var putanja = PronadjiDbf(folderPath, fileName);
            if (putanja == null || !poseceniFajlovi.Add(putanja))
                continue;

            try
            {
                var zapisi = DbfReader.CitajSveZapise(putanja);
                foreach (var z in zapisi)
                {
                    var stavka = MapirajZapis(z);
                    stavka.Isplata = isplata;
                    stavka.Vrsta = vrsta;
                    rezultat.Add(stavka);
                }
            }
            catch
            {
                // Preskacemo ostecene fajlove
            }
        }
        return rezultat;
    }

    /// <summary>
    /// Vraca popis svih dostupnih LD fajlova u folderu (za prikaz statusa).
    /// </summary>
    public static List<(string Fajl, int Isplata, string Vrsta, int BrojZapisa)> PopisFajlova(string folderPath)
    {
        var popis = new List<(string, int, string, int)>();
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return popis;


        var poseceniFajlovi = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (fileName, isplata, vrsta) in EnumerirajKandidate())
        {
            var putanja = PronadjiDbf(folderPath, fileName);
            if (putanja == null || !poseceniFajlovi.Add(putanja))
                continue;

            int brojZapisa = 0;
            try { brojZapisa = DbfReader.CitajSveZapise(putanja).Count; }
            catch { }

            popis.Add((Path.GetFileName(putanja), isplata, vrsta, brojZapisa));
        }
        return popis;
    }

    private static IEnumerable<(string FileName, int Isplata, string Vrsta)> EnumerirajKandidate()
    {
        foreach (var (prefix, isplata, vrsta) in Tipovi)
        {
            // Fox baze cesto koriste bazni fajl (npr. LD.DBF) i/ili 00 varijantu.
            yield return ($"{prefix}.DBF", isplata, vrsta);
            yield return ($"{prefix}0.DBF", isplata, vrsta);
            yield return ($"{prefix}00.DBF", isplata, vrsta);

            for (int i = 1; i <= 99; i++)
            {
                yield return ($"{prefix}{i}.DBF", isplata, vrsta);
                yield return ($"{prefix}{i:00}.DBF", isplata, vrsta);
            }
        }
    }

    private static LdObracunStavka MapirajZapis(Dictionary<string, object?> z)
    {
        return new LdObracunStavka
        {
            Broj = Int(z, "BROJ"),
            Sifraprih = Str(z, "SIFRAPRIH"),
            ImePrez = Str(z, "IME_PREZ"),
            Evidbroj = Str(z, "EVIDBROJ"),
            Maticnibr = Str(z, "MATICNIBR"),
            Idbroj = Str(z, "IDBROJ"),
            Dok = Str(z, "DOK"),
            Grupa = Int(z, "GRUPA"),
            Grupa1 = Int(z, "GRUPA1"),
            Mtr = Int(z, "MTR"),
            Mesec = Int(z, "MESEC"),
            Nazmes = Str(z, "NAZMES"),
            Godina = Str(z, "GODINA"),
            Casvr = Dec(z, "CASVR"),
            Casuc = Dec(z, "CASUC"),
            Casnoc = Dec(z, "CASNOC"),
            Casprod = Dec(z, "CASPROD"),
            Casradnap = Dec(z, "CASRADNAP"),
            Casned = Dec(z, "CASNED"),
            Casdor = Dec(z, "CASDOR"),
            Cslput = Dec(z, "CSLPUT"),
            Caspraz = Dec(z, "CASPRAZ"),
            Casbol = Dec(z, "CASBOL"),
            Casbol2 = Dec(z, "CASBOL2"),
            Casplac = Dec(z, "CASPLAC"),
            Casplac2 = Dec(z, "CASPLAC2"),
            Casgod = Dec(z, "CASGOD"),
            Casvv = Dec(z, "CASVV"),
            Cas1 = Dec(z, "CAS1"),
            Cas2 = Dec(z, "CAS2"),
            Cas3 = Dec(z, "CAS3"),
            Cassus = Dec(z, "CASSUS"),
            Casneplac = Dec(z, "CASNEPLAC"),
            Caspriprav = Dec(z, "CASPRIPRAV"),
            Casuk = Dec(z, "CASUK"),
            Dinvr = Dec(z, "DINVR"),
            Dinuc = Dec(z, "DINUC"),
            Dinnoc = Dec(z, "DINNOC"),
            Dinprod = Dec(z, "DINPROD"),
            Dinradnap = Dec(z, "DINRADNAP"),
            Dinned = Dec(z, "DINNED"),
            Dindor = Dec(z, "DINDOR"),
            Dinsl = Dec(z, "DINSL"),
            Dinpraz = Dec(z, "DINPRAZ"),
            Dinbol = Dec(z, "DINBOL"),
            Dinbol2 = Dec(z, "DINBOL2"),
            Dinplac = Dec(z, "DINPLAC"),
            Dinplac2 = Dec(z, "DINPLAC2"),
            Dingod = Dec(z, "DINGOD"),
            Dinvv = Dec(z, "DINVV"),
            Din1 = Dec(z, "DIN1"),
            Din2 = Dec(z, "DIN2"),
            Din3 = Dec(z, "DIN3"),
            Dinsus = Dec(z, "DINSUS"),
            Dinmin = Dec(z, "DINMIN"),
            Dinuk = Dec(z, "DINUK"),
            Dinpriprav = Dec(z, "DINPRIPRAV"),
            Stim1 = Dec(z, "STIM1"),
            Stim2 = Dec(z, "STIM2"),
            Stim3 = Dec(z, "STIM3"),
            Stim1proc = Dec(z, "STIM1PROC"),
            Stim2proc = Dec(z, "STIM2PROC"),
            Stim3proc = Dec(z, "STIM3PROC"),
            Topli = Dec(z, "TOPLI"),
            Regres = Dec(z, "REGRES"),
            Terenski = Dec(z, "TERENSKI"),
            Fiksna = Dec(z, "FIKSNA"),
            Dotacija = Dec(z, "DOTACIJA"),
            Ldodaci = Dec(z, "LDODACI"),
            Naknade = Dec(z, "NAKNADE"),
            Bruto = Dec(z, "BRUTO"),
            Neto = Dec(z, "NETO"),
            Neto2 = Dec(z, "NETO2"),
            Netosve = Dec(z, "NETOSVE"),
            Netoprev = Dec(z, "NETOPREV"),
            Netoost = Dec(z, "NETOOST"),
            Cenarada = Dec(z, "CENARADA"),
            Startbod = Dec(z, "STARTBOD"),
            Dopsocr = Dec(z, "DOPSOCR"),
            Dopsocf = Dec(z, "DOPSOCF"),
            Doppr = Dec(z, "DOPPR"),
            Dopzr = Dec(z, "DOPZR"),
            Dopnr = Dec(z, "DOPNR"),
            Doppf = Dec(z, "DOPPF"),
            Dopzf = Dec(z, "DOPZF"),
            Dopnf = Dec(z, "DOPNF"),
            Doppru = Dec(z, "DOPPRU"),
            Doppfu = Dec(z, "DOPPFU"),
            Dopzfu = Dec(z, "DOPZFU"),
            Dopnfu = Dec(z, "DOPNFU"),
            Doposlob = Dec(z, "DOPOSLOB"),
            Dopumanj = Dec(z, "DOPUMANJ"),
            Pioumanjr = Dec(z, "PIOUMANJR"),
            Pioumanjf = Dec(z, "PIOUMANJF"),
            Porez = Dec(z, "POREZ"),
            Porezs = Dec(z, "POREZS"),
            Porezu = Dec(z, "POREZU"),
            Poroslob = Dec(z, "POROSLOB"),
            Porumanj = Dec(z, "PORUMANJ"),
            Krediti = Dec(z, "KREDITI"),
            Kreditia = Dec(z, "KREDITIA"),
            Akontac = Dec(z, "AKONTAC"),
            Prevoz = Dec(z, "PREVOZ"),
            Kasa = Dec(z, "KASA"),
            Kasarata = Dec(z, "KASARATA"),
            Samodopr = Dec(z, "SAMODOPR"),
            Sindikat1 = Dec(z, "SINDIKAT1"),
            Sindikat2 = Dec(z, "SINDIKAT2"),
            Solidarn = Dec(z, "SOLIDARN"),
            Aliment = Dec(z, "ALIMENT"),
            Obust1 = Dec(z, "OBUST1"),
            Obust2 = Dec(z, "OBUST2"),
            Obust3 = Dec(z, "OBUST3"),
            Obust4 = Dec(z, "OBUST4"),
            Solpor = Dec(z, "SOLPOR"),
            Ukobust = Dec(z, "UKOBUST"),
            Zaisplatu = Dec(z, "ZAISPLATU"),
            Benproc = Dec(z, "BENPROC"),
            Bendin = Dec(z, "BENDIN"),
            Komorajd = Dec(z, "KOMORAJD"),
            Komorasd = Dec(z, "KOMORASD"),
            Komorard = Dec(z, "KOMORARD"),
            Bkumanj = Dec(z, "BKUMANJ"),
            Arhiva = Str(z, "ARHIVA"),
            Arhiva2 = Str(z, "ARHIVA2"),
        };
    }

    public static string? PronadjiDbf(string folderPath, string fileName)
    {
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

    private static string Str(Dictionary<string, object?> z, string k)
        => z.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;

    private static int Int(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0;
        if (v is decimal d) return (int)d;
        if (v is int i) return i;
        if (int.TryParse(v?.ToString(), out var p)) return p;
        return 0;
    }

    private static decimal Dec(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0m;
        if (v is decimal d) return d;
        if (decimal.TryParse(v?.ToString(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var p)) return p;
        return 0m;
    }
}
