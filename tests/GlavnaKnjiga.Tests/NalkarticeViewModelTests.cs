using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class NalkarticeViewModelTests
{
    [Fact]
    public void FormirajKarticu_RacunaKumulativniSaldoPreFiltriranogPerioda()
    {
        var redovi = new[]
        {
            new NalpRow
            {
                Konto = "2020", Datdok = new DateTime(2026, 1, 10),
                Dug = 100, Brnal = "1",
            },
            new NalpRow
            {
                Konto = "2020", Datdok = new DateTime(2026, 2, 10),
                Pot = 30, Brnal = "2",
            },
            new NalpRow
            {
                Konto = "4350", Datdok = new DateTime(2026, 2, 10),
                Pot = 40, Brnal = "3",
            },
        };

        var rezultat = NalkarticeViewModel.FormirajKarticu(
            redovi, "2020", new DateTime(2026, 2, 1), new DateTime(2026, 2, 28),
            "", "", "0");

        var red = Assert.Single(rezultat);
        Assert.Equal(70, red.Dpsaldo);
        Assert.Equal(30, red.Pot);
    }

    [Fact]
    public void FormirajKarticu_VodiOdvojenSaldoZaSvakiKonto()
    {
        var redovi = new[]
        {
            new NalpRow { Konto = "2020", Datdok = new DateTime(2026, 1, 1), Dug = 100 },
            new NalpRow { Konto = "4350", Datdok = new DateTime(2026, 1, 1), Pot = 60 },
        };

        var rezultat = NalkarticeViewModel.FormirajKarticu(
            redovi, "", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31),
            "", "", "0");

        Assert.Equal(100, rezultat.Single(r => r.Konto == "2020").Dpsaldo);
        Assert.Equal(-60, rezultat.Single(r => r.Konto == "4350").Dpsaldo);
    }
}
