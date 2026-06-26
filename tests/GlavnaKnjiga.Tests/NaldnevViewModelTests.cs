using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class NaldnevViewModelTests
{
    [Fact]
    public void FormirajPregled_PrimenjujeOriginalneFiltere()
    {
        var redovi = new[]
        {
            new NalpRow
            {
                Konto = "202001", Datdok = new DateTime(2026, 2, 1),
                Dok = "IRA", Mp = 1, Mtr = 10, Dug = 100, Brnal = "2",
            },
            new NalpRow
            {
                Konto = "202002", Datdok = new DateTime(2026, 2, 2),
                Dok = "IRA", Mp = 1, Mtr = 10, Pot = 80, Brnal = "1",
            },
            new NalpRow
            {
                Konto = "202003", Datdok = new DateTime(2026, 2, 3),
                Dok = "IRA", Mp = 2, Mtr = 10, Dug = 50, Brnal = "3",
            },
            new NalpRow
            {
                Konto = "201000", Datdok = new DateTime(2026, 2, 1),
                Dok = "IRA", Mp = 1, Mtr = 10, Dug = 40, Brnal = "4",
            },
        };

        var rezultat = NaldnevViewModel.FormirajPregled(
            redovi, "202", new DateTime(2026, 2, 1), new DateTime(2026, 2, 28),
            "IRA", "1", "10", "D");

        var red = Assert.Single(rezultat);
        Assert.Equal("202001", red.Konto);
    }

    [Fact]
    public void FormirajPregled_NulaZaMtrZnaciSvaMestaTroska()
    {
        var redovi = new[]
        {
            new NalpRow { Konto = "5000", Datdok = new DateTime(2026, 1, 2), Mtr = 10, Pot = 20 },
            new NalpRow { Konto = "5000", Datdok = new DateTime(2026, 1, 1), Mtr = 20, Dug = 30 },
        };

        var rezultat = NaldnevViewModel.FormirajPregled(
            redovi, "", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31),
            "", "", "0", "S");

        Assert.Equal(2, rezultat.Count);
        Assert.Equal(new[] { new DateTime(2026, 1, 1), new DateTime(2026, 1, 2) },
            rezultat.Select(r => r.Datdok!.Value).ToArray());
    }

    [Fact]
    public void FormirajPregled_ObrcePogresnoUnetPeriod()
    {
        var redovi = new[]
        {
            new NalpRow { Konto = "5000", Datdok = new DateTime(2026, 6, 1), Dug = 10 },
        };

        var rezultat = NaldnevViewModel.FormirajPregled(
            redovi, "", new DateTime(2026, 12, 31), new DateTime(2026, 1, 1),
            "", "", "0", "S");

        Assert.Single(rezultat);
    }
}
