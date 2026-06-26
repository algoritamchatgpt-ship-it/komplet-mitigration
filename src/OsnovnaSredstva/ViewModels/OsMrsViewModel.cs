using CommunityToolkit.Mvvm.ComponentModel;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using System.Collections.ObjectModel;
using KolDef = OsnovnaSredstva.Services.OsStampacHelper.KolDef;

namespace OsnovnaSredstva.ViewModels;

public partial class OsMrsViewModel : ObservableObject
{
    public enum TipPregledaEnum { Mrs, PoreskaStara, PoreskaNova }

    [ObservableProperty] private string _naslov = "";
    [ObservableProperty] private string _poruka = "";

    public TipPregledaEnum TipPregledaVm { get; private set; }

    public bool JeMrs => TipPregledaVm == TipPregledaEnum.Mrs;
    public bool JePoreskaStara => TipPregledaVm == TipPregledaEnum.PoreskaStara;
    public bool JePoreskaNova => TipPregledaVm == TipPregledaEnum.PoreskaNova;

    public ObservableCollection<OsMrsRedak> Stavke { get; } = [];

    public static OsMrsViewModel MrsPregled(IEnumerable<OsKartica> kartice, bool skraceni = false)
    {
        var vm = new OsMrsViewModel { TipPregledaVm = TipPregledaEnum.Mrs };
        vm.Naslov = skraceni
            ? "PREGLED MRS - Skraceni"
            : "PREGLED MRS - Osnovna sredstva";
        vm.Ucitaj(kartice);
        return vm;
    }

    public static OsMrsViewModel KarticePregled(IEnumerable<OsKartica> kartice)
    {
        var vm = new OsMrsViewModel { TipPregledaVm = TipPregledaEnum.Mrs };
        vm.Naslov = "KARTICA OSNOVNIH SREDSTAVA";
        vm.Ucitaj(kartice);
        return vm;
    }

    public static OsMrsViewModel PoreskaStara(IEnumerable<OsKartica> kartice)
    {
        var vm = new OsMrsViewModel { TipPregledaVm = TipPregledaEnum.PoreskaStara };
        vm.Naslov = "PREGLED PORESKE - Pocetne vrednosti (PP)";
        vm.Ucitaj(kartice);
        return vm;
    }

    public static OsMrsViewModel PoreskaNova(IEnumerable<OsKartica> kartice)
    {
        var vm = new OsMrsViewModel { TipPregledaVm = TipPregledaEnum.PoreskaNova };
        vm.Naslov = "PREGLED PORESKE - Tekuce vrednosti (PP)";
        vm.Ucitaj(kartice);
        return vm;
    }

    private void Ucitaj(IEnumerable<OsKartica> kartice)
    {
        foreach (var k in kartice)
        {
            Stavke.Add(new OsMrsRedak
            {
                Sifra = k.Osifra?.Trim() ?? "",
                Naziv = k.Naz?.Trim() ?? "",
                Konto = k.Konto?.Trim() ?? "",
                Mesto = k.Mesto?.Trim() ?? "",
                Ag = k.Ag?.Trim() ?? "",
                Nab0 = k.Nab0,
                Isp0 = k.Isp0,
                Sad0 = k.Sad0,
                StopaOt = k.StopaOt,
                Amort = D(k, "AMORT"),
                Isp = D(k, "ISP"),
                Sad = D(k, "SAD"),
                Nab02 = D(k, "NAB02"),
                Isp02 = D(k, "ISP02"),
                Sad02 = D(k, "SAD02"),
                StopaOt2 = D(k, "STOPAOT2"),
                Amort2 = D(k, "AMORT2"),
                Isp2 = D(k, "ISP2"),
                Sad2 = D(k, "SAD2"),
            });
        }

        Poruka = $"Ukupno {Stavke.Count} zapisa.";
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void Stampaj()
    {
        if (JeMrs)
        {
            KolDef[] kol = [
                new("Šifra",   58, false), new("Naziv",  160, false), new("Konto", 82, false),
                new("Mesto",   68, false), new("AG",      42, false),
                new("Nab0",    88), new("Isp0", 88), new("Sad0",  88),
                new("Stopa%",  58), new("Amort",88), new("Isp",   88), new("Sad",  88)
            ];
            var redovi = Stavke.Select(s => new[] {
                s.Sifra, s.Naziv, s.Konto, s.Mesto, s.Ag,
                s.Nab0.ToString("N2"), s.Isp0.ToString("N2"), s.Sad0.ToString("N2"),
                s.StopaOt.ToString("N3"), s.Amort.ToString("N2"), s.Isp.ToString("N2"), s.Sad.ToString("N2")
            }).ToList();
            string[] uk = [
                "UKUPNO", "", "", "", "",
                Stavke.Sum(s => s.Nab0).ToString("N2"),  Stavke.Sum(s => s.Isp0).ToString("N2"),
                Stavke.Sum(s => s.Sad0).ToString("N2"),  "",
                Stavke.Sum(s => s.Amort).ToString("N2"), Stavke.Sum(s => s.Isp).ToString("N2"),
                Stavke.Sum(s => s.Sad).ToString("N2")
            ];
            OsStampacHelper.Stampaj(Naslov, kol, redovi, uk, landscape: true, m => Poruka = m);
        }
        else if (JePoreskaStara)
        {
            KolDef[] kol = [
                new("Šifra",  65, false), new("Naziv", 210, false),
                new("Nab02", 100), new("Isp02", 100), new("Sad02", 100), new("St.PP%", 70)
            ];
            var redovi = Stavke.Select(s => new[] {
                s.Sifra, s.Naziv,
                s.Nab02.ToString("N2"), s.Isp02.ToString("N2"), s.Sad02.ToString("N2"),
                s.StopaOt2.ToString("N3")
            }).ToList();
            string[] uk = [
                "UKUPNO", "",
                Stavke.Sum(s => s.Nab02).ToString("N2"), Stavke.Sum(s => s.Isp02).ToString("N2"),
                Stavke.Sum(s => s.Sad02).ToString("N2"), ""
            ];
            OsStampacHelper.Stampaj(Naslov, kol, redovi, uk, landscape: false, m => Poruka = m);
        }
        else // JePoreskaNova
        {
            KolDef[] kol = [
                new("Šifra",   65, false), new("Naziv", 210, false),
                new("Amort2", 100), new("Isp2", 100), new("Sad2", 100), new("St.PP%", 70)
            ];
            var redovi = Stavke.Select(s => new[] {
                s.Sifra, s.Naziv,
                s.Amort2.ToString("N2"), s.Isp2.ToString("N2"), s.Sad2.ToString("N2"),
                s.StopaOt2.ToString("N3")
            }).ToList();
            string[] uk = [
                "UKUPNO", "",
                Stavke.Sum(s => s.Amort2).ToString("N2"), Stavke.Sum(s => s.Isp2).ToString("N2"),
                Stavke.Sum(s => s.Sad2).ToString("N2"), ""
            ];
            OsStampacHelper.Stampaj(Naslov, kol, redovi, uk, landscape: false, m => Poruka = m);
        }
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
            sw.WriteLine("Sifra;Naziv;Konto;Mesto;AG;Nab0;Isp0;Sad0;StopaOt;Amort;Isp;Sad;Nab02;Isp02;Sad02;StopaOt2;Amort2;Isp2;Sad2");
            foreach (var s in Stavke)
                sw.WriteLine($"{s.Sifra};{s.Naziv};{s.Konto};{s.Mesto};{s.Ag};{s.Nab0:N2};{s.Isp0:N2};{s.Sad0:N2};{s.StopaOt:N3};{s.Amort:N2};{s.Isp:N2};{s.Sad:N2};{s.Nab02:N2};{s.Isp02:N2};{s.Sad02:N2};{s.StopaOt2:N3};{s.Amort2:N2};{s.Isp2:N2};{s.Sad2:N2}");
            Poruka = $"CSV izvoz završen: {dlg.FileName} ({Stavke.Count} redova).";
        }
        catch (Exception ex) { Poruka = $"Greška izvoza: {ex.Message}"; }
    }

    private static decimal D(OsKartica k, string p)
        => OsSaldoViewModel.DajDec(k, p);
}
