using CommunityToolkit.Mvvm.ComponentModel;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using System.Collections.ObjectModel;
using KolDef = OsnovnaSredstva.Services.OsStampacHelper.KolDef;

namespace OsnovnaSredstva.ViewModels;

public partial class OsPoaIzvestajViewModel : ObservableObject
{
    public enum TipIzvestaja
    {
        PoaObrazac,
        EvidencijaPoa
    }

    [ObservableProperty] private string _naslov = "";
    [ObservableProperty] private string _poruka = "";
    public ObservableCollection<OsPoaIzvestajRed> Stavke { get; } = [];

    public OsPoaIzvestajViewModel(IEnumerable<OsKartica> kartice, TipIzvestaja tip, DateTime? periodDo)
    {
        Naslov = tip == TipIzvestaja.PoaObrazac
            ? "POA OBRAZAC"
            : "EVIDENCIJA POA";

        var src = kartice ?? Enumerable.Empty<OsKartica>();
        IEnumerable<OsKartica> filtrirano = tip switch
        {
            TipIzvestaja.EvidencijaPoa => src.Where(k =>
                !string.IsNullOrWhiteSpace(DajStr(k, "NACINOB"))),

            TipIzvestaja.PoaObrazac => src.Where(k =>
            {
                var nacinob = DajStr(k, "NACINOB");
                if (!nacinob.Equals("POA", StringComparison.OrdinalIgnoreCase)) return false;

                var datProd = DajDate(k, "DATPROD");
                // Aktivna POA sredstva (bez datuma prodaje) uvijek se prikazuju
                if (!datProd.HasValue) return true;
                // Rashodovana: prikazuju se samo ako su prodata nakon 01.01.2019
                // i unutar posmatranog perioda
                if (datProd.Value <= new DateTime(2019, 1, 1)) return false;
                if (periodDo.HasValue && datProd.Value >= periodDo.Value.Date) return false;
                return true;
            }),

            _ => src
        };

        foreach (var k in filtrirano.OrderBy(k => k.Osifra, StringComparer.OrdinalIgnoreCase))
        {
            Stavke.Add(new OsPoaIzvestajRed
            {
                Osifra = k.Osifra?.Trim() ?? "",
                Naziv = k.Naz?.Trim() ?? "",
                InvBroj = k.InvBroj?.Trim() ?? "",
                DatProd = DajDate(k, "DATPROD"),
                NacinOb = DajStr(k, "NACINOB"),
                Sad = DajDec(k, "SAD"),
                Sad2 = DajDec(k, "SAD2"),
                Pam = DajDec(k, "PAM"),
                Ram = DajDec(k, "RAM"),
                Obezvredj = DajDec(k, "OBEZVREDJ")
            });
        }

        Poruka = $"Ukupno {Stavke.Count} zapisa.";
    }

    private static string DajStr(OsKartica k, string polje)
        => k.ExtraPolja.TryGetValue(polje, out var v)
            ? Convert.ToString(v)?.Trim() ?? string.Empty
            : string.Empty;

    private static decimal DajDec(OsKartica k, string polje)
        => OsSaldoViewModel.DajDec(k, polje);

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void Stampaj()
    {
        KolDef[] kol = [
            new("Osifra",    62, false), new("Naziv",   160, false), new("InvBroj", 90, false),
            new("DatProd",   80),        new("NacinOb",  65, false),
            new("Sad",       90),        new("Sad2",     90),
            new("PAM",       90),        new("RAM",      90), new("Obezvredj", 90)
        ];
        var redovi = Stavke.Select(s => new[] {
            s.Osifra, s.Naziv, s.InvBroj,
            s.DatProd?.ToString("dd.MM.yyyy") ?? "", s.NacinOb,
            s.Sad.ToString("N2"), s.Sad2.ToString("N2"),
            s.Pam.ToString("N2"), s.Ram.ToString("N2"), s.Obezvredj.ToString("N2")
        }).ToList();
        string[] uk = [
            "UKUPNO", "", "", "", "",
            Stavke.Sum(s => s.Sad).ToString("N2"),       Stavke.Sum(s => s.Sad2).ToString("N2"),
            Stavke.Sum(s => s.Pam).ToString("N2"),       Stavke.Sum(s => s.Ram).ToString("N2"),
            Stavke.Sum(s => s.Obezvredj).ToString("N2")
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
            sw.WriteLine("Osifra;Naziv;InvBroj;DatProd;NacinOb;Sad;Sad2;PAM;RAM;Obezvredj");
            foreach (var s in Stavke)
                sw.WriteLine($"{s.Osifra};{s.Naziv};{s.InvBroj};{s.DatProd?.ToString("dd.MM.yyyy") ?? ""};{s.NacinOb};{s.Sad:N2};{s.Sad2:N2};{s.Pam:N2};{s.Ram:N2};{s.Obezvredj:N2}");
            Poruka = $"CSV izvoz završen: {dlg.FileName} ({Stavke.Count} redova).";
        }
        catch (Exception ex) { Poruka = $"Greška izvoza: {ex.Message}"; }
    }

    private static DateTime? DajDate(OsKartica k, string polje)
    {
        if (!k.ExtraPolja.TryGetValue(polje, out var v) || v == null) return null;
        return v switch
        {
            DateTime dt => dt,
            string s when DateTime.TryParse(s, out var dt) => dt,
            _ => null
        };
    }
}

public class OsPoaIzvestajRed
{
    public string Osifra { get; set; } = "";
    public string Naziv { get; set; } = "";
    public string InvBroj { get; set; } = "";
    public DateTime? DatProd { get; set; }
    public string NacinOb { get; set; } = "";
    public decimal Sad { get; set; }
    public decimal Sad2 { get; set; }
    public decimal Pam { get; set; }
    public decimal Ram { get; set; }
    public decimal Obezvredj { get; set; }
}
