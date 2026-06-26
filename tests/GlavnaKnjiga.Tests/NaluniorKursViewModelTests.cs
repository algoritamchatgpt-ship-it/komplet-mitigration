using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class NaluniorKursViewModelTests
{
    [Fact]
    public void PrimeniKurs_PreračunavaDevizneIRedovneRacune()
    {
        var redovi = new[]
        {
            new UniorRow
            {
                Datpdv = new DateTime(2026, 6, 20),
                Devdug = 100,
                Kurs = 7,
            },
            new UniorRow
            {
                Osn18 = 1_000,
                Pdv18 = 200,
            },
        };
        var kursevi = new Dictionary<DateTime, decimal>
        {
            [new DateTime(2026, 6, 20)] = 117.25m,
        };

        var rezultat = NaluniorKursViewModel.PrimeniKurs(
            redovi, "eur", kursevi);

        Assert.Equal("EUR", redovi[0].Dev);
        Assert.Equal(117.25m, redovi[0].Devkurs);
        Assert.Equal(11_725m, redovi[0].Osn18);
        Assert.Equal(11_725m, redovi[0].Ukprod);
        Assert.Equal(7, redovi[0].Kurs);
        Assert.Equal(1_200m, redovi[1].Ukprod);
        Assert.Equal(1, rezultat.Deviznih);
        Assert.Equal(1, rezultat.SaKursom);
        Assert.Equal(0, rezultat.BezKursa);
    }

    [Fact]
    public void PrimeniKurs_KadaKursNedostajePostavljaNultiPreracun()
    {
        var red = new UniorRow
        {
            Datpdv = new DateTime(2026, 6, 21),
            Devdug = 50,
            Osn18 = 999,
            Ukprod = 999,
        };

        var rezultat = NaluniorKursViewModel.PrimeniKurs(
            new[] { red }, "USD", new Dictionary<DateTime, decimal>());

        Assert.Equal("USD", red.Dev);
        Assert.Equal(0, red.Devkurs);
        Assert.Equal(0, red.Osn18);
        Assert.Equal(0, red.Ukprod);
        Assert.Equal(1, rezultat.BezKursa);
    }
}
