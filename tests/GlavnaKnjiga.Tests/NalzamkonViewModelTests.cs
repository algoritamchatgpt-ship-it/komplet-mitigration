using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class NalzamkonViewModelTests
{
    [Fact]
    public void ZameniKonto_MenjaSamoPotpunoJednakeKonte()
    {
        var rows = new List<NalpRow>
        {
            new() { Konto = "5000" },
            new() { Konto = "5000 " },
            new() { Konto = "50001" },
            new() { Konto = "6000" },
        };

        var promenjeno = NalzamkonViewModel.ZameniKonto(rows, "5000", "5100");

        Assert.Equal(2, promenjeno);
        Assert.Equal("5100", rows[0].Konto);
        Assert.Equal("5100", rows[1].Konto);
        Assert.Equal("50001", rows[2].Konto);
        Assert.Equal("6000", rows[3].Konto);
    }

    [Fact]
    public void ZameniKonto_VracaNulaKadaKontoNePostoji()
    {
        var rows = new List<NalpRow> { new() { Konto = "1000" } };

        var promenjeno = NalzamkonViewModel.ZameniKonto(rows, "2000", "2100");

        Assert.Equal(0, promenjeno);
        Assert.Equal("1000", rows[0].Konto);
    }
}
