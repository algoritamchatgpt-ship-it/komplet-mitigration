using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class NalViewModelTests
{
    [Fact]
    public void SortirajRedove_PodrzavaDatumKontoINalogRedosled()
    {
        var redovi = new[]
        {
            new NalpRow { Konto = "6000", Brnal = "2", Datdok = new DateTime(2026, 2, 1) },
            new NalpRow { Konto = "5000", Brnal = "3", Datdok = new DateTime(2026, 1, 1) },
            new NalpRow { Konto = "4000", Brnal = "1", Datdok = new DateTime(2026, 3, 1) },
        };

        Assert.Equal(
            new[] { "5000", "6000", "4000" },
            NalViewModel.SortirajRedove(redovi, 0).Select(r => r.Konto));
        Assert.Equal(
            new[] { "4000", "5000", "6000" },
            NalViewModel.SortirajRedove(redovi, 1).Select(r => r.Konto));
        Assert.Equal(
            new[] { "1", "2", "3" },
            NalViewModel.SortirajRedove(redovi, 2).Select(r => r.Brnal));
    }

    [Fact]
    public void FormirajSintetiku_GrupiseKontaISabiraIznose()
    {
        var redovi = new[]
        {
            new NalpRow { Brnal = "15", Konto = "2020", Dug = 100, Devdug = 10 },
            new NalpRow { Brnal = "15", Konto = "2020", Pot = 20, Devpot = 2 },
            new NalpRow { Brnal = "15", Konto = "4350", Pot = 80 },
            new NalpRow { Brnal = "16", Konto = "2020", Dug = 999 },
        };

        var rezultat = NalViewModel.FormirajSintetiku(redovi, "15");

        Assert.Equal(2, rezultat.Count);
        Assert.Equal("2020", rezultat[0].Konto);
        Assert.Equal(100, rezultat[0].Dug);
        Assert.Equal(20, rezultat[0].Pot);
        Assert.Equal(10, rezultat[0].Devdug);
        Assert.Equal(2, rezultat[0].Devpot);
        Assert.Equal("4350", rezultat[1].Konto);
        Assert.Equal(80, rezultat[1].Pot);
    }
}
