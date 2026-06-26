using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class Nalrezul6ViewModelTests
{
    [Fact]
    public void FormirajRezultat_PreslikavaFoxProRedosledISalda()
    {
        var vm = new Nalrezul6ViewModel("C:\\ne-koristi-se", 2026);
        var izvor = new[]
        {
            new NalpRow { Konto = "5000", Dug = 120m, Pot = 20m },
            new NalpRow { Konto = "5990", Dug = 5m, Pot = 0m },
            new NalpRow { Konto = "6000", Dug = 10m, Pot = 210m },
            new NalpRow { Konto = "6990", Dug = 0m, Pot = 7m },
            new NalpRow { Konto = "7210", Dug = 10m, Pot = 0m },
            new NalpRow { Konto = "7220", Dug = 5m, Pot = 0m },
            new NalpRow { Konto = "7230", Dug = 2m, Pot = 0m },
        };

        var rezultat = vm.FormirajRezultat(izvor);

        Assert.Collection(rezultat,
            r => AssertRed(r, "5990", 0m, 100m),
            r => AssertRed(r, "7100", 100m, 0m),
            r => AssertRed(r, "6990", 214m, 0m),
            r => AssertRed(r, "7100", 0m, 214m),
            r => AssertRed(r, "7100", 114m, 0m),
            r => AssertRed(r, "7120", 0m, 114m),
            r => AssertRed(r, "7120", 114m, 0m),
            r => AssertRed(r, "7200", 0m, 114m),
            r => AssertRed(r, "7200", 114m, 0m),
            r => AssertRed(r, "7210", 0m, 10m),
            r => AssertRed(r, "7230", 0m, 2m),
            r => AssertRed(r, "7220", 0m, 5m),
            r => AssertRed(r, "7240", 0m, 97m),
            r => AssertRed(r, "7240", 97m, 0m),
            r => AssertRed(r, "3410", 0m, 97m));

        Assert.Equal(rezultat.Sum(r => r.Dug), rezultat.Sum(r => r.Pot));
        Assert.All(rezultat, r =>
        {
            Assert.Equal("999991", r.Brnal);
            Assert.Equal("UTVRĐIVANJE REZULTATA", r.Opis);
            Assert.Equal(new DateTime(2026, 12, 31), r.Datdok);
        });
    }

    [Fact]
    public void FormirajRezultat_Iskljucuje599IDvostrukoSabira699()
    {
        var vm = new Nalrezul6ViewModel("C:\\ne-koristi-se", 2026);
        var izvor = new[]
        {
            new NalpRow { Konto = "5999", Dug = 40m, Pot = 0m },
            new NalpRow { Konto = "6999", Dug = 0m, Pot = 30m },
        };

        var rezultat = vm.FormirajRezultat(izvor);

        AssertRed(rezultat[0], "5990", 0m, 0m);
        AssertRed(rezultat[2], "6990", 60m, 0m);
    }

    private static void AssertRed(NalpRow red, string konto, decimal dug, decimal pot)
    {
        Assert.Equal(konto, red.Konto);
        Assert.Equal(dug, red.Dug);
        Assert.Equal(pot, red.Pot);
    }
}
