using CommunityToolkit.Mvvm.ComponentModel;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using System.Collections.ObjectModel;
using KolDef = OsnovnaSredstva.Services.OsStampacHelper.KolDef;

namespace OsnovnaSredstva.ViewModels;

public partial class OsSaldoViewModel : ObservableObject
{
    public enum OsSaldoPrikazTip
    {
        Analitika,
        Sintetika,
        NabavkePoAgrupama,
        PocetnoStanje
    }

    [ObservableProperty] private string _naslov = "";
    [ObservableProperty] private string _poruka = "";

    public ObservableCollection<OsSaldoStavka> Stavke { get; } = [];
    public OsSaldoPrikazTip Prikaz { get; private set; } = OsSaldoPrikazTip.Analitika;
    public string NazivKljucneKolone { get; private set; } = "Sifra";

    public static OsSaldoViewModel PoKontu(IEnumerable<OsKartica> kartice)
    {
        var vm = new OsSaldoViewModel();
        vm.Prikaz = OsSaldoPrikazTip.Analitika;
        vm.NazivKljucneKolone = "Konto";
        vm.Naslov = "SALDO ANALITIKA";
        vm.Ucitaj(kartice, k => string.IsNullOrWhiteSpace(k.Konto) ? "(bez konta)" : k.Konto.Trim());
        return vm;
    }

    public static OsSaldoViewModel PoKontuSintetika(IEnumerable<OsKartica> kartice)
    {
        var vm = new OsSaldoViewModel();
        vm.Prikaz = OsSaldoPrikazTip.Sintetika;
        vm.NazivKljucneKolone = "Konto";
        vm.Naslov = "SALDO SINTETIKA";
        vm.Ucitaj(
            kartice,
            k => string.IsNullOrWhiteSpace(k.Konto) ? "(bez konta)" : k.Konto.Trim(),
            ukljuciTekuciMrs: true,
            ukljuciPoreskaPolja: false);
        return vm;
    }

    /// <summary>
    /// Legacy ospopis5.prg ("SALDO NABAVKE PO A.GRUPAMA"): TOTAL ON AG FIELDS
    /// NAB0,ISP0,SAD0,NAB02,ISP02,SAD02,NAB,ISP,SAD,NAB2,ISP2,SAD2,AMORT,AMORT2
    /// ... FOR DATNAB&gt;=MEDAT0 — pun set MRS+poreskih kolona, samo NOVE kartice
    /// (nabavljene u tekućem ili kasnijem periodu).
    /// </summary>
    public static OsSaldoViewModel SaldoNabavkePoAgrupama(IEnumerable<OsKartica> kartice, DateTime? periodOd)
    {
        var vm = new OsSaldoViewModel();
        vm.Prikaz = OsSaldoPrikazTip.NabavkePoAgrupama;
        vm.NazivKljucneKolone = "AG";
        vm.Naslov = "SALDO NABAVKE PO A.GRUPAMA";

        var filtrirane = periodOd.HasValue
            ? kartice.Where(k => k.DatNab.HasValue && k.DatNab.Value.Date >= periodOd.Value.Date)
            : kartice;

        vm.Ucitaj(
            filtrirane,
            k => string.IsNullOrWhiteSpace(k.Ag) ? "(bez AG)" : k.Ag.Trim(),
            ukljuciTekuciMrs: true,
            ukljuciPoreskaPolja: true);

        if (periodOd.HasValue)
            vm.Poruka += $"  (DATNAB >= {periodOd.Value:dd.MM.yyyy})";

        return vm;
    }

    public static OsSaldoViewModel PoMestu(IEnumerable<OsKartica> kartice, string? kontoFilter = null)
    {
        var vm = new OsSaldoViewModel();
        vm.Prikaz = OsSaldoPrikazTip.Analitika;
        vm.NazivKljucneKolone = "Mesto";
        var konto = (kontoFilter ?? string.Empty).Trim();

        vm.Naslov = string.IsNullOrWhiteSpace(konto)
            ? "SALDO PO MESTIMA"
            : $"SALDO PO MESTIMA - KONTO {konto}";

        var filtrirane = string.IsNullOrWhiteSpace(konto)
            ? kartice
            : kartice.Where(k => string.Equals((k.Konto ?? string.Empty).Trim(), konto, StringComparison.OrdinalIgnoreCase));

        vm.Ucitaj(filtrirane, k => string.IsNullOrWhiteSpace(k.Mesto) ? "(bez mesta)" : k.Mesto.Trim());
        return vm;
    }

    /// <summary>
    /// Legacy ospopis4.prg ("PREGLED KARTICA" dugme na OS.scx): TOTAL ON KONTO FIELDS
    /// NAB0,ISP0,SAD0 ... FOR DATNAB&lt;MEDAT0 — saldo po kontu samo za kartice nabavljene
    /// pre početka tekućeg perioda (ospodaci.dbf EDAT0), bez MRS/poreskih kolona.
    /// </summary>
    public static OsSaldoViewModel KarticaPoKontu(IEnumerable<OsKartica> kartice, DateTime? periodOd)
    {
        var vm = new OsSaldoViewModel();
        vm.Prikaz = OsSaldoPrikazTip.Sintetika;
        vm.NazivKljucneKolone = "Konto";
        vm.Naslov = "KARTICA OSNOVNIH SREDSTAVA";

        var filtrirane = periodOd.HasValue
            ? kartice.Where(k => k.DatNab.HasValue && k.DatNab.Value.Date < periodOd.Value.Date)
            : kartice;

        vm.Ucitaj(
            filtrirane,
            k => string.IsNullOrWhiteSpace(k.Konto) ? "(bez konta)" : k.Konto.Trim(),
            ukljuciTekuciMrs: false,
            ukljuciPoreskaPolja: false);

        if (periodOd.HasValue)
            vm.Poruka += $"  (DATNAB < {periodOd.Value:dd.MM.yyyy})";

        return vm;
    }

    public static OsSaldoViewModel PoPopisu(IEnumerable<OsKartica> kartice, string naslov)
    {
        var vm = new OsSaldoViewModel();
        vm.Prikaz = OsSaldoPrikazTip.Analitika;
        vm.NazivKljucneKolone = "Konto";
        vm.Naslov = naslov;
        vm.Ucitaj(kartice, k => string.IsNullOrWhiteSpace(k.Konto) ? "(bez konta)" : k.Konto.Trim());
        return vm;
    }

    private void Ucitaj(
        IEnumerable<OsKartica> kartice,
        Func<OsKartica, string> kljuc,
        bool ukljuciTekuciMrs = true,
        bool ukljuciPoreskaPolja = true)
    {
        var svi = kartice.ToList();
        var grupe = new SortedDictionary<string, OsSaldoStavka>(StringComparer.OrdinalIgnoreCase);

        foreach (var k in svi)
        {
            var sifra = kljuc(k);
            if (!grupe.TryGetValue(sifra, out var s))
            {
                s = new OsSaldoStavka { Sifra = sifra };
                grupe[sifra] = s;
            }

            s.BrojKartica++;
            s.Nab0 += k.Nab0;
            s.Isp0 += k.Isp0;
            s.Sad0 += k.Sad0;

            if (ukljuciTekuciMrs)
            {
                s.Nab += DajDec(k, "NAB");
                s.Isp += DajDec(k, "ISP");
                s.Sad += DajDec(k, "SAD");
                s.Amort += DajDec(k, "AMORT");
            }

            if (ukljuciPoreskaPolja)
            {
                s.Nab02 += DajDec(k, "NAB02");
                s.Isp02 += DajDec(k, "ISP02");
                s.Sad02 += DajDec(k, "SAD02");
                s.Nab2 += DajDec(k, "NAB2");
                s.Isp2 += DajDec(k, "ISP2");
                s.Amort2 += DajDec(k, "AMORT2");
                s.Sad2 += DajDec(k, "SAD2");
            }
        }

        foreach (var s in grupe.Values) Stavke.Add(s);
        Poruka = $"Ukupno {grupe.Count} grupe, {svi.Count} kartica.";
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void Stampaj()
    {
        KolDef[] kol = [
            new(NazivKljucneKolone, 90, false), new("Br.", 38),
            new("Nab0",  86), new("Isp0",  86), new("Sad0",  86),
            new("Amort", 86), new("Isp.",  86), new("Sad.",  86),
            new("Sad02", 86), new("Nab2",  86), new("Isp2",  86),
            new("Amort2",86), new("Sad2",  86)
        ];
        var redovi = Stavke.Select(s => new[] {
            s.Sifra, s.BrojKartica.ToString(),
            s.Nab0.ToString("N2"),  s.Isp0.ToString("N2"),  s.Sad0.ToString("N2"),
            s.Amort.ToString("N2"), s.Isp.ToString("N2"),   s.Sad.ToString("N2"),
            s.Sad02.ToString("N2"), s.Nab2.ToString("N2"),  s.Isp2.ToString("N2"),
            s.Amort2.ToString("N2"),s.Sad2.ToString("N2")
        }).ToList();
        string[] uk = [
            "UKUPNO", Stavke.Sum(s => s.BrojKartica).ToString(),
            Stavke.Sum(s => s.Nab0).ToString("N2"),   Stavke.Sum(s => s.Isp0).ToString("N2"),
            Stavke.Sum(s => s.Sad0).ToString("N2"),   Stavke.Sum(s => s.Amort).ToString("N2"),
            Stavke.Sum(s => s.Isp).ToString("N2"),    Stavke.Sum(s => s.Sad).ToString("N2"),
            Stavke.Sum(s => s.Sad02).ToString("N2"),  Stavke.Sum(s => s.Nab2).ToString("N2"),
            Stavke.Sum(s => s.Isp2).ToString("N2"),   Stavke.Sum(s => s.Amort2).ToString("N2"),
            Stavke.Sum(s => s.Sad2).ToString("N2")
        ];
        OsStampacHelper.Stampaj(Naslov, kol, redovi, uk, landscape: true, m => Poruka = m);
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void IzveziCsv()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Izvoz u CSV",
            Filter = "CSV (*.csv)|*.csv|Svi fajlovi (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = Naslov.Replace(" ", "_") + ".csv"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            using var sw = new System.IO.StreamWriter(dlg.FileName, false, new System.Text.UTF8Encoding(true));
            sw.WriteLine($"{NazivKljucneKolone};BrojKartica;Nab0;Isp0;Sad0;Amort;Isp;Sad;Sad02;Nab2;Isp2;Amort2;Sad2");
            foreach (var s in Stavke)
                sw.WriteLine($"{s.Sifra};{s.BrojKartica};{s.Nab0:N2};{s.Isp0:N2};{s.Sad0:N2};{s.Amort:N2};{s.Isp:N2};{s.Sad:N2};{s.Sad02:N2};{s.Nab2:N2};{s.Isp2:N2};{s.Amort2:N2};{s.Sad2:N2}");
            Poruka = $"CSV izvoz završen: {dlg.FileName} ({Stavke.Count} redova).";
        }
        catch (Exception ex) { Poruka = $"Greška izvoza: {ex.Message}"; }
    }

    internal static decimal DajDec(OsKartica k, string polje)
    {
        if (!k.ExtraPolja.TryGetValue(polje, out var val) || val is null) return 0m;
        return val switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            double db => (decimal)db,
            _ when decimal.TryParse(val.ToString(),
                       System.Globalization.NumberStyles.Any,
                       System.Globalization.CultureInfo.InvariantCulture, out var d) => d,
            _ => 0m
        };
    }
}
