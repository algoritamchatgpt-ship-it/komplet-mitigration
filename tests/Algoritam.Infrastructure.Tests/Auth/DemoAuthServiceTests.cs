using Algoritam.Infrastructure.Services;

namespace Algoritam.Infrastructure.Tests.Auth;

public class DemoAuthServiceTests
{
    private readonly DemoAuthService _svc = new();

    [Fact]
    public async Task PrijavaAsync_TacniPodaci_VracaKorisnika()
    {
        var korisnik = await _svc.PrijavaAsync("admin", "admin");

        Assert.NotNull(korisnik);
        Assert.Equal("admin", korisnik.KorisnikIme);
    }

    [Fact]
    public async Task PrijavaAsync_PogresnаLozinka_VracaNull()
    {
        var korisnik = await _svc.PrijavaAsync("admin", "pogresna");

        Assert.Null(korisnik);
    }

    [Fact]
    public async Task PrijavaAsync_NepostojeciKorisnik_VracaNull()
    {
        var korisnik = await _svc.PrijavaAsync("nepostoji", "admin");

        Assert.Null(korisnik);
    }

    [Theory]
    [InlineData("ADMIN", "admin")]   // case-insensitive korisnik
    [InlineData("Admin", "admin")]
    public async Task PrijavaAsync_KorisnikImeNijeCaseSensitive(string ime, string lozinka)
    {
        var korisnik = await _svc.PrijavaAsync(ime, lozinka);

        Assert.NotNull(korisnik);
    }

    [Fact]
    public async Task PrijavaAsync_AdminKorisnik_ImaSupervizorPravo()
    {
        var korisnik = await _svc.PrijavaAsync("admin", "admin");

        Assert.NotNull(korisnik);
        Assert.True(korisnik.JeSupervizor);
    }

    [Fact]
    public async Task PrijavaAsync_ObicanKorisnik_NijeSupervizor()
    {
        var korisnik = await _svc.PrijavaAsync("korisnik", "korisnik");

        Assert.NotNull(korisnik);
        Assert.False(korisnik.JeSupervizor);
    }

    [Fact]
    public async Task PrijavaAsync_AdminKorisnik_ImaGKPristup()
    {
        var korisnik = await _svc.PrijavaAsync("admin", "admin");

        Assert.NotNull(korisnik);
        Assert.True(korisnik.PassGk);
        Assert.True(korisnik.PassAn);
        Assert.True(korisnik.PassBl);
    }

    [Fact]
    public async Task PrijavaAsync_ObicanKorisnik_NemaBlagajnu()
    {
        var korisnik = await _svc.PrijavaAsync("korisnik", "korisnik");

        Assert.NotNull(korisnik);
        Assert.False(korisnik.PassBl);
        Assert.False(korisnik.PassTv);
    }

    [Fact]
    public async Task PrijavaAsync_PraznoIme_VracaNull()
    {
        var korisnik = await _svc.PrijavaAsync("", "admin");

        Assert.Null(korisnik);
    }
}
