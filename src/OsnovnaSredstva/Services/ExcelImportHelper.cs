using ClosedXML.Excel;
using OsnovnaSredstva.Models;
using System.Globalization;

namespace OsnovnaSredstva.Services;

public static class ExcelImportHelper
{
    // Mapiranje poznatih zaglavlja na nazive polja (case-insensitive)
    private static readonly Dictionary<string, string> HeaderMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["šifra"]        = "OSIFRA", ["sifra"]         = "OSIFRA", ["osifra"]      = "OSIFRA",
        ["naziv"]        = "NAZ",    ["naz"]            = "NAZ",
        ["dat.nabavke"]  = "DATNAB", ["datnab"]         = "DATNAB", ["datum nabavke"] = "DATNAB",
        ["br.naloga"]    = "BRNAL",  ["brnal"]          = "BRNAL",  ["br naloga"]     = "BRNAL",
        ["konto"]        = "KONTO",
        ["vrsta"]        = "VRSTA",
        ["ag"]           = "AG",
        ["agpod"]        = "AGPOD",
        ["inv.broj"]     = "INVBROJ", ["invbroj"]       = "INVBROJ", ["inv broj"]     = "INVBROJ",
        ["mesto"]        = "MESTO",
        ["nab.vr."]      = "NAB0",   ["nab0"]           = "NAB0",   ["nabavna vr."]   = "NAB0",
        ["isp.vr."]      = "ISP0",   ["isp0"]           = "ISP0",   ["isp vr."]       = "ISP0",
        ["sad.vr."]      = "SAD0",   ["sad0"]           = "SAD0",   ["sad vr."]       = "SAD0",
        ["kom."]         = "KOM",    ["kom"]            = "KOM",
        ["cena"]         = "CENA",
        ["stopa ot."]    = "STOPAOT", ["stopaot"]       = "STOPAOT", ["stopa"]        = "STOPAOT",
        ["osnovkor"]     = "OSNOVKOR", ["osnov.kor."]   = "OSNOVKOR",
        ["izvor"]        = "IZVOR",
        ["preneto"]      = "PRENETO",
    };

    public readonly record struct ImportRezultat(
        List<OsKartica> Kartice,
        List<string>    Greske);

    public static ImportRezultat CitajKartice(string putanja)
    {
        var kartice = new List<OsKartica>();
        var greske  = new List<string>();

        using var wb = new XLWorkbook(putanja);
        var ws = wb.Worksheets.First();

        // Čitam zaglavlja iz prvog reda
        var headerRow = ws.Row(1);
        var kolMap = new Dictionary<int, string>(); // kolona → ime polja

        foreach (var cell in headerRow.CellsUsed())
        {
            var tekst = cell.GetString().Trim();
            if (HeaderMap.TryGetValue(tekst, out var poljeIme))
                kolMap[cell.Address.ColumnNumber] = poljeIme;
        }

        if (!kolMap.ContainsValue("OSIFRA"))
        {
            greske.Add("Nije pronađena kolona Šifra (OSIFRA) u zaglavlju fajla.");
            return new ImportRezultat(kartice, greske);
        }

        // Čitam podatke počev od reda 2
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int r = 2; r <= lastRow; r++)
        {
            var row = ws.Row(r);

            // Preskačem redove gdje je šifra prazna ili "UKUPNO"
            var sifraKol = kolMap.FirstOrDefault(kv => kv.Value == "OSIFRA").Key;
            var sifra = sifraKol > 0 ? row.Cell(sifraKol).GetString().Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(sifra) ||
                sifra.Equals("UKUPNO", StringComparison.OrdinalIgnoreCase)) continue;

            var k = new OsKartica { Osifra = sifra };
            var redGreske = new List<string>();

            foreach (var (kolBr, polje) in kolMap)
            {
                if (polje == "OSIFRA") continue;
                var cell = row.Cell(kolBr);

                try
                {
                    switch (polje)
                    {
                        case "NAZ":      k.Naz      = cell.GetString().Trim(); break;
                        case "BRNAL":    k.BrNal    = cell.GetString().Trim(); break;
                        case "KONTO":    k.Konto    = cell.GetString().Trim(); break;
                        case "VRSTA":    k.Vrsta    = cell.GetString().Trim(); break;
                        case "AG":       k.Ag       = cell.GetString().Trim(); break;
                        case "AGPOD":    k.AgPod    = cell.GetString().Trim(); break;
                        case "INVBROJ":  k.InvBroj  = cell.GetString().Trim(); break;
                        case "MESTO":    k.Mesto    = cell.GetString().Trim(); break;
                        case "OSNOVKOR": k.OsnovKor = cell.GetString().Trim(); break;
                        case "IZVOR":    k.Izvor    = cell.GetString().Trim(); break;
                        case "PRENETO":  k.Preneto  = cell.GetString().Trim(); break;

                        case "DATNAB":
                            k.DatNab = ParseDatum(cell);
                            break;

                        case "NAB0":    k.Nab0    = ParseDecimal(cell); break;
                        case "ISP0":    k.Isp0    = ParseDecimal(cell); break;
                        case "SAD0":    k.Sad0    = ParseDecimal(cell); break;
                        case "KOM":     k.Kom     = ParseDecimal(cell); break;
                        case "CENA":    k.Cena    = ParseDecimal(cell); break;
                        case "STOPAOT": k.StopaOt = ParseDecimal(cell); break;
                    }
                }
                catch
                {
                    redGreske.Add($"Red {r}, kolona {polje}: greška pri čitanju.");
                }
            }

            if (redGreske.Count > 0)
                greske.AddRange(redGreske);

            kartice.Add(k);
        }

        return new ImportRezultat(kartice, greske);
    }

    private static DateTime? ParseDatum(IXLCell cell)
    {
        if (cell.DataType == XLDataType.DateTime) return cell.GetDateTime();
        var s = cell.GetString().Trim();
        if (string.IsNullOrWhiteSpace(s)) return null;

        if (DateTime.TryParseExact(s, ["dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd"],
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        if (DateTime.TryParse(s, out var dt2)) return dt2;
        return null;
    }

    private static decimal ParseDecimal(IXLCell cell)
    {
        if (cell.DataType is XLDataType.Number) return (decimal)cell.GetDouble();
        var s = cell.GetString().Trim().Replace(".", "").Replace(",", ".");
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;
    }
}
