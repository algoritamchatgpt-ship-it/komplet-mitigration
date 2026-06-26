using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class NalogPregledViewModelTests
{
    [Fact]
    public void IzNalp_MapiraFinansijskaIDeviznaPolja()
    {
        var izvor = new NalpRow
        {
            Konto = "1000",
            Dug = 120m,
            Pot = 20m,
            Dev = "EUR",
            Devkurs = 117.2m,
            Devdug = 10m,
            Devpot = 2m,
            Ulaz = 3m,
            Izlaz = 1m,
        };

        var red = NalogPregledViewModel.IzNalp(izvor);

        Assert.Equal("1000", red.Konto);
        Assert.Equal(120m, red.Dug);
        Assert.Equal(20m, red.Pot);
        Assert.Equal("EUR", red.Dev);
        Assert.Equal(10m, red.Devdug);
        Assert.Equal(2m, red.Devpot);
        Assert.Equal(3m, red.Ulaz);
        Assert.Equal(1m, red.Izlaz);
    }

    [Fact]
    public void ViewModel_RacunaDinarskeIDevizneTotale()
    {
        var vm = new NalogPregledViewModel("PREGLED",
        [
            new NalogPregledRow { Dug = 100m, Pot = 20m, Devdug = 10m, Devpot = 1m },
            new NalogPregledRow { Dug = 0m, Pot = 30m, Devdug = 0m, Devpot = 2m },
        ]);

        Assert.Equal(100m, vm.UkupnoDug);
        Assert.Equal(50m, vm.UkupnoPot);
        Assert.Equal(50m, vm.Saldo);
        Assert.Equal(10m, vm.UkupnoDevDug);
        Assert.Equal(3m, vm.UkupnoDevPot);
    }
}
