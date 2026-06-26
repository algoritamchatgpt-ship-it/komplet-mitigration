using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalgruViewModel : ObservableObject
{
    private readonly string _firmPath;

    public event Action? ZatvoriFormu;
    public event Action? DodatRed;

    [ObservableProperty] private ObservableCollection<NalgruRow> _redovi = new();
    [ObservableProperty] private NalgruRow? _selektovaniRed;
    [ObservableProperty] private string _naslov = "PREGLED GRUPE KONTA";

    public NalgruViewModel(string firmPath)
    {
        _firmPath = firmPath;
        UcitajNalgru();
    }

    private void UcitajNalgru()
    {
        var path = Path.Combine(_firmPath, "nalgru.dbf");
        if (!File.Exists(path)) return;

        var rows = new List<NalgruRow>();
        foreach (var rec in new SimpleDbfReader(path).Zapisi())
        {
            rows.Add(new NalgruRow
            {
                Konto   = rec.DajString("KONTO").TrimEnd(),
                Preneto = rec.DajString("PRENETO").TrimEnd(),
                Idbr    = rec.DajDecimal("IDBR"),
            });
        }
        Redovi = new ObservableCollection<NalgruRow>(rows);
        SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
    }

    private void SnimiNalgru()
    {
        var path = Path.Combine(_firmPath, "nalgru.dbf");
        if (!File.Exists(path)) return;

        var schema = DbfTableWriter.LoadSchema(path);
        DbfTableWriter.WriteTable(path, schema, Redovi, (row, field) =>
            field.ToUpperInvariant() switch
            {
                "KONTO"   => (object?)row.Konto.PadRight(10),
                "PRENETO" => row.Preneto.PadRight(1),
                "IDBR"    => row.Idbr,
                _         => null,
            });
    }

    [RelayCommand]
    private void Dodaj()
    {
        var novi = new NalgruRow();
        Redovi.Add(novi);
        SelektovaniRed = novi;
        DodatRed?.Invoke();
    }

    [RelayCommand]
    private void Brisanje()
    {
        if (SelektovaniRed == null) return;
        if (MessageBox.Show("Brisanje reda. Nastaviti?", "BRISANJE",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        Redovi.Remove(SelektovaniRed);
        SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
        SnimiNalgru();
    }

    [RelayCommand]
    private void Pregled()
    {
        var kontoList = Redovi
            .Where(r => !string.IsNullOrWhiteSpace(r.Konto))
            .Select(r => r.Konto.Trim())
            .ToList();

        if (kontoList.Count == 0)
        {
            MessageBox.Show("Nema konta u listi grupe.", "PREGLED GRUPE KONTA",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var nalPath = Path.Combine(_firmPath, "nal.dbf");
        if (!File.Exists(nalPath))
        {
            MessageBox.Show("nal.dbf ne postoji.", "PREGLED GRUPE KONTA",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var rows = new SimpleDbfReader(nalPath).Zapisi()
            .Select(Nalp2ViewModel.NalpRowFromRecord)
            .Where(r => kontoList.Contains(r.Konto.Trim(), StringComparer.OrdinalIgnoreCase))
            .ToList();

        var ukDug = rows.Sum(r => r.Dug);
        var ukPot = rows.Sum(r => r.Pot);

        MessageBox.Show(
            $"Kartice za grupu konta — {kontoList.Count} konta:\n" +
            $"Pronađeno redova: {rows.Count}\n" +
            $"Ukupno DUG: {ukDug:N2}   POT: {ukPot:N2}   Saldo: {ukDug - ukPot:N2}\n\n" +
            $"[Štampa — NALGRU0.FRX nije implementirana]",
            "PREGLED GRUPE KONTA",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand] private void IdiGore()
    {
        if (SelektovaniRed == null || Redovi.Count == 0) return;
        var idx = Redovi.IndexOf(SelektovaniRed);
        if (idx > 0) SelektovaniRed = Redovi[idx - 1];
    }

    [RelayCommand] private void IdiDole()
    {
        if (SelektovaniRed == null || Redovi.Count == 0) return;
        var idx = Redovi.IndexOf(SelektovaniRed);
        if (idx < Redovi.Count - 1) SelektovaniRed = Redovi[idx + 1];
    }

    [RelayCommand] private void IdiNaVrh()
    {
        if (Redovi.Count > 0) SelektovaniRed = Redovi[0];
    }

    [RelayCommand] private void IdiNaDno()
    {
        if (Redovi.Count > 0) SelektovaniRed = Redovi[^1];
    }

    [RelayCommand]
    private void Izlaz()
    {
        SnimiNalgru();
        ZatvoriFormu?.Invoke();
    }
}
