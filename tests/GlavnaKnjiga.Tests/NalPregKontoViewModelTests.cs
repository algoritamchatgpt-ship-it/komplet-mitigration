using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;

namespace GlavnaKnjiga.Tests;

public class NalPregKontoViewModelTests
{
    [Fact]
    public void FormirajPregled_DodajePocetnoStanjeIVodiKumulativniSaldo()
    {
        var redovi = new[]
        {
            new NalpRow
            {
                Konto = "2020000001", Datdok = new DateTime(2025, 12, 20),
                Dug = 100, Opis = "Početno",
            },
            new NalpRow
            {
                Konto = "2020000001", Datdok = new DateTime(2026, 1, 10),
                Pot = 30, Brnal = "1",
            },
            new NalpRow
            {
                Konto = "2020000002", Datdok = new DateTime(2026, 1, 11),
                Dug = 10, Brnal = "2",
            },
            new NalpRow
            {
                Konto = "4350000000", Datdok = new DateTime(2026, 1, 12),
                Pot = 999, Brnal = "3",
            },
        };

        var rezultat = Formiraj(redovi, konto: "2020");

        Assert.Equal(3, rezultat.Count);
        Assert.Equal("POČETNO STANJE", rezultat[0].Opis);
        Assert.Equal(100, rezultat[0].Dpsaldo);
        Assert.Equal(70, rezultat[1].Dpsaldo);
        Assert.Equal(80, rezultat[2].Dpsaldo);
    }

    [Fact]
    public void FormirajPregled_GrupisePoDanimaIPodrzavaPadajuciSaldo()
    {
        var redovi = new[]
        {
            new NalpRow
            {
                Konto = "2020", Datdok = new DateTime(2025, 12, 31), Dug = 50,
            },
            new NalpRow
            {
                Konto = "2020", Datdok = new DateTime(2026, 1, 5), Pot = 10,
            },
            new NalpRow
            {
                Konto = "2020", Datdok = new DateTime(2026, 1, 5), Dug = 5,
            },
            new NalpRow
            {
                Konto = "2020", Datdok = new DateTime(2026, 1, 6), Dug = 20,
            },
        };

        var rezultat = Formiraj(
            redovi,
            konto: "2020",
            saldoPoDanima: "D",
            padajuciSaldo: "D");

        Assert.Equal(3, rezultat.Count);
        Assert.Equal(45, rezultat[1].Dpsaldo);
        Assert.Equal(65, rezultat[2].Dpsaldo);
        Assert.Equal("SALDO PO DANU", rezultat[2].Opis);
    }

    [Fact]
    public void FormirajPregled_FiltriraSmerIUklanjaSlicneStavke()
    {
        var redovi = new[]
        {
            new NalpRow
            {
                Konto = "2020", Datdok = new DateTime(2026, 1, 2), Dug = 100,
            },
            new NalpRow
            {
                Konto = "2020", Datdok = new DateTime(2026, 1, 3), Pot = 102,
            },
            new NalpRow
            {
                Konto = "2020", Datdok = new DateTime(2026, 1, 4), Dug = 40,
            },
        };

        var bezSlicnih = Formiraj(
            redovi,
            konto: "2020",
            izbaciSlicne: "D",
            tolerancija: 5);
        var samoDuguje = Formiraj(
            redovi,
            konto: "2020",
            dugPotSve: "D");

        var preostali = Assert.Single(bezSlicnih);
        Assert.Equal(40, preostali.Dug);
        Assert.Equal(2, samoDuguje.Count);
        Assert.All(samoDuguje, r => Assert.NotEqual(0, r.Dug));
    }

    private static IReadOnlyList<NalpRow> Formiraj(
        IEnumerable<NalpRow> redovi,
        string konto,
        string saldoPoDanima = "N",
        string padajuciSaldo = "N",
        string saldoPoMesecima = "N",
        string dugPotSve = "S",
        string sortirano = "N",
        string izbaciSlicne = "N",
        decimal tolerancija = 5) =>
        NalPregKontoViewModel.FormirajPregled(
            redovi,
            konto,
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31),
            "",
            "",
            "0",
            "",
            saldoPoDanima,
            padajuciSaldo,
            saldoPoMesecima,
            dugPotSve,
            sortirano,
            izbaciSlicne,
            tolerancija);
}
