using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class NalprilViewModelTests
{
    [Fact]
    public void FormirajAutomatskeRedove_RazdvajaPrethodniITekuciPeriod()
    {
        var podesavanja = new[] { new NalprilKontoPodesavanje("2020", "D") };
        var aaan = new Dictionary<string, NalprilAaan>(StringComparer.OrdinalIgnoreCase)
        {
            ["2020"] = new("2020", "1", "KUPCI")
        };
        var analitika = new[]
        {
            new NalprilAnalitikaStavka("00001", "R-1", new DateTime(2026, 1, 10), 1_000, 0, ""),
            new NalprilAnalitikaStavka("00001", "R-1", new DateTime(2026, 1, 15), 0, 250, ""),
            new NalprilAnalitikaStavka("00002", "R-2", new DateTime(2026, 6, 10), 600, 0, ""),
            new NalprilAnalitikaStavka("00002", "R-2", new DateTime(2026, 6, 12), 0, 100, ""),
            new NalprilAnalitikaStavka("00003", "R-3", new DateTime(2026, 6, 15), 500, 0, "*"),
        };
        var nazivi = new Dictionary<string, string>
        {
            ["00001"] = "Kupac jedan",
            ["00002"] = "Kupac dva",
        };

        var rezultat = Nalpril1ViewModel.FormirajAutomatskeRedove(
            podesavanja,
            aaan,
            _ => analitika,
            nazivi,
            new DateTime(2026, 6, 1),
            new DateTime(2026, 6, 30));

        Assert.Equal(2, rezultat.Count);
        Assert.Equal(750, rezultat[0].Dugpre);
        Assert.Equal(0, rezultat[0].Dug);
        Assert.Equal("Kupac jedan", rezultat[0].Naziv);
        Assert.Equal(500, rezultat[1].Dug);
        Assert.Equal("*", rezultat[1].Pauto);
    }

    [Fact]
    public void FormirajAutomatskeRedove_PotrazujeFormiraOdliv()
    {
        var podesavanja = new[] { new NalprilKontoPodesavanje("4330", "P") };
        var aaan = new Dictionary<string, NalprilAaan>(StringComparer.OrdinalIgnoreCase)
        {
            ["4330"] = new("4330", "2", "DOBAVLJAČI")
        };
        var analitika = new[]
        {
            new NalprilAnalitikaStavka("00009", "U-1", new DateTime(2026, 6, 20), 200, 900, ""),
        };

        var rezultat = Nalpril1ViewModel.FormirajAutomatskeRedove(
            podesavanja, aaan, _ => analitika,
            new Dictionary<string, string>(),
            new DateTime(2026, 6, 1), new DateTime(2026, 6, 30));

        var red = Assert.Single(rezultat);
        Assert.Equal(700, red.Pot);
        Assert.Equal(0, red.Dug);
        Assert.Equal("DOBAVLJAČI", red.Opis);
    }

    [Fact]
    public void PreglediLikvidnosti_GrupišuPoKontu()
    {
        var redovi = new[]
        {
            new NalprilRow { Konto = "2020", Dugpre = 100, Dug = 50 },
            new NalprilRow { Konto = "2020", Potpre = 20, Pot = 10 },
            new NalprilRow { Konto = "4330", Pot = 80 },
        };

        var tekuca = NalprilViewModel.FormirajTekucuLikvidnost(redovi);
        var ukupna = NalprilViewModel.FormirajUkupnuLikvidnost(redovi);

        Assert.Equal(2, tekuca.Count);
        Assert.Equal(50, tekuca[0].Dug);
        Assert.Equal(10, tekuca[0].Pot);
        Assert.Equal(3, ukupna.Count);
        Assert.Equal("1", ukupna[0].Grupa);
        Assert.Equal(80, ukupna[0].Saldo);
    }
}
