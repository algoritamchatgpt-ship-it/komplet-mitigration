using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class NalzatViewModelTests
{
    [Fact]
    public void ZatvaranjeKlasa_FormiraKontraStavkeISumarnaKonta()
    {
        var vm = NalzatViewModel.ZaKlase("C:\\ne-koristi-se", 2026);
        var izvor = new[]
        {
            new NalpRow { Konto = "5000", Dug = 120m, Pot = 20m },
            new NalpRow { Konto = "6000", Dug = 10m, Pot = 210m },
            new NalpRow { Konto = "5990", Dug = 5m, Pot = 0m },
            new NalpRow { Konto = "6990", Dug = 0m, Pot = 7m },
        };

        var rezultat = vm.FormirajZatvaranjeKlasa(izvor);

        Assert.Collection(rezultat,
            r => AssertRed(r, "5000", 0m, 100m),
            r => AssertRed(r, "6000", 200m, 0m),
            r => AssertRed(r, "6990", 0m, 200m),
            r => AssertRed(r, "5990", 100m, 0m));
        Assert.Equal(rezultat.Sum(r => r.Dug), rezultat.Sum(r => r.Pot));
    }

    [Fact]
    public void ZatvaranjeAktiveIPasive_FormiraKontraStavkeIBalansiraKonto7300()
    {
        var vm = NalzatViewModel.ZaAktivuIPasivu("C:\\ne-koristi-se", 2026);
        var izvor = new[]
        {
            new NalpRow { Konto = "1000", Dug = 150m, Pot = 20m },
            new NalpRow { Konto = "2000", Dug = 10m, Pot = 70m },
        };

        var rezultat = vm.FormirajZatvaranjeAktiveIPasive(izvor);

        Assert.Collection(rezultat,
            r => AssertRed(r, "1000", 0m, 130m),
            r => AssertRed(r, "2000", 60m, 0m),
            r => AssertRed(r, "7300", 0m, 60m),
            r => AssertRed(r, "7300", 130m, 0m));
        Assert.Equal(rezultat.Sum(r => r.Dug), rezultat.Sum(r => r.Pot));
    }

    private static void AssertRed(NalpRow red, string konto, decimal dug, decimal pot)
    {
        Assert.Equal(konto, red.Konto);
        Assert.Equal(dug, red.Dug);
        Assert.Equal(pot, red.Pot);
    }
}
