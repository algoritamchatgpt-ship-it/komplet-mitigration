using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class NalslagViewModelTests
{
    [Fact]
    public void FormirajPregled_PodrazumevanoVracaSamoNeslozeneNalogeUPeriodu()
    {
        var vm = new NalslagViewModel("C:\\ne-koristi-se", 2026);
        var izvor = PrimerRedova();

        var rezultat = vm.FormirajPregled(izvor);

        var red = Assert.Single(rezultat);
        Assert.Equal("000002", red.Brnal);
        Assert.Equal(40m, red.Dug);
        Assert.Equal(10m, red.Pot);
        Assert.Equal(30m, red.Razlika);
        Assert.False(red.JeSlozen);
    }

    [Fact]
    public void FormirajPregled_KadaJePrikaziSveUkljucenVracaISlozeneNaloge()
    {
        var vm = new NalslagViewModel("C:\\ne-koristi-se", 2026)
        {
            PrikaziSve = true,
        };

        var rezultat = vm.FormirajPregled(PrimerRedova());

        Assert.Collection(rezultat,
            r =>
            {
                Assert.Equal("000001", r.Brnal);
                Assert.True(r.JeSlozen);
                Assert.Equal(100m, r.Dug);
                Assert.Equal(100m, r.Pot);
            },
            r =>
            {
                Assert.Equal("000002", r.Brnal);
                Assert.False(r.JeSlozen);
            });
    }

    private static NalpRow[] PrimerRedova() =>
    [
        new() { Brnal = "000001", Datdok = new DateTime(2026, 2, 1), Dug = 100m, Pot = 50m },
        new() { Brnal = "000001", Datdok = new DateTime(2026, 2, 2), Dug = 0m, Pot = 50m },
        new() { Brnal = "000002", Datdok = new DateTime(2026, 3, 1), Dug = 40m, Pot = 10m },
        new() { Brnal = "000003", Datdok = new DateTime(2025, 12, 31), Dug = 50m, Pot = 0m },
    ];
}
