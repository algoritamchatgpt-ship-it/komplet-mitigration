using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class NalmtrPregledViewModelTests
{
    [Fact]
    public void FormirajPregled_FiltriraPeriodIKlasuISabiraKonta()
    {
        var rows = new[]
        {
            new NalmtrRow
            {
                Konto = "5000", Datdok = new DateTime(2026, 1, 1),
                Dug = 100m, Pot = 20m, Saldo = 80m, Iznos01 = 30m, Iznos02 = 50m,
            },
            new NalmtrRow
            {
                Konto = "5000", Datdok = new DateTime(2026, 2, 1),
                Dug = 40m, Pot = 10m, Saldo = 30m, Iznos01 = 10m, Iznos02 = 20m,
            },
            new NalmtrRow
            {
                Konto = "6000", Datdok = new DateTime(2026, 1, 1),
                Pot = 70m, Iznos01 = 70m,
            },
            new NalmtrRow
            {
                Konto = "5000", Datdok = new DateTime(2025, 12, 31),
                Dug = 999m, Iznos01 = 999m,
            },
        };

        var rezultat = NalmtrPregledViewModel.FormirajPregled(
            rows, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), "5");

        var red = Assert.Single(rezultat);
        Assert.Equal("5000", red.Konto);
        Assert.Equal(140m, red.Dug);
        Assert.Equal(30m, red.Pot);
        Assert.Equal(110m, red.Saldo);
        Assert.Equal(40m, red.Iznos01);
        Assert.Equal(70m, red.Iznos02);
        Assert.Equal(110m, red.Ukupno);
    }

    [Fact]
    public void FormirajPregled_BezKlaseVracaSveKategorije()
    {
        var rows = new[]
        {
            new NalmtrRow { Konto = "5000", Datdok = new DateTime(2026, 1, 1), Iznos01 = 10m },
            new NalmtrRow { Konto = "6000", Datdok = new DateTime(2026, 1, 1), Iznos01 = 20m },
        };

        var rezultat = NalmtrPregledViewModel.FormirajPregled(
            rows, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), null);

        Assert.Equal(new[] { "5000", "6000" },
            rezultat.Select(r => r.Konto).ToArray());
    }
}
