using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class Nalrezul6ViewModel : ObservableObject
{
    private const string Naslov = "UTVRĐIVANJE REZULTATA";
    private readonly string _firmPath;

    public event Action? ZatvoriFormu;

    [ObservableProperty] private string _kontoPrihoda = "6";
    [ObservableProperty] private string _kontoRashoda = "5";
    [ObservableProperty] private DateTime? _pocetniDatum;
    [ObservableProperty] private DateTime? _zadnjiDatum;
    [ObservableProperty] private string _brojNaloga = "999991";
    [ObservableProperty] private string _opis = Naslov;

    public Nalrezul6ViewModel(string firmPath, int godina)
    {
        _firmPath = firmPath;
        PocetniDatum = new DateTime(godina, 1, 1);
        ZadnjiDatum = new DateTime(godina, 12, 31);
    }

    [RelayCommand]
    private void Utvrdjivanje()
    {
        if (!Validiraj()) return;

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

            var novi = FormirajRezultat(izvor);
            var schema = DbfTableWriter.LoadSchema(nalapPath);
            var postojeci = new SimpleDbfReader(nalapPath).Zapisi()
                .Select(Nalp2ViewModel.NalpRowFromRecord)
                .ToList();
            postojeci.AddRange(novi);

            DbfTableWriter.WriteTable(
                nalapPath, schema, postojeci, Nalp2ViewModel.NalpRowFieldMapper);
            SacuvajDatumskiPeriod();

            MessageBox.Show($"Utvrđivanje je završeno. Dodato redova: {novi.Count}.",
                Naslov, MessageBoxButton.OK, MessageBoxImage.Information);
            ZatvoriFormu?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri utvrđivanju rezultata:\n{ex.Message}",
                Naslov, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    internal List<NalpRow> FormirajRezultat(IEnumerable<NalpRow> izvor)
    {
        var relevantni = izvor.Where(r =>
        {
            var konto = r.Konto.Trim();
            return PocetnaKlasa(konto) == KontoRashoda.Trim() ||
                   PocetnaKlasa(konto) == KontoPrihoda.Trim() ||
                   konto.StartsWith("721", StringComparison.Ordinal) ||
                   konto.StartsWith("722", StringComparison.Ordinal) ||
                   konto.StartsWith("723", StringComparison.Ordinal);
        });

        var salda = relevantni
            .GroupBy(r => r.Konto.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .Select(g => new SaldoKonta(g.Key, g.Sum(r => r.Dug), g.Sum(r => r.Pot)))
            .ToList();

        decimal rashodi599 = 0;
        decimal prihodi699 = 0;
        decimal saldo721 = 0;
        decimal saldo722 = 0;
        decimal saldo723 = 0;

        foreach (var saldo in salda)
        {
            // NALRAZ16.PRG posle početnog filtera koristi fiksne klase 5 i 6.
            if (PocetnaKlasa(saldo.Konto) == "5")
            {
                rashodi599 += saldo.Dug - saldo.Pot;
                if (saldo.Konto.StartsWith("599", StringComparison.Ordinal))
                    rashodi599 -= saldo.Dug - saldo.Pot;
            }

            if (PocetnaKlasa(saldo.Konto) == "6")
            {
                prihodi699 += saldo.Pot - saldo.Dug;

                // Originalni NALRAZ16.PRG konto 699 sabira još jednom.
                // Ovo ponašanje je zadržano radi potpune kompatibilnosti.
                if (saldo.Konto.StartsWith("699", StringComparison.Ordinal))
                    prihodi699 += saldo.Pot - saldo.Dug;
            }

            if (saldo.Konto.StartsWith("721", StringComparison.Ordinal))
                saldo721 += saldo.Dug - saldo.Pot;
            if (saldo.Konto.StartsWith("722", StringComparison.Ordinal))
                saldo722 += saldo.Dug - saldo.Pot;
            if (saldo.Konto.StartsWith("723", StringComparison.Ordinal))
                saldo723 += saldo.Dug - saldo.Pot;
        }

        var rezultat = new List<NalpRow>
        {
            NoviRed("5990", 0, rashodi599),
            NoviRed("7100", rashodi599, 0),
            NoviRed("6990", prihodi699, 0),
            NoviRed("7100", 0, prihodi699),
        };

        var saldo712 = prihodi699 - rashodi599;
        rezultat.Add(saldo712 > 0
            ? NoviRed("7100", saldo712, 0)
            : NoviRed("7100", 0, -saldo712));

        rezultat.Add(saldo712 > 0
            ? NoviRed("7120", 0, saldo712)
            : NoviRed("7120", -saldo712, 0));
        rezultat.Add(saldo712 > 0
            ? NoviRed("7120", saldo712, 0)
            : NoviRed("7120", 0, -saldo712));

        var saldo720 = saldo712;
        rezultat.Add(saldo720 > 0
            ? NoviRed("7200", 0, saldo720)
            : NoviRed("7200", -saldo720, 0));
        rezultat.Add(saldo720 > 0
            ? NoviRed("7200", saldo720, 0)
            : NoviRed("7200", 0, -saldo720));

        // FoxPro za 721 i 723 popunjava samo POT kada je saldo pozitivan.
        rezultat.Add(NoviRed("7210", 0, saldo721 > 0 ? saldo721 : 0));
        rezultat.Add(NoviRed("7230", 0, saldo723 > 0 ? saldo723 : 0));
        rezultat.Add(saldo722 > 0
            ? NoviRed("7220", 0, saldo722)
            : NoviRed("7220", -saldo722, 0));

        var saldo724 = saldo720 - saldo721 - saldo722 - saldo723;
        rezultat.Add(saldo724 > 0
            ? NoviRed("7240", 0, saldo724)
            : NoviRed("7240", -saldo724, 0));
        rezultat.Add(saldo724 > 0
            ? NoviRed("7240", saldo724, 0)
            : NoviRed("7240", 0, -saldo724));
        rezultat.Add(saldo724 > 0
            ? NoviRed("3410", 0, saldo724)
            : NoviRed("3510", -saldo724, 0));

        return rezultat;
    }

    private bool Validiraj()
    {
        if (PocetniDatum == null || ZadnjiDatum == null)
        {
            MessageBox.Show("Unesite početni i zadnji datum.", Naslov,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (PocetniDatum > ZadnjiDatum)
        {
            MessageBox.Show("Početni datum ne može biti posle zadnjeg datuma.", Naslov,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(KontoPrihoda) ||
            string.IsNullOrWhiteSpace(KontoRashoda))
        {
            MessageBox.Show("Unesite klase prihoda i rashoda.", Naslov,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(BrojNaloga))
        {
            MessageBox.Show("Unesite broj naloga.", Naslov,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    private NalpRow NoviRed(string konto, decimal dug, decimal pot) => new()
    {
        Konto = konto,
        Dug = dug,
        Pot = pot,
        Datdok = ZadnjiDatum,
        Brnal = BrojNaloga.Trim(),
        Opis = Opis.Trim(),
        Datum = DateTime.Today,
        Vreme = DateTime.Now.ToString("HH:mm:ss"),
    };

    private void SacuvajDatumskiPeriod()
    {
        var path = Dbf("datumi.dbf");
        if (path == null) return;

        var schema = DbfTableWriter.LoadSchema(path);
        var rows = new List<Dictionary<string, object?>>();
        foreach (var rec in new SimpleDbfReader(path, schema.Encoding).Zapisi())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in schema.Fields)
            {
                row[field.Name] = field.Type switch
                {
                    'D' => rec.DajDate(field.Name),
                    'N' or 'F' => rec.DajDecimal(field.Name),
                    'L' => rec.DajBool(field.Name),
                    _ => rec.DajString(field.Name),
                };
            }
            rows.Add(row);
        }

        if (rows.Count == 0) return;
        rows[0]["DAT0"] = PocetniDatum;
        rows[0]["DAT1"] = ZadnjiDatum;
        DbfTableWriter.WriteTable(path, schema, rows,
            (row, field) => row.TryGetValue(field, out var value) ? value : null);
    }

    private static string PocetnaKlasa(string konto) =>
        string.IsNullOrWhiteSpace(konto) ? string.Empty : konto.Trim()[..1];

    private string? Dbf(string name)
    {
        var path = Path.Combine(_firmPath, name);
        return File.Exists(path) ? path : null;
    }

    private sealed record SaldoKonta(string Konto, decimal Dug, decimal Pot);
}
