using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using OsnovnaSredstva.Views;

namespace OsnovnaSredstva.ViewModels;

internal static class OsPopisFilterSupport
{
    public static OsPopisFilterData Ucitaj(AppState appState)
    {
        var path = DbfHelper.NadjiDbf(appState, "ospopis.dbf");
        if (path == null)
            return new OsPopisFilterData();

        try
        {
            foreach (var r in new SimpleDbfReader(path).Zapisi())
            {
                var mtr = r.DajInt("MTR");
                return new OsPopisFilterData
                {
                    Mesto = r.DajString("MESTO").Trim(),
                    Mtr = mtr < 1 ? 0 : mtr,
                    Konto = r.DajString("KONTO").Trim(),
                    Ag = r.DajString("AG").Trim(),
                    AgPod = r.DajString("AGPOD").Trim(),
                    Grupa = r.DajString("GRUPA").Trim(),
                };
            }
        }
        catch
        {
            // Pregled može da radi i bez zapamćenih kriterijuma.
        }

        return new OsPopisFilterData();
    }

    public static void Sacuvaj(AppState appState, OsPopisFilterData data)
    {
        var path = DbfHelper.NadjiDbf(appState, "ospopis.dbf");
        if (path == null)
            return;

        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            var reader = new SimpleDbfReader(path);
            var redovi = new List<Dictionary<string, object?>>();

            foreach (var r in reader.Zapisi())
            {
                var red = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var f in reader.Fields)
                {
                    red[f.Name] = f.Type switch
                    {
                        'D' => (object?)r.DajDate(f.Name),
                        'N' or 'F' => r.DajDecimal(f.Name),
                        'L' => r.DajBool(f.Name),
                        _ => r.DajString(f.Name),
                    };
                }
                redovi.Add(red);
            }

            if (redovi.Count == 0)
                redovi.Add(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase));

            var prvi = redovi[0];
            prvi["MESTO"] = data.Mesto.Trim();
            prvi["MTR"] = data.Mtr < 1 ? 0 : data.Mtr;
            prvi["KONTO"] = data.Konto.Trim();
            prvi["AG"] = data.Ag.Trim();
            prvi["AGPOD"] = data.AgPod.Trim();
            prvi["GRUPA"] = data.Grupa.Trim();

            DbfTableWriter.WriteTable(path, schema, redovi,
                (red, field) => red.TryGetValue(field, out var value) ? value : null);
        }
        catch
        {
            // Pamćenje kriterijuma nije uslov za prikaz.
        }
    }

    public static IReadOnlyList<OsKartica> Primeni(
        IEnumerable<OsKartica> kartice,
        OsPopisFilterData data)
    {
        var mesto = data.Mesto.Trim();
        var konto = data.Konto.Trim();
        var ag = data.Ag.Trim();
        var agPod = data.AgPod.Trim();
        var grupa = data.Grupa.Trim();
        var mtr = data.Mtr < 1 ? 0 : data.Mtr;

        return kartice.Where(k =>
            (mesto.Length == 0 ||
             string.Equals(k.Mesto?.Trim(), mesto, StringComparison.OrdinalIgnoreCase)) &&
            (mtr == 0 || (int)OsSaldoViewModel.DajDec(k, "MTR") == mtr) &&
            (ag.Length == 0 ||
             string.Equals(k.Ag?.Trim(), ag, StringComparison.OrdinalIgnoreCase)) &&
            (agPod.Length == 0 ||
             string.Equals(k.AgPod?.Trim(), agPod, StringComparison.OrdinalIgnoreCase)) &&
            (konto.Length == 0 ||
             (k.Konto ?? string.Empty).Trim().StartsWith(
                 konto, StringComparison.OrdinalIgnoreCase)) &&
            (grupa.Length == 0 ||
             string.Equals(DajTekst(k, "GRUPA"), grupa, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static string DajTekst(OsKartica kartica, string polje) =>
        kartica.ExtraPolja.TryGetValue(polje, out var value)
            ? Convert.ToString(value)?.Trim() ?? string.Empty
            : string.Empty;
}
