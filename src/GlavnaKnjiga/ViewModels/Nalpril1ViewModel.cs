using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>
/// NALPRIL1 — PREUZIMANJE PRILIVA I ODLIVA.
/// Doslovni port nalpril1.prg: NALPRIL0 + AAAN određuju analitičku tabelu i stranu
/// duguje/potražuje, a otvorene stavke se grupišu po SIFRA+BRRAC.
/// </summary>
public partial class Nalpril1ViewModel : ObservableObject
{
    private readonly string _firmPath;

    public event Action? ZatvoriFormu;

    [ObservableProperty] private DateTime _dat0 = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    [ObservableProperty] private DateTime _dat1 = DateTime.Today;
    [ObservableProperty] private string _poruka = string.Empty;

    public Nalpril1ViewModel(string firmPath)
    {
        _firmPath = firmPath;
        UcitajDatume();
    }

    [RelayCommand]
    private void Preuzmi()
    {
        if (Dat0 > Dat1)
        {
            Poruka = "Početni datum ne može biti posle zadnjeg datuma.";
            return;
        }

        try
        {
            var dodato = PreuzmiIzAnalitike();
            Poruka = $"Preuzimanje završeno — dodato {dodato} automatskih redova.";
            MessageBox.Show(Poruka, "PREUZIMANJE PRILIVA I ODLIVA",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Poruka = $"Greška: {ex.Message}";
            MessageBox.Show(Poruka, "PREUZIMANJE PRILIVA I ODLIVA",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    private int PreuzmiIzAnalitike()
    {
        var nalprilPath = NadjiDbf("nalpril.dbf")
            ?? throw new FileNotFoundException("nalpril.dbf nije pronađen.");
        var nalpril0Path = NadjiDbf("nalpril0.dbf")
            ?? throw new FileNotFoundException("nalpril0.dbf nije pronađen.");
        var aaanPath = NadjiDbf("aaan.dbf")
            ?? throw new FileNotFoundException("aaan.dbf nije pronađen.");

        var podesavanja = new SimpleDbfReader(nalpril0Path).Zapisi()
            .Select(r => new NalprilKontoPodesavanje(
                r.DajString("KONTO"), r.DajString("DP")))
            .Where(r => !string.IsNullOrWhiteSpace(r.Konto))
            .ToList();

        var aaan = new SimpleDbfReader(aaanPath).Zapisi()
            .Select(r => new NalprilAaan(
                r.DajString("KONTO"),
                r.DajString("SIFPROD"),
                r.DajString("PNAZIV")))
            .Where(r => !string.IsNullOrWhiteSpace(r.Konto))
            .GroupBy(r => r.Konto.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var nazivi = UcitajNazivePartnera();
        var cache = new Dictionary<string, IReadOnlyList<NalprilAnalitikaStavka>>(
            StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<NalprilAnalitikaStavka> UcitajAnalitiku(string sifProd)
        {
            if (cache.TryGetValue(sifProd, out var postojece))
                return postojece;

            var path = NadjiDbf($"anal{sifProd.Trim()}.dbf");
            if (path == null)
                return cache[sifProd] = [];

            return cache[sifProd] = new SimpleDbfReader(path).Zapisi()
                .Select(r => new NalprilAnalitikaStavka(
                    r.DajString("SIFRA"),
                    r.DajString("BRRAC"),
                    r.DajDate("VALUTA"),
                    r.DajDecimal("DUG"),
                    r.DajDecimal("POT"),
                    r.DajString("ZATVAR")))
                .ToList();
        }

        var novi = FormirajAutomatskeRedove(
            podesavanja, aaan, UcitajAnalitiku, nazivi, Dat0.Date, Dat1.Date);

        var reader = new SimpleDbfReader(nalprilPath);
        var svi = reader.Zapisi().Select(NalprilViewModel.MapRecord).ToList();
        svi.AddRange(novi);

        var schema = DbfTableWriter.LoadSchema(nalprilPath);
        DbfTableWriter.WriteTable(nalprilPath, schema, svi, NalprilViewModel.FieldMapper);
        SacuvajDatume();
        return novi.Count;
    }

    internal static List<NalprilRow> FormirajAutomatskeRedove(
        IEnumerable<NalprilKontoPodesavanje> podesavanja,
        IReadOnlyDictionary<string, NalprilAaan> aaan,
        Func<string, IReadOnlyList<NalprilAnalitikaStavka>> ucitajAnalitiku,
        IReadOnlyDictionary<string, string> naziviPartnera,
        DateTime dat0,
        DateTime dat1)
    {
        var rezultat = new List<NalprilRow>();

        foreach (var podesavanje in podesavanja)
        {
            var konto = podesavanje.Konto.Trim();
            if (!aaan.TryGetValue(konto, out var info) ||
                string.IsNullOrWhiteSpace(info.SifProd))
                continue;

            var analitika = ucitajAnalitiku(info.SifProd).ToList();
            var duguje = podesavanje.Dp.Trim().Equals("D", StringComparison.OrdinalIgnoreCase);

            rezultat.AddRange(FormirajPeriod(
                analitika, konto, info.PNaziv, duguje, dat0, dat1,
                prePerioda: true, naziviPartnera));
            rezultat.AddRange(FormirajPeriod(
                analitika, konto, info.PNaziv, duguje, dat0, dat1,
                prePerioda: false, naziviPartnera));
        }

        return rezultat;
    }

    private static IEnumerable<NalprilRow> FormirajPeriod(
        IReadOnlyList<NalprilAnalitikaStavka> analitika,
        string konto,
        string opis,
        bool duguje,
        DateTime dat0,
        DateTime dat1,
        bool prePerioda,
        IReadOnlyDictionary<string, string> naziviPartnera)
    {
        var kandidati = analitika.Where(r =>
            string.IsNullOrWhiteSpace(r.Zatvar) &&
            r.Valuta.HasValue &&
            (prePerioda
                ? r.Valuta.Value.Date < dat0
                : r.Valuta.Value.Date >= dat0 && r.Valuta.Value.Date <= dat1) &&
            (duguje ? r.Dug != 0 : r.Pot != 0));

        foreach (var grupa in kandidati.GroupBy(Kljuc))
        {
            var prvi = grupa.First();
            var sviZaPartnera = analitika.Where(r => Kljuc(r) == grupa.Key);
            var primarno = duguje ? grupa.Sum(r => r.Dug) : grupa.Sum(r => r.Pot);
            var suprotno = duguje
                ? sviZaPartnera.Sum(r => r.Pot)
                : sviZaPartnera.Sum(r => r.Dug);
            var saldo = primarno - suprotno;
            var sifra = prvi.Sifra.Trim();

            var red = new NalprilRow
            {
                Konto = konto,
                Sifra = sifra,
                Brrac = prvi.BrRac.Trim(),
                Naziv = naziviPartnera.TryGetValue(sifra, out var naziv) ? naziv : string.Empty,
                Pauto = "*",
                Opis = opis.Trim(),
                Dat0 = dat0,
                Dat1 = dat1,
            };

            if (prePerioda)
            {
                if (duguje) red.Dugpre = saldo;
                else red.Potpre = saldo;
            }
            else
            {
                if (duguje) red.Dug = saldo;
                else red.Pot = saldo;
            }

            yield return red;
        }
    }

    private static string Kljuc(NalprilAnalitikaStavka r) =>
        r.Sifra.Trim().ToUpperInvariant() + "\u001F" + r.BrRac.Trim().ToUpperInvariant();

    private Dictionary<string, string> UcitajNazivePartnera()
    {
        var path = NadjiDbf("an0.dbf");
        if (path == null)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return new SimpleDbfReader(path).Zapisi()
            .Select(r => new { Sifra = r.DajString("SIFRA").Trim(), Naziv = r.DajString("NAZIV").Trim() })
            .Where(r => r.Sifra.Length > 0)
            .GroupBy(r => r.Sifra, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Naziv, StringComparer.OrdinalIgnoreCase);
    }

    private void UcitajDatume()
    {
        var path = NadjiDbf("datumi.dbf");
        if (path == null) return;

        try
        {
            var prvi = new SimpleDbfReader(path).Zapisi().FirstOrDefault();
            if (prvi == null) return;
            Dat0 = prvi.DajDate("DAT0") ?? Dat0;
            Dat1 = prvi.DajDate("DAT1") ?? Dat1;
        }
        catch { }
    }

    private void SacuvajDatume()
    {
        var path = NadjiDbf("datumi.dbf");
        if (path == null) return;

        var schema = DbfTableWriter.LoadSchema(path);
        var redovi = DbfRedovi.Ucitaj(path, schema);
        if (redovi.Count == 0) return;
        redovi[0]["DAT0"] = Dat0;
        redovi[0]["DAT1"] = Dat1;
        DbfRedovi.Snimi(path, schema, redovi);
    }

    private string? NadjiDbf(string ime)
    {
        foreach (var folder in new[] { _firmPath, Path.Combine(_firmPath, "data00") })
        {
            if (!Directory.Exists(folder)) continue;
            var direktno = Path.Combine(folder, ime);
            if (File.Exists(direktno)) return direktno;
            var bezObziraNaCase = Directory.EnumerateFiles(folder, "*.dbf", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(p => Path.GetFileName(p).Equals(ime, StringComparison.OrdinalIgnoreCase));
            if (bezObziraNaCase != null) return bezObziraNaCase;
        }
        return null;
    }
}

internal sealed record NalprilKontoPodesavanje(string Konto, string Dp);
internal sealed record NalprilAaan(string Konto, string SifProd, string PNaziv);
internal sealed record NalprilAnalitikaStavka(
    string Sifra,
    string BrRac,
    DateTime? Valuta,
    decimal Dug,
    decimal Pot,
    string Zatvar);

internal static class DbfRedovi
{
    internal static List<Dictionary<string, object?>> Ucitaj(
        string path, DbfTableWriter.DbfSchema schema)
    {
        var rezultat = new List<Dictionary<string, object?>>();
        foreach (var rec in new SimpleDbfReader(path, schema.Encoding).Zapisi())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in schema.Fields)
            {
                row[field.Name] = field.Type switch
                {
                    'D' => rec.DajDate(field.Name),
                    'N' or 'F' => rec.DajDecimal(field.Name),
                    'L' => rec.DajBool(field.Name),
                    _ => rec.DajString(field.Name),
                };
            }
            rezultat.Add(row);
        }
        return rezultat;
    }

    internal static void Snimi(
        string path,
        DbfTableWriter.DbfSchema schema,
        List<Dictionary<string, object?>> redovi) =>
        DbfTableWriter.WriteTable(path, schema, redovi,
            (row, field) => row.TryGetValue(field, out var value) ? value : null);
}
