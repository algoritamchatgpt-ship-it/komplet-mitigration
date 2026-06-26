using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class NalDopuniKontoViewModelTests
{
    [Theory]
    [InlineData("1200", 10, "120000")]
    [InlineData("  4330  ", 10, "433000")]
    [InlineData("1234567890", 10, "1234567890")]
    public void DopunjenoKonto_DodajeDveNuleIUvažavaŠirinuPolja(
        string konto, int sirina, string ocekivano)
    {
        Assert.Equal(ocekivano, NalDopuniKontoViewModel.DopunjenoKonto(konto, sirina));
    }
}
