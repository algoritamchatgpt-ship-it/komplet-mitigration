using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class NalpDefkViewModelTests
{
    [Fact]
    public void FormirajPocetneRedove_PrepisujeSveTriFoxProGrupe()
    {
        var rezultat = NalpDefkViewModel.FormirajPocetneRedove(
            [
                new(1, 11m, "Kupci", "", "2020", "KUP", "D", "D"),
            ],
            [
                new(2, 22m, "Veleprodaja", "", "1320", "VP"),
            ],
            [
                new(3, 33m, "Maloprodaja", "", "1340", "MP"),
            ]);

        Assert.Collection(
            rezultat,
            red =>
            {
                Assert.Equal("AN", red.Vrsta);
                Assert.Equal("anal1", red.Imetabele);
                Assert.Equal("2020", red.Konto);
                Assert.Equal("D", red.Dp);
                Assert.Equal("D", red.Devizno);
                Assert.Empty(red.Dok);
            },
            red =>
            {
                Assert.Equal("VP", red.Vrsta);
                Assert.Equal("tvtm2", red.Imetabele);
                Assert.Equal("V2", red.Dok);
            },
            red =>
            {
                Assert.Equal("MP", red.Vrsta);
                Assert.Equal("tm3", red.Imetabele);
                Assert.Equal("M3", red.Dok);
            });
    }

    [Fact]
    public void FormirajPocetneRedove_PreskaceSakriveneIPraznaKonta()
    {
        var rezultat = NalpDefkViewModel.FormirajPocetneRedove(
            [
                new(1, 0m, "Sakriven", "N", "2020", ""),
                new(2, 0m, "Bez konta", "", "   ", ""),
                new(3, 0m, "Vidljiv", "D", "2021", ""),
            ],
            [],
            []);

        var red = Assert.Single(rezultat);
        Assert.Equal("2021", red.Konto);
        Assert.Equal("anal3", red.Imetabele);
    }
}
