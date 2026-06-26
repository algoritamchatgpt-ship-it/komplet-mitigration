using OsnovnaSredstva.Utilities;
using OsnovnaSredstva.Views;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace OsnovnaSredstva.Services;

public static class OsStampacHelper
{
    public readonly record struct KolDef(string Hdr, double W, bool Desno = true);

    // A4 u DIPs (96 DPI): 210mm × 297mm
    private const double A4W = 793.7;
    private const double A4H = 1122.5;

    public static void Stampaj(
        string naslov,
        IReadOnlyList<KolDef> kolone,
        IReadOnlyList<string[]> redovi,
        string[]? ukupno = null,
        bool landscape = false,
        Action<string>? onGotov = null)
    {
        double pageW = landscape ? A4H : A4W;
        double pageH = landscape ? A4W : A4H;
        var doc = GradiDoc(naslov, kolone, redovi, ukupno, pageW, pageH);

        byte[]? pdfBytes = null;
        try { pdfBytes = GenerisPdfBytes(naslov, kolone, redovi, ukupno); } catch { }

        var pdfNaziv = NaslovUFilename(naslov);
        new PrintPreviewWindow(doc, pdfBytes, pdfNaziv, naslov) { Owner = Application.Current.MainWindow }.ShowDialog();
        onGotov?.Invoke($"Pregled zatvoren — {redovi.Count} redova.");
    }

    public static byte[] GenerisPdfBytes(
        string naslov,
        IReadOnlyList<KolDef> kolone,
        IReadOnlyList<string[]> redovi,
        string[]? ukupno = null)
    {
        var sirine = new int[kolone.Count];
        for (int i = 0; i < kolone.Count; i++)
        {
            var maxData = redovi.Count > 0
                ? redovi.Max(r => i < r.Length ? (r[i] ?? "").Length : 0)
                : 0;
            if (ukupno != null && i < ukupno.Length)
                maxData = Math.Max(maxData, (ukupno[i] ?? "").Length);
            sirine[i] = Math.Max(kolone[i].Hdr.Length, Math.Min(maxData, 30));
        }

        const int maxLineWidth = 85;
        var grupe = new List<List<int>>();
        var tekucaGrupa = new List<int>();
        int tekucaSirina = 0;

        for (int i = 0; i < kolone.Count; i++)
        {
            int potrebno = sirine[i] + 1;
            if (tekucaGrupa.Count > 0 && tekucaSirina + potrebno > maxLineWidth)
            {
                grupe.Add(tekucaGrupa);
                tekucaGrupa = [];
                tekucaSirina = 0;
            }
            tekucaGrupa.Add(i);
            tekucaSirina += potrebno;
        }
        if (tekucaGrupa.Count > 0) grupe.Add(tekucaGrupa);

        var lines = new List<string>();
        for (int g = 0; g < grupe.Count; g++)
        {
            var grupa = grupe[g];
            if (g > 0) { lines.Add(string.Empty); lines.Add($"--- kolone {g + 1}/{grupe.Count} ---"); lines.Add(string.Empty); }

            var header = string.Concat(grupa.Select(ci => kolone[ci].Hdr.PadRight(sirine[ci] + 1))).TrimEnd();
            lines.Add(header);
            lines.Add(new string('-', Math.Min(header.Length, maxLineWidth)));

            foreach (var red in redovi)
            {
                var line = string.Concat(grupa.Select(ci =>
                {
                    var val = ci < red.Length ? (red[ci] ?? "") : "";
                    if (val.Length > sirine[ci]) val = val[..sirine[ci]];
                    return val.PadRight(sirine[ci] + 1);
                })).TrimEnd();
                lines.Add(line);
            }

            if (ukupno != null)
            {
                lines.Add(new string('-', Math.Min(header.Length, maxLineWidth)));
                var ukLine = string.Concat(grupa.Select(ci =>
                {
                    var val = ci < ukupno.Length ? (ukupno[ci] ?? "") : "";
                    return val.PadRight(sirine[ci] + 1);
                })).TrimEnd();
                lines.Add(ukLine);
            }
        }

        return SimplePdfWriter.GenerisPdfBytes(naslov, lines);
    }

    private static string NaslovUFilename(string naslov)
    {
        var safe = string.Concat(naslov.Select(c =>
            char.IsLetterOrDigit(c) || c == '-' ? c : '_')).Trim('_');
        return (safe.Length > 0 ? safe : "izvestaj") + ".pdf";
    }

    private static FlowDocument GradiDoc(
        string naslov,
        IReadOnlyList<KolDef> kolone,
        IReadOnlyList<string[]> redovi,
        string[]? ukupno,
        double pageW, double pageH)
    {
        var doc = new FlowDocument
        {
            PageWidth   = pageW,
            PageHeight  = pageH,
            ColumnWidth = pageW,
            FontFamily  = new FontFamily("Courier New"),
            FontSize    = 8,
            PagePadding = new Thickness(20, 16, 20, 16)
        };

        // Zaglavlje: naslov levo, datum desno — dva paragrafa (Star width ne radi u FlowDocument tabeli)
        doc.Blocks.Add(new Paragraph(new Run(naslov))
        {
            FontSize = 11, FontWeight = FontWeights.Bold,
            Margin = new Thickness(0), Padding = new Thickness(0, 0, 0, 2)
        });
        doc.Blocks.Add(new Paragraph(new Run($"Datum: {DateTime.Now:dd.MM.yyyy}"))
        {
            FontSize = 8, TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 0, 0, 5), Padding = new Thickness(0, 0, 0, 3),
            BorderBrush = Brushes.DimGray, BorderThickness = new Thickness(0, 0, 0, 1)
        });

        // Tabela podataka
        var tbl = new Table { CellSpacing = 0 };
        foreach (var k in kolone)
            tbl.Columns.Add(new TableColumn { Width = new GridLength(k.W) });

        var rg = new TableRowGroup();
        tbl.RowGroups.Add(rg);

        rg.Rows.Add(GradiRed(kolone.Select(k => k.Hdr).ToArray(), kolone, isHeader: true));

        var altBrush = new SolidColorBrush(Color.FromRgb(244, 247, 252));
        for (int i = 0; i < redovi.Count; i++)
        {
            var r = GradiRed(redovi[i], kolone);
            if (i % 2 == 1) r.Background = altBrush;
            rg.Rows.Add(r);
        }

        if (ukupno != null)
            rg.Rows.Add(GradiRed(ukupno, kolone, isUkupno: true));

        doc.Blocks.Add(tbl);
        return doc;
    }

    private static TableRow GradiRed(
        string[] celije,
        IReadOnlyList<KolDef> kolone,
        bool isHeader = false,
        bool isUkupno = false)
    {
        var row = new TableRow();
        if (isHeader || isUkupno)
            row.Background = new SolidColorBrush(Color.FromRgb(213, 227, 242));

        for (int i = 0; i < kolone.Count; i++)
        {
            var txt = i < celije.Length ? celije[i] : "";
            var vPad = isHeader || isUkupno ? 2.0 : 1.0;
            var para = new Paragraph(new Run(txt))
            {
                Padding       = new Thickness(3, vPad, 3, vPad),
                TextAlignment = kolone[i].Desno ? TextAlignment.Right : TextAlignment.Left,
                FontWeight    = isHeader || isUkupno ? FontWeights.Bold : FontWeights.Normal
            };
            var cell = new TableCell(para)
            {
                BorderBrush     = Brushes.Gray,
                BorderThickness = isHeader  ? new Thickness(0, 0, 0, 1)
                                : isUkupno ? new Thickness(0, 1, 0, 0)
                                :             new Thickness(0)
            };
            row.Cells.Add(cell);
        }
        return row;
    }
}
