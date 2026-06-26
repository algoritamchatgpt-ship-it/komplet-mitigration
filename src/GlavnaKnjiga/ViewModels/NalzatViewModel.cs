using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalzatViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly bool _aktivaIPasiva;

    public event Action? ZatvoriFormu;

    public string Naslov => _aktivaIPasiva
        ? "ZATVARANJE AKTIVE I PASIVE"
        : "ZATVARANJE KLASA 5 I 6";

    public bool PrikaziKonta => !_aktivaIPasiva;

    [ObservableProperty] private string _kontoPrihoda = "6";
    [ObservableProperty] private string _kontoRashoda = "5";
    [ObservableProperty] private DateTime? _pocetniDatum;
    [ObservableProperty] private DateTime? _zadnjiDatum;
    [ObservableProperty] private string _brojNaloga = string.Empty;
    [ObservableProperty] private string _opis = string.Empty;

    private NalzatViewModel(string firmPath, int godina, bool aktivaIPasiva)
    {
        _firmPath = firmPath;
        _aktivaIPasiva = aktivaIPasiva;
        PocetniDatum = new DateTime(godina, 1, 1);
        ZadnjiDatum = new DateTime(godina, 12, 31);
        BrojNaloga = aktivaIPasiva ? "999999" : "999992";
        Opis = aktivaIPasiva
            ? "ZATVARANJE AKTIVE I PASIVE"
            : "ZATVARANJE KLASA 5 I 6";
    }

    public static NalzatViewModel ZaKlase(string firmPath, int godina) =>
        new(firmPath, godina, false);

    public static NalzatViewModel ZaAktivuIPasivu(string firmPath, int godina) =>
        new(firmPath, godina, true);

    [RelayCommand]
    private void Zatvaranje()
    {
        if (PocetniDatum == null || ZadnjiDatum == null)
        {
            MessageBox.Show("Unesite početni i zadnji datum.", Naslov,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (PocetniDatum > ZadnjiDatum)
        {
            MessageBox.Show("Početni datum ne može biti posle zadnjeg datuma.", Naslov,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(BrojNaloga))
        {
            MessageBox.Show("Unesite broj naloga.", Naslov,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!_aktivaIPasiva &&
            (string.IsNullOrWhiteSpace(KontoPrihoda) || string.IsNullOrWhiteSpace(KontoRashoda)))
        {
            MessageBox.Show("Unesite klase prihoda i rashoda.", Naslov,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var nalPath = Dbf("nal.dbf");
        var nalapPath = Dbf("nalap.dbf");
        if (nalPath == null || nalapPath == null)
        {
            MessageBox.Show("Nedostaje nal.dbf ili nalap.dbf.", Naslov,
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (MessageBox.Show(
                $"Obrada će dodati stavke naloga {BrojNaloga.Trim()} u nalap.dbf. Nastaviti?",
                Naslov, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            var izvor = new SimpleDbfReader(nalPath).Zapisi()
                .Select(Nalp2ViewModel.NalpRowFromRecord)
                .Where(r => r.Datdok >= PocetniDatum && r.Datdok <= ZadnjiDatum)
                .ToList();

            var novi = _aktivaIPasiva
                ? FormirajZatvaranjeAktiveIPasive(izvor)
                : FormirajZatvaranjeKlasa(izvor);

            var schema = DbfTableWriter.LoadSchema(nalapPath);
            var postojeci = new SimpleDbfReader(nalapPath).Zapisi()
                .Select(Nalp2ViewModel.NalpRowFromRecord)
                .ToList();
            postojeci.AddRange(novi);

            DbfTableWriter.WriteTable(nalapPath, schema, postojeci, Nalp2ViewModel.NalpRowFieldMapper);

            MessageBox.Show($"Obrada je završena. Dodato redova u nalap.dbf: {novi.Count}.",
                Naslov, MessageBoxButton.OK, MessageBoxImage.Information);
            ZatvoriFormu?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri zatvaranju:\n{ex.Message}",
                Naslov, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    internal List<NalpRow> FormirajZatvaranjeKlasa(IEnumerable<NalpRow> izvor)
    {
        var datum = ZadnjiDatum!.Value;
        var rezultat = new List<NalpRow>();
        var rashodi = Grupisi(izvor.Where(r =>
            PocetnaKlasa(r.Konto) == KontoRashoda.Trim() &&
            !r.Konto.Trim().StartsWith("599", StringComparison.Ordinal)));
        var prihodi = Grupisi(izvor.Where(r =>
            PocetnaKlasa(r.Konto) == KontoPrihoda.Trim() &&
            !r.Konto.Trim().StartsWith("699", StringComparison.Ordinal)));

        foreach (var stavka in rashodi)
        {
            var neto = stavka.Dug - stavka.Pot;
            rezultat.Add(NoviRed(stavka.Konto, 0, neto, datum));
        }

        foreach (var stavka in prihodi)
        {
            var neto = stavka.Pot - stavka.Dug;
            rezultat.Add(NoviRed(stavka.Konto, neto, 0, datum));
        }

        var ukupnoDug = rezultat.Sum(r => r.Dug);
        var ukupnoPot = rezultat.Sum(r => r.Pot);
        rezultat.Add(NoviRed("6990", 0, ukupnoDug, datum));
        rezultat.Add(NoviRed("5990", ukupnoPot, 0, datum));
        return rezultat;
    }

    internal List<NalpRow> FormirajZatvaranjeAktiveIPasive(IEnumerable<NalpRow> izvor)
    {
        var datum = ZadnjiDatum!.Value;
        var rezultat = new List<NalpRow>();

        foreach (var stavka in Grupisi(izvor))
        {
            var neto = stavka.Dug - stavka.Pot;
            rezultat.Add(neto > 0
                ? NoviRed(stavka.Konto, 0, neto, datum)
                : NoviRed(stavka.Konto, Math.Abs(neto), 0, datum));
        }

        var ukupnoDug = rezultat.Sum(r => r.Dug);
        var ukupnoPot = rezultat.Sum(r => r.Pot);
        rezultat.Add(NoviRed("7300", 0, ukupnoDug, datum));
        rezultat.Add(NoviRed("7300", ukupnoPot, 0, datum));
        return rezultat;
    }

    private NalpRow NoviRed(string konto, decimal dug, decimal pot, DateTime datum) => new()
    {
        Konto = konto,
        Dug = dug,
        Pot = pot,
        Datdok = datum,
        Brnal = BrojNaloga.Trim(),
        Opis = Opis.Trim(),
        Datum = DateTime.Today,
        Vreme = DateTime.Now.ToString("HH:mm:ss"),
    };

    private static List<SaldoKonta> Grupisi(IEnumerable<NalpRow> rows) =>
        rows.GroupBy(r => r.Konto.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Select(g => new SaldoKonta(g.Key, g.Sum(r => r.Dug), g.Sum(r => r.Pot)))
            .OrderBy(r => r.Konto, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string PocetnaKlasa(string konto)
    {
        var vrednost = konto.Trim();
        return vrednost.Length == 0 ? string.Empty : vrednost[..1];
    }

    private string? Dbf(string name)
    {
        var path = Path.Combine(_firmPath, name);
        return File.Exists(path) ? path : null;
    }

    private sealed record SaldoKonta(string Konto, decimal Dug, decimal Pot);
}
