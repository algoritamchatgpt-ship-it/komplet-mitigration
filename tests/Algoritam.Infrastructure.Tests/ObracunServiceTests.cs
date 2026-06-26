using Algoritam.Domain.Entities;
using Algoritam.Infrastructure.Services;

namespace Algoritam.Infrastructure.Tests;

public class ObracunServiceTests
{
    [Fact]
    public void Obracunaj_StandardniObracun_PraznikJeBezProcentaPoFoxu()
    {
        var service = new ObracunService();
        var param = KreirajParametre();
        var radnik = KreirajRadnika(startniBodovi: 1m);
        var stavka = KreirajStavku();

        service.Obracunaj(stavka, radnik, param, obracunatiNaknade: true);

        Assert.Equal(50000m, stavka.Dinpraz);
        Assert.Equal(32500m, stavka.Dinbol);
        Assert.Equal(22500m, stavka.Dinplac);
    }

    [Fact]
    public void Obracunaj_DefaultnoNeObracunavaNaknade_CuvaPostojeceVrednosti()
    {
        var service = new ObracunService();
        var param = KreirajParametre();
        var radnik = KreirajRadnika(startniBodovi: 1m);
        var stavka = KreirajStavku();

        stavka.Dinpraz = 5000m;
        stavka.Dinbol = 3250m;
        stavka.Dinplac = 2250m;
        stavka.Dingod = 5000m;

        service.Obracunaj(stavka, radnik, param);

        Assert.Equal(5000m, stavka.Dinpraz);
        Assert.Equal(3250m, stavka.Dinbol);
        Assert.Equal(2250m, stavka.Dinplac);
        Assert.Equal(5000m, stavka.Dingod);
    }

    [Fact]
    public void Obracunaj_ObracunOdUkupneObaveze_PraznikJeBezProcentaPoFoxu()
    {
        var service = new ObracunService();
        var param = KreirajParametre();
        var radnik = KreirajRadnika(startniBodovi: 1_000_000m);
        var stavka = KreirajStavku();

        service.Obracunaj(
            stavka,
            radnik,
            param,
            obracunatiNaknade: true,
            obracunOdUkupneObaveze: true);

        var satnica = stavka.Casuc == 0 ? 0m : stavka.Dinuc / stavka.Casuc;
        var ocekivaniPraznik = Math.Round(stavka.Caspraz * satnica, 2);
        Assert.Equal(ocekivaniPraznik, stavka.Dinpraz);
    }

    private static LdParametar KreirajParametre() =>
        new()
        {
            Godina = "2026",
            Mesec = 2,
            Isplata = 1,
            Redispl = 1,
            Czakon = 160,
            Cenarada = 1_000_000m,
            Procpraz = 110m,
            Procbol = 65m,
            Procplac = 45m,
            Procmin = 0m,
            Ekoefs = 1m,
            Decimale = "2",
            Nakpos = "NEMA",
            Procpor = 10m
        };

    private static Radnik KreirajRadnika(decimal startniBodovi) =>
        new()
        {
            Broj = 1,
            ImePrezime = "PETAR PETROVIC",
            EvidencijskiBroj = "1",
            MaticniBroj = "1111111111111",
            IdBroj = "111111111",
            StartniBodovi = startniBodovi,
            Staz = 2
        };

    private static LdObracunStavka KreirajStavku() =>
        new()
        {
            Broj = 1,
            Casuc = 144m,
            Casprod = 8m,
            Casradnap = 8m,
            Caspraz = 8m,
            Casbol = 8m,
            Casplac = 8m,
            Casgod = 8m
        };
}
