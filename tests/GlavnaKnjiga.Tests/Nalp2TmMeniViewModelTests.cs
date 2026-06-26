using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class Nalp2TmMeniViewModelTests
{
    [Theory]
    [InlineData("UPLACENO GOTOVINA", "UPLACENO GOTOVINA 05.03")]
    [InlineData("UPLACENO CEKOVI", "UPLACENO CEKOVI  05.03")]
    [InlineData("UPLACENO KARTICA", "UPLACENO KARTICA 05.03")]
    [InlineData("UPLACENO VIRMAN", "UPLACENO VIRMAN 05.03")]
    [InlineData("UPLACENO OSTALO", "UPLACENO OSTALO 05.03")]
    public void FormirajOpis_DodajeDanIMesec(
        string opcija, string ocekivano)
    {
        var rezultat = Nalp2TmMeniViewModel.FormirajOpis(
            opcija, new DateTime(2026, 3, 5), out var prazniOpis);

        Assert.False(prazniOpis);
        Assert.Equal(ocekivano, rezultat);
    }

    [Fact]
    public void FormirajOpis_IzlazSignaliziraBrisanjeOpisaKnjizenja()
    {
        var rezultat = Nalp2TmMeniViewModel.FormirajOpis(
            "IZLAZ", DateTime.Today, out var prazniOpis);

        Assert.True(prazniOpis);
        Assert.Null(rezultat);
    }
}
