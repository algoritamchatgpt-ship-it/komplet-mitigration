using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class NalptkViewModelTests
{
    [Fact]
    public void FormirajStavke_SabiraPazarIUslugeZaIzabraniNalog()
    {
        var prometi = new[]
        {
            new NalptkViewModel.KepuPromet("1", "1340", "6120", "15", 100, 20),
            new NalptkViewModel.KepuPromet("1", "1340", "6120", "15", 50, 5),
            new NalptkViewModel.KepuPromet("1", "1340", "6120", "16", 999, 999),
        };

        var rezultat = NalptkViewModel.FormirajStavke(
            prometi, "15", new DateTime(2026, 6, 25));

        Assert.Collection(rezultat,
            pazar =>
            {
                Assert.Equal("1340", pazar.Konto);
                Assert.Equal(150, pazar.Pot);
                Assert.Equal("PAZAR PRODAVNICE 1", pazar.Opis);
                Assert.Equal("M1", pazar.Dok);
                Assert.Equal("15", pazar.Brnal);
            },
            usluge =>
            {
                Assert.Equal("6120", usluge.Konto);
                Assert.Equal(25, usluge.Pot);
                Assert.Equal("PAZAR PROD.1 USLUGE", usluge.Opis);
                Assert.Equal(new DateTime(2026, 6, 25), usluge.Datdok);
            });
    }

    [Fact]
    public void FormirajStavke_PreskaceNulteIznose()
    {
        var prometi = new[]
        {
            new NalptkViewModel.KepuPromet("1", "1340", "6120", "15", 0, 30),
            new NalptkViewModel.KepuPromet("2", "1341", "6121", "15", 0, 0),
        };

        var rezultat = NalptkViewModel.FormirajStavke(
            prometi, "15", new DateTime(2026, 1, 1));

        var red = Assert.Single(rezultat);
        Assert.Equal("6120", red.Konto);
        Assert.Equal(30, red.Pot);
    }
}
