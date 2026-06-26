using System.Text;
using System.IO;

namespace Algoritam.WPF.Utilities;

public static class SimplePdfWriter
{
    private const double PageWidth = 595d;
    private const double PageHeight = 842d;
    private const double MarginLeft = 36d;
    private const double MarginTop = 806d;
    private const double MarginBottom = 36d;
    private const double LineHeight = 13d;
    private const int FontSize = 9;

    // Courier monospace: pri 9pt, 1 karakter ≈ 5.4pt → oko 96 karaktera po liniji
    public const int LineWidth = 90;

    public static void WriteTextPdf(string filePath, string title, IReadOnlyList<string> lines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var folder = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(folder))
            Directory.CreateDirectory(folder);

        var allLines = new List<string>();
        if (!string.IsNullOrWhiteSpace(title))
        {
            allLines.Add(title.Trim());
            allLines.Add(string.Empty);
        }

        allLines.AddRange(lines);
        if (allLines.Count == 0)
            allLines.Add("-");

        var rowsPerPage = Math.Max(1, (int)Math.Floor((MarginTop - MarginBottom) / LineHeight));
        var pages = SplitToPages(allLines, rowsPerPage);

        var pageCount = pages.Count;
        var fontObjectNumber = 3 + (pageCount * 2);
        var maxObjectNumber = fontObjectNumber;

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        var offsets = new long[maxObjectNumber + 1];

        WriteAscii(stream, "%PDF-1.4\n");
        WriteAscii(stream, "%SimplePdfWriter\n");

        for (var objectNumber = 1; objectNumber <= maxObjectNumber; objectNumber++)
        {
            offsets[objectNumber] = stream.Position;
            WriteAscii(stream, $"{objectNumber} 0 obj\n");
            WriteAscii(stream, BuildObject(objectNumber, pages, fontObjectNumber));
            WriteAscii(stream, "\nendobj\n");
        }

        var xrefPosition = stream.Position;
        WriteAscii(stream, $"xref\n0 {maxObjectNumber + 1}\n");
        WriteAscii(stream, "0000000000 65535 f \n");

        for (var objectNumber = 1; objectNumber <= maxObjectNumber; objectNumber++)
        {
            WriteAscii(stream, $"{offsets[objectNumber]:0000000000} 00000 n \n");
        }

        WriteAscii(stream, "trailer\n");
        WriteAscii(stream, $"<< /Size {maxObjectNumber + 1} /Root 1 0 R >>\n");
        WriteAscii(stream, "startxref\n");
        WriteAscii(stream, $"{xrefPosition}\n");
        WriteAscii(stream, "%%EOF");
    }

    private static string BuildObject(int objectNumber, IReadOnlyList<IReadOnlyList<string>> pages, int fontObjectNumber)
    {
        if (objectNumber == 1)
            return "<< /Type /Catalog /Pages 2 0 R >>";

        if (objectNumber == 2)
        {
            var kids = new StringBuilder();
            for (var index = 0; index < pages.Count; index++)
            {
                var pageObjectNumber = 3 + (index * 2);
                if (kids.Length > 0)
                    kids.Append(' ');

                kids.Append(pageObjectNumber).Append(" 0 R");
            }

            return $"<< /Type /Pages /Count {pages.Count} /Kids [{kids}] >>";
        }

        var pageIndex = (objectNumber - 3) / 2;
        var isPageObject = (objectNumber - 3) % 2 == 0;

        if (pageIndex >= 0 && pageIndex < pages.Count)
        {
            var pageObjectNumber = 3 + (pageIndex * 2);
            var contentObjectNumber = pageObjectNumber + 1;

            if (isPageObject)
            {
                return $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth:0} {PageHeight:0}] /Resources << /Font << /F1 {fontObjectNumber} 0 R >> >> /Contents {contentObjectNumber} 0 R >>";
            }

            var streamBody = BuildContentStream(pages[pageIndex]);
            var length = Encoding.ASCII.GetByteCount(streamBody);
            return $"<< /Length {length} >>\nstream\n{streamBody}\nendstream";
        }

        if (objectNumber == fontObjectNumber)
            return "<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>";

        throw new InvalidOperationException($"Neocekivan PDF objekat: {objectNumber}");
    }

    private static string BuildContentStream(IReadOnlyList<string> lines)
    {
        var sb = new StringBuilder();
        sb.Append("BT\n");
        sb.Append($"/F1 {FontSize} Tf\n");
        sb.Append($"{LineHeight:0.##} TL\n");
        sb.Append($"{MarginLeft:0.##} {MarginTop:0.##} Td\n");

        for (var i = 0; i < lines.Count; i++)
        {
            var text = EscapePdfText(lines[i]);
            sb.Append('(').Append(text).Append(") Tj\n");
            if (i < lines.Count - 1)
                sb.Append("T*\n");
        }

        sb.Append("ET");
        return sb.ToString();
    }

    private static IReadOnlyList<IReadOnlyList<string>> SplitToPages(IReadOnlyList<string> lines, int rowsPerPage)
    {
        var pages = new List<IReadOnlyList<string>>();

        for (var index = 0; index < lines.Count; index += rowsPerPage)
        {
            var count = Math.Min(rowsPerPage, lines.Count - index);
            var chunk = new List<string>(count);
            for (var offset = 0; offset < count; offset++)
                chunk.Add(lines[index + offset]);

            pages.Add(chunk);
        }

        if (pages.Count == 0)
            pages.Add(new List<string> { "-" });

        return pages;
    }

    private static string EscapePdfText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var sb = new StringBuilder(text.Length + 8);
        foreach (var c in text)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '(':  sb.Append("\\(");  break;
                case ')':  sb.Append("\\)");  break;
                case '\r': case '\n': sb.Append(' '); break;
                // Srpska slova — transliteracija jer Helvetica nema te gliphe
                case 'đ': sb.Append("dj"); break;
                case 'Đ': sb.Append("Dj"); break;
                case 'č': sb.Append("c");  break;
                case 'Č': sb.Append("C");  break;
                case 'š': sb.Append("s");  break;
                case 'Š': sb.Append("S");  break;
                case 'ž': sb.Append("z");  break;
                case 'Ž': sb.Append("Z");  break;
                case 'ć': sb.Append("c");  break;
                case 'Ć': sb.Append("C");  break;
                default:
                    if (c >= 32 && c <= 126)
                        sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    private static void WriteAscii(Stream stream, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }
}
