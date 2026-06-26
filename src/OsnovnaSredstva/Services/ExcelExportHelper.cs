using ClosedXML.Excel;
using OsnovnaSredstva.Models;

namespace OsnovnaSredstva.Services;

public static class ExcelExportHelper
{
    private static readonly (string Hdr, bool IsBroj)[] Kolone =
    [
        ("Šifra",       false),
        ("Naziv",       false),
        ("Dat.nabavke", false),
        ("Br.naloga",   false),
        ("Konto",       false),
        ("Vrsta",       false),
        ("AG",          false),
        ("AgPod",       false),
        ("Inv.broj",    false),
        ("Mesto",       false),
        ("Nab.vr.",     true),
        ("Isp.vr.",     true),
        ("Sad.vr.",     true),
        ("Kom.",        true),
        ("Cena",        true),
        ("Stopa ot.",   true),
        ("OsnovKor",    false),
        ("Izvor",       false),
        ("Preneto",     false),
    ];

    public static void SnimiKartice(string putanja, IReadOnlyList<OsKartica> kartice, string listNaziv)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(listNaziv.Length > 31 ? listNaziv[..31] : listNaziv);

        // Zaglavlje
        for (int i = 0; i < Kolone.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = Kolone[i].Hdr;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Podaci
        for (int r = 0; r < kartice.Count; r++)
        {
            var k   = kartice[r];
            var row = r + 2;

            ws.Cell(row, 1).Value  = k.Osifra;
            ws.Cell(row, 2).Value  = k.Naz;
            ws.Cell(row, 3).Value  = k.DatNab.HasValue ? k.DatNab.Value.ToString("dd.MM.yyyy") : "";
            ws.Cell(row, 4).Value  = k.BrNal;
            ws.Cell(row, 5).Value  = k.Konto;
            ws.Cell(row, 6).Value  = k.Vrsta;
            ws.Cell(row, 7).Value  = k.Ag;
            ws.Cell(row, 8).Value  = k.AgPod;
            ws.Cell(row, 9).Value  = k.InvBroj;
            ws.Cell(row, 10).Value = k.Mesto;
            ws.Cell(row, 11).Value = k.Nab0;
            ws.Cell(row, 12).Value = k.Isp0;
            ws.Cell(row, 13).Value = k.Sad0;
            ws.Cell(row, 14).Value = k.Kom;
            ws.Cell(row, 15).Value = k.Cena;
            ws.Cell(row, 16).Value = k.StopaOt;
            ws.Cell(row, 17).Value = k.OsnovKor;
            ws.Cell(row, 18).Value = k.Izvor;
            ws.Cell(row, 19).Value = k.Preneto;

            // Decimalni format za brojeve
            for (int c = 11; c <= 15; c++)
                ws.Cell(row, c).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 16).Style.NumberFormat.Format = "#,##0.000";

            // Naizmjenična boja redova
            if (r % 2 == 1)
            {
                for (int c = 1; c <= Kolone.Length; c++)
                    ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#F4F7FC");
            }
        }

        // Ukupno red
        if (kartice.Count > 0)
        {
            var ukRed = kartice.Count + 2;
            ws.Cell(ukRed, 1).Value = "UKUPNO";
            ws.Cell(ukRed, 1).Style.Font.Bold = true;

            int[] decKolone = [11, 12, 13, 14, 15];
            foreach (var c in decKolone)
            {
                var cell = ws.Cell(ukRed, c);
                cell.FormulaA1 = $"=SUM({ws.Cell(2, c).Address}:{ws.Cell(ukRed - 1, c).Address})";
                cell.Style.Font.Bold = true;
                cell.Style.NumberFormat.Format = "#,##0.00";
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D5E3F2");
            }
        }

        ws.Columns().AdjustToContents(1, kartice.Count + 2);

        // Zamrzni zaglavlje
        ws.SheetView.FreezeRows(1);

        wb.SaveAs(putanja);
    }
}
