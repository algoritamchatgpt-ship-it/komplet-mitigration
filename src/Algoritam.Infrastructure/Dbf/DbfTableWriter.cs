using System.Globalization;
using System.Text;

namespace Algoritam.Infrastructure.Dbf;

public static class DbfTableWriter
{
    public sealed record DbfField(string Name, char Type, int Length, int Decimals, int Offset);

    public sealed class DbfSchema
    {
        public required string TemplatePath { get; init; }
        public required byte Version { get; init; }
        public required byte CodePageMark { get; init; }
        public required ushort HeaderLength { get; init; }
        public required ushort RecordLength { get; init; }
        public required byte[] HeaderBytes { get; init; }
        public required IReadOnlyList<DbfField> Fields { get; init; }
        public required Encoding Encoding { get; init; }
    }

    public static DbfSchema LoadSchema(string templatePath)
    {
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("DBF template nije pronadjen.", templatePath);

        using var fs = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var br = new BinaryReader(fs, Encoding.ASCII);

        var version = br.ReadByte();
        br.ReadBytes(3);
        br.ReadUInt32();
        var headerLen = br.ReadUInt16();
        var recordLen = br.ReadUInt16();
        br.ReadBytes(16);
        br.ReadByte();
        var codePage = br.ReadByte();
        br.ReadBytes(2);

        fs.Seek(0, SeekOrigin.Begin);
        var headerBytes = br.ReadBytes(headerLen);
        if (headerBytes.Length != headerLen)
            throw new InvalidDataException("Neispravan DBF header.");

        fs.Seek(32, SeekOrigin.Begin);
        var fields = new List<DbfField>();
        var offset = 1;
        while (true)
        {
            var first = br.ReadByte();
            if (first == 0x0D)
                break;

            var rest = br.ReadBytes(31);
            if (rest.Length < 31)
                throw new InvalidDataException("Neispravan opis DBF polja.");

            var nameBytes = new byte[11];
            nameBytes[0] = first;
            Array.Copy(rest, 0, nameBytes, 1, 10);
            var name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0').Trim();
            var type = (char)rest[10];
            // Field descriptor je 32 bajta; posto je prvi bajt vec procitan,
            // duzina i decimale su na rest[15] i rest[16].
            var length = rest[15];
            var decimals = rest[16];

            fields.Add(new DbfField(name, type, length, decimals, offset));
            offset += length;
        }

        return new DbfSchema
        {
            TemplatePath = templatePath,
            Version = version,
            CodePageMark = codePage,
            HeaderLength = headerLen,
            RecordLength = recordLen,
            HeaderBytes = headerBytes,
            Fields = fields,
            Encoding = MapEncoding(codePage)
        };
    }

    public static void WriteTable<T>(
        string targetPath,
        DbfSchema schema,
        IReadOnlyList<T> rows,
        Func<T, string, object?> valueResolver)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        var header = (byte[])schema.HeaderBytes.Clone();
        var now = DateTime.Now;
        header[1] = (byte)(now.Year - 1900);
        header[2] = (byte)now.Month;
        header[3] = (byte)now.Day;

        var countBytes = BitConverter.GetBytes((uint)rows.Count);
        Array.Copy(countBytes, 0, header, 4, 4);

        using var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        fs.Write(header, 0, header.Length);

        foreach (var row in rows)
        {
            var record = Enumerable.Repeat((byte)0x20, schema.RecordLength).ToArray();
            record[0] = 0x20;

            foreach (var field in schema.Fields)
            {
                var value = valueResolver(row, field.Name);
                var bytes = FormatField(field, value, schema.Encoding);
                Array.Copy(bytes, 0, record, field.Offset, Math.Min(bytes.Length, field.Length));
            }

            fs.Write(record, 0, record.Length);
        }

        fs.WriteByte(0x1A);
    }

    private static byte[] FormatField(DbfField field, object? value, Encoding enc)
    {
        var text = field.Type switch
        {
            'C' => FormatCharacter(value, field.Length),
            'N' => FormatNumeric(value, field.Length, field.Decimals),
            'F' => FormatNumeric(value, field.Length, field.Decimals),
            'D' => FormatDate(value),
            'L' => FormatLogical(value),
            _ => new string(' ', field.Length)
        };

        var bytes = enc.GetBytes(text);
        if (bytes.Length == field.Length)
            return bytes;

        if (bytes.Length < field.Length)
        {
            var padded = new byte[field.Length];
            Array.Fill(padded, (byte)0x20);
            Array.Copy(bytes, 0, padded, 0, bytes.Length);
            return padded;
        }

        var truncated = new byte[field.Length];
        Array.Copy(bytes, 0, truncated, 0, field.Length);
        return truncated;
    }

    private static string FormatCharacter(object? value, int len)
    {
        var text = Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
        if (text.Length > len)
            return text[..len];
        return text.PadRight(len, ' ');
    }

    private static string FormatNumeric(object? value, int len, int decimals)
    {
        if (value is null)
            return new string(' ', len);

        decimal d;
        try
        {
            d = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return new string(' ', len);
        }

        d = Math.Round(d, decimals, MidpointRounding.AwayFromZero);
        var format = decimals > 0 ? $"F{decimals}" : "F0";
        var text = d.ToString(format, CultureInfo.InvariantCulture);

        if (text.Length > len)
            return new string('*', len);

        return text.PadLeft(len, ' ');
    }

    private static string FormatDate(object? value)
    {
        if (value is DateTime dt)
            return dt.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        if (value is DateTimeOffset dto)
            return dto.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        if (value is string s && DateTime.TryParse(s, out var parsed))
            return parsed.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        return "        ";
    }

    private static string FormatLogical(object? value)
    {
        if (value is bool b)
            return b ? "T" : "F";

        if (value is string s)
        {
            var upper = s.Trim().ToUpperInvariant();
            if (upper is "T" or "Y" or ".T." or "1" or "D")
                return "T";
            if (upper is "F" or "N" or ".F." or "0")
                return "F";
        }

        return "?";
    }

    public static void DodajRedove(
        string dbfPath,
        DbfSchema schema,
        IReadOnlyList<Dictionary<string, object?>> rows)
    {
        if (rows.Count == 0) return;

        using var fs = new FileStream(dbfPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        fs.Seek(4, SeekOrigin.Begin);
        using var br = new BinaryReader(fs, schema.Encoding, leaveOpen: true);
        var existing = br.ReadUInt32();

        long dataEnd = schema.HeaderLength + (long)existing * schema.RecordLength;
        fs.Seek(dataEnd, SeekOrigin.Begin);

        foreach (var row in rows)
        {
            var record = Enumerable.Repeat((byte)0x20, schema.RecordLength).ToArray();
            record[0] = 0x20;

            foreach (var field in schema.Fields)
            {
                row.TryGetValue(field.Name, out var value);
                var bytes = FormatField(field, value, schema.Encoding);
                Array.Copy(bytes, 0, record, field.Offset, Math.Min(bytes.Length, field.Length));
            }
            fs.Write(record, 0, record.Length);
        }

        fs.WriteByte(0x1A);

        fs.Seek(4, SeekOrigin.Begin);
        using var bw = new BinaryWriter(fs, schema.Encoding, leaveOpen: true);
        bw.Write((uint)(existing + rows.Count));

        var now = DateTime.Now;
        fs.Seek(1, SeekOrigin.Begin);
        fs.WriteByte((byte)(now.Year - 1900));
        fs.WriteByte((byte)now.Month);
        fs.WriteByte((byte)now.Day);
    }

    public static void OznaciBrisanjePoIndeksima(string dbfPath, IEnumerable<int> indeksi)
    {
        var lista = indeksi.ToList();
        if (lista.Count == 0) return;

        using var fs = new FileStream(dbfPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        fs.Seek(8, SeekOrigin.Begin);
        using var br = new BinaryReader(fs, Encoding.ASCII, leaveOpen: true);
        var headerLen = br.ReadUInt16();
        var recordLen = br.ReadUInt16();

        foreach (var idx in lista)
        {
            fs.Seek(headerLen + (long)idx * recordLen, SeekOrigin.Begin);
            fs.WriteByte(0x2A);
        }

        var now = DateTime.Now;
        fs.Seek(1, SeekOrigin.Begin);
        fs.WriteByte((byte)(now.Year - 1900));
        fs.WriteByte((byte)now.Month);
        fs.WriteByte((byte)now.Day);
    }

    public static DbfSchema EnsureFieldExists(
        string dbfPath,
        string fieldName,
        char fieldType = 'C',
        int fieldLength = 60,
        int fieldDecimals = 0)
    {
        var schema = LoadSchema(dbfPath);

        if (schema.Fields.Any(f => string.Equals(f.Name, fieldName, StringComparison.OrdinalIgnoreCase)))
            return schema;

        var zapisi = DbfReader.CitajSveZapise(dbfPath);
        var noviHeader = IzgradiProsiriHeader(schema, fieldName, fieldType, fieldLength, fieldDecimals);
        var noviRecordLength = (ushort)(schema.RecordLength + fieldLength);

        var tempPath = dbfPath + ".emltmp";
        try
        {
            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var h = (byte[])noviHeader.Clone();
                var now = DateTime.Now;
                h[1] = (byte)(now.Year - 1900);
                h[2] = (byte)now.Month;
                h[3] = (byte)now.Day;
                var cntBytes = BitConverter.GetBytes((uint)zapisi.Count);
                Array.Copy(cntBytes, 0, h, 4, 4);
                fs.Write(h, 0, h.Length);

                foreach (var zapis in zapisi)
                {
                    var rec = new byte[noviRecordLength];
                    Array.Fill(rec, (byte)0x20);
                    rec[0] = 0x20;

                    foreach (var field in schema.Fields)
                    {
                        zapis.TryGetValue(field.Name, out var val);
                        var bytes = FormatField(field, val, schema.Encoding);
                        Array.Copy(bytes, 0, rec, field.Offset, Math.Min(bytes.Length, field.Length));
                    }

                    fs.Write(rec, 0, rec.Length);
                }
                fs.WriteByte(0x1A);
            }

            File.Move(tempPath, dbfPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
                try { File.Delete(tempPath); } catch { }
            throw;
        }

        return LoadSchema(dbfPath);
    }

    private static byte[] IzgradiProsiriHeader(
        DbfSchema schema, string fieldName, char fieldType, int fieldLength, int fieldDecimals)
    {
        var stariHeader = schema.HeaderBytes;

        var terminatorPos = stariHeader.Length;
        for (int i = 32; i < stariHeader.Length; i++)
        {
            if (stariHeader[i] == 0x0D)
            {
                terminatorPos = i;
                break;
            }
        }

        var fieldDesc = new byte[32];
        var nameBytes = Encoding.ASCII.GetBytes(fieldName.ToUpperInvariant());
        Array.Copy(nameBytes, 0, fieldDesc, 0, Math.Min(nameBytes.Length, 11));
        fieldDesc[11] = (byte)fieldType;
        fieldDesc[16] = (byte)fieldLength;
        fieldDesc[17] = (byte)fieldDecimals;

        var noviHeaderLength = (ushort)(schema.HeaderLength + 32);
        var noviHeader = new byte[noviHeaderLength];

        int kopirati = Math.Min(terminatorPos, stariHeader.Length);
        Array.Copy(stariHeader, 0, noviHeader, 0, kopirati);

        Array.Copy(fieldDesc, 0, noviHeader, terminatorPos, 32);

        if (terminatorPos + 32 < noviHeaderLength)
            noviHeader[terminatorPos + 32] = 0x0D;

        var hlBytes = BitConverter.GetBytes(noviHeaderLength);
        noviHeader[8] = hlBytes[0];
        noviHeader[9] = hlBytes[1];

        var newRecLen = (ushort)(schema.RecordLength + fieldLength);
        var rlBytes = BitConverter.GetBytes(newRecLen);
        noviHeader[10] = rlBytes[0];
        noviHeader[11] = rlBytes[1];

        return noviHeader;
    }

    private static Encoding MapEncoding(byte codePage) => codePage switch
    {
        0x01 => Encoding.GetEncoding(437),
        0x02 => Encoding.GetEncoding(850),
        0x03 => Encoding.GetEncoding(1252),
        0x64 => Encoding.GetEncoding(852),
        0x65 => Encoding.GetEncoding(866),
        0xC8 => Encoding.GetEncoding(1250),
        0xC9 => Encoding.GetEncoding(1251),
        0xCB => Encoding.GetEncoding(1253),
        _ => Encoding.GetEncoding(1250),
    };
}
