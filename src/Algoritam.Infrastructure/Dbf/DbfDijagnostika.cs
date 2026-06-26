using System.Text;

namespace Algoritam.Infrastructure.Dbf;

public static class DbfDijagnostika
{
    /// <summary>
    /// Čita header DBF fajla i vraća string sa svim informacijama —
    /// kod stranice, polja, i prve 3 zapisa.
    /// </summary>
    public static string AnalizirajFajl(string putanja)
    {
        if (!File.Exists(putanja))
            return $"GREŠKA: fajl ne postoji — {putanja}";

        var sb = new StringBuilder();

        using var stream = new FileStream(putanja, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new BinaryReader(stream);

        // Header
        byte verzija     = reader.ReadByte();
        byte[] datum     = reader.ReadBytes(3);
        uint  brZapisa   = reader.ReadUInt32();
        ushort hLen      = reader.ReadUInt16();
        ushort rLen      = reader.ReadUInt16();
        reader.ReadBytes(16); // rezervisano (bytes 12-27)
        reader.ReadByte();    // byte 28: table flags
        byte kodStranice = reader.ReadByte(); // byte 29: code page mark
        reader.ReadBytes(2);  // bytes 30-31: rezervisano

        sb.AppendLine($"Fajl: {putanja}");
        sb.AppendLine($"Verzija DBF: 0x{verzija:X2}");
        sb.AppendLine($"Broj zapisa: {brZapisa}");
        sb.AppendLine($"Kod stranice (byte 29): 0x{kodStranice:X2} → {MapirajEncoding(kodStranice).EncodingName}");
        sb.AppendLine();

        // Polja
        var polja = new List<(string Ime, char Tip, int Duz)>();
        sb.AppendLine("POLJA:");
        while (true)
        {
            byte prvi = reader.ReadByte();
            if (prvi == 0x0D) break;
            var imeB = new byte[11];
            imeB[0] = prvi;
            reader.Read(imeB, 1, 10);
            string ime = Encoding.ASCII.GetString(imeB).TrimEnd('\0');
            char tip   = (char)reader.ReadByte();
            reader.ReadBytes(4);
            int duz    = reader.ReadByte();
            reader.ReadBytes(15);
            polja.Add((ime, tip, duz));
            sb.AppendLine($"  {ime,-15} {tip}({duz})");
        }

        // Prve 3 zapisa
        var enc = MapirajEncoding(kodStranice);
        stream.Seek(hLen, SeekOrigin.Begin);
        sb.AppendLine();
        sb.AppendLine("PRVA 3 ZAPISA (neobrisan):");
        int prikazano = 0;
        while (prikazano < 3)
        {
            if (stream.Position >= stream.Length) break;
            byte flag = reader.ReadByte();
            var redak = new StringBuilder();
            foreach (var (ime, _, duz) in polja)
            {
                byte[] b = reader.ReadBytes(duz);
                string v = enc.GetString(b).Trim();
                if (!string.IsNullOrEmpty(v))
                    redak.Append($"{ime}={v}  ");
            }
            if (flag != '*') // nije obrisan
            {
                sb.AppendLine($"  [{prikazano+1}] {redak}");
                prikazano++;
            }
        }

        return sb.ToString();
    }

    private static Encoding MapirajEncoding(byte kod) => kod switch
    {
        0x01 => Encoding.GetEncoding(437),   // DOS USA
        0x02 => Encoding.GetEncoding(850),   // DOS Multilingual
        0x03 => Encoding.GetEncoding(1252),  // Windows ANSI
        0x64 => Encoding.GetEncoding(852),   // Eastern European DOS
        0x65 => Encoding.GetEncoding(866),   // Russian DOS
        0xC8 => Encoding.GetEncoding(1250),  // Windows Eastern European
        0xC9 => Encoding.GetEncoding(1251),  // Windows Russian
        0xCB => Encoding.GetEncoding(1253),  // Windows Greek
        _    => Encoding.GetEncoding(1250),  // default za srpski
    };
}
