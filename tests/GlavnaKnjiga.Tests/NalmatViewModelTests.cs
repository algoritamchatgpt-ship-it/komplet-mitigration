using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class NalmatViewModelTests
{
    [Fact]
    public void FormirajPrijem_UzimaSveRedoveKontaKojeImaMaterijalniPromet()
    {
        var izvor = new[]
        {
            new NalpRow { Konto = "1000", Ulaz = 10m, Datdok = new DateTime(2026, 1, 1) },
            new NalpRow { Konto = "1000", Dug = 25m, Datdok = new DateTime(2026, 1, 2) },
            new NalpRow { Konto = "2000", Dug = 40m, Datdok = new DateTime(2026, 1, 1) },
            new NalpRow { Konto = "3000", Izlaz = 3m, Datdok = new DateTime(2026, 1, 1) },
        };

        var rezultat = NalmatViewModel.FormirajPrijem(izvor);

        Assert.Equal(3, rezultat.Count);
        Assert.Equal(new[] { "1000", "1000", "3000" },
            rezultat.Select(r => r.Konto.Trim()).ToArray());
    }

    [Fact]
    public void SrediCene_IzlazVrednujePoDotadasnjojProsecnojCeni()
    {
        var rows = new List<NalmatRow>
        {
            new() { Konto = "1000", Datdok = new DateTime(2026, 1, 1), Ulaz = 10m, Cena = 5m },
            new() { Konto = "1000", Datdok = new DateTime(2026, 1, 2), Ulaz = 10m, Cena = 7m },
            new() { Konto = "1000", Datdok = new DateTime(2026, 1, 3), Izlaz = 4m, Cena = 99m },
        };

        NalmatViewModel.SrediCene(rows);

        Assert.Equal(50m, rows[0].UkupnoD);
        Assert.Equal(70m, rows[1].UkupnoD);
        Assert.Equal(6m, rows[2].Cena);
        Assert.Equal(24m, rows[2].UkupnoP);
    }

    [Fact]
    public void FormirajKarticu_RacunaKumulativnoStanjeISaldo()
    {
        var rows = new[]
        {
            new NalmatRow { Konto = "1000", Datdok = new DateTime(2026, 1, 1), Ulaz = 10m, UkupnoD = 50m },
            new NalmatRow { Konto = "1000", Datdok = new DateTime(2026, 1, 2), Izlaz = 4m, UkupnoP = 20m },
        };

        var rezultat = NalmatViewModel.FormirajKarticu(rows, "1000");

        Assert.Equal(10m, rezultat[0].Stanje);
        Assert.Equal(50m, rezultat[0].Saldo);
        Assert.Equal(6m, rezultat[1].Stanje);
        Assert.Equal(30m, rezultat[1].Saldo);
    }

    [Fact]
    public void FormirajSaldo_LagerRacunaKolicinuVrednostIProsecnuCenu()
    {
        var rows = new[]
        {
            new NalmatRow { Konto = "1000", Ulaz = 10m, UkupnoD = 50m },
            new NalmatRow { Konto = "1000", Izlaz = 4m, UkupnoP = 20m },
        };

        var red = Assert.Single(NalmatViewModel.FormirajSaldo(rows, true));

        Assert.Equal(6m, red.Stanje);
        Assert.Equal(30m, red.Saldo);
        Assert.Equal(5m, red.Cena);
    }

    [Fact]
    public void FormirajNalogOdstupanja_PraviFoxProKorektivneStavke()
    {
        var rows = new[]
        {
            new NalmatRow { Konto = "1000", Dug = 100m, UkupnoD = 90m, Pot = 0m, UkupnoP = 0m },
            new NalmatRow { Konto = "2000", Dug = 0m, UkupnoD = 0m, Pot = 80m, UkupnoP = 75m },
        };

        var rezultat = NalmatViewModel.FormirajNalogOdstupanja(
            rows, new DateTime(2026, 12, 31));

        Assert.Collection(rezultat,
            r =>
            {
                Assert.Equal("1000", r.Konto);
                Assert.Equal(-10m, r.Dug);
                Assert.Equal(0m, r.Pot);
            },
            r =>
            {
                Assert.Equal("2000", r.Konto);
                Assert.Equal(0m, r.Dug);
                Assert.Equal(-5m, r.Pot);
            });
        Assert.All(rezultat, r =>
        {
            Assert.Equal("888888", r.Brnal);
            Assert.Equal("SLAGANJE MATERIJALA", r.Opis);
        });
    }
}
