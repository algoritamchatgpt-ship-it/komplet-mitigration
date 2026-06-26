using Algoritam.Domain.Entities;

namespace Algoritam.Domain.Tests;

public class KorisnikTests
{
    [Fact]
    public void Korisnik_PodrazumevaneVrednosti_PravaNijeSupervizor()
    {
        var k = new Korisnik();

        Assert.False(k.JeSupervizor);
        Assert.False(k.Aktivan);
        Assert.Equal(string.Empty, k.KorisnikIme);
    }

    [Fact]
    public void Korisnik_SupervizorFlag_MozeSePodesitiDirektno()
    {
        var k = new Korisnik { JeSupervizor = true, KorisnikIme = "admin", Aktivan = true };

        Assert.True(k.JeSupervizor);
        Assert.Equal("admin", k.KorisnikIme);
        Assert.True(k.Aktivan);
    }

    [Fact]
    public void Radnik_PodrazumevaneVrednosti_PrazniStringovi()
    {
        var r = new Radnik();

        Assert.Equal(0, r.Broj);
        Assert.Equal(string.Empty, r.ImePrezime);
        Assert.Equal(string.Empty, r.MaticniBroj);
    }

    [Fact]
    public void Radnik_PostavljanjePoljaNaBrojIIme()
    {
        var r = new Radnik { Broj = 42, ImePrezime = "PETAR PETROVIĆ", MaticniBroj = "1234567890123" };

        Assert.Equal(42, r.Broj);
        Assert.Equal("PETAR PETROVIĆ", r.ImePrezime);
        Assert.Equal("1234567890123", r.MaticniBroj);
    }
}
