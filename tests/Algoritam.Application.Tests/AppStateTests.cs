using Algoritam.Application;
using Algoritam.Domain.Entities;

namespace Algoritam.Application.Tests;

public class AppStateTests
{
    [Fact]
    public void Prijavi_Postavlja_TrenutniKorisnik()
    {
        var state = new AppState();
        var korisnik = new Korisnik { KorisnikIme = "admin", JeSupervizor = true };

        state.Prijavi(korisnik);

        Assert.True(state.JePrijavljen);
        Assert.Equal("admin", state.TrenutniKorisnik?.KorisnikIme);
    }

    [Fact]
    public void Odjavi_Brise_TrenutniKorisnik()
    {
        var state = new AppState();
        state.Prijavi(new Korisnik { KorisnikIme = "admin" });

        state.Odjavi();

        Assert.False(state.JePrijavljen);
        Assert.Null(state.TrenutniKorisnik);
    }

    [Fact]
    public void JeSupervizor_BezPrijave_VracaFalse()
    {
        var state = new AppState();

        Assert.False(state.JeSupervizor);
    }

    [Fact]
    public void JeSupervizor_SupervizorKorisnik_VracaTrue()
    {
        var state = new AppState();
        state.Prijavi(new Korisnik { JeSupervizor = true });

        Assert.True(state.JeSupervizor);
    }

    [Fact]
    public void PodrazumevanaGodina_JeTekaGodina()
    {
        var state = new AppState();

        Assert.Equal(DateTime.Now.Year, state.AktivnaGodina);
    }

    [Fact]
    public void PostaviFirmu_Postavlja_AktivnaFirma()
    {
        var state = new AppState();
        var firma = new Algoritam.Domain.Entities.Firma { Id = 1, Naziv = "Test firma" };

        state.PostaviFirmu(firma);

        Assert.NotNull(state.AktivnaFirma);
        Assert.Equal(1, state.AktivnaFirma.Id);
    }

    [Fact]
    public void Odjavi_Brise_AktivnaFirma()
    {
        var state = new AppState();
        state.PostaviFirmu(new Algoritam.Domain.Entities.Firma { Id = 2 });
        state.Prijavi(new Algoritam.Domain.Entities.Korisnik());

        state.Odjavi();

        Assert.Null(state.AktivnaFirma);
    }
}
