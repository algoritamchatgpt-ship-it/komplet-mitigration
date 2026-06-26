using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class KontoPlanViewModelTests
{
    [Fact]
    public void Filtriraj_KombinujePocetakKontaISadrzajNaziva()
    {
        var vm = new KontoPlanViewModel(
            "C:\\folder-koji-ne-postoji", "konto.dbf", "KONTNI PLAN");
        var rows = new[]
        {
            new KontoPlanRow { Konto = "5000", Naziv = "Troškovi materijala" },
            new KontoPlanRow { Konto = "5010", Naziv = "Nabavna vrednost" },
            new KontoPlanRow { Konto = "6000", Naziv = "Prihodi od prodaje" },
        };

        var rezultat = vm.Filtriraj(rows, "50", "mater");

        var red = Assert.Single(rezultat);
        Assert.Equal("5000", red.Konto);
    }

    [Fact]
    public void Filtriraj_BezUslovaSortiraPoKontu()
    {
        var vm = new KontoPlanViewModel(
            "C:\\folder-koji-ne-postoji", "konto.dbf", "KONTNI PLAN");
        var rows = new[]
        {
            new KontoPlanRow { Konto = "6000" },
            new KontoPlanRow { Konto = "1000" },
        };

        var rezultat = vm.Filtriraj(rows, "", "");

        Assert.Equal(new[] { "1000", "6000" },
            rezultat.Select(r => r.Konto).ToArray());
    }
}
