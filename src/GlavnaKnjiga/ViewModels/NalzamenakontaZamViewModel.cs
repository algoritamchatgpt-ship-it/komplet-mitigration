using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalzamenakontaZamViewModel : ObservableObject
{
    private readonly string _firmPath;

    public event Action? ZatvoriFormu;
    public event Action? DodatRed;

    [ObservableProperty] private ObservableCollection<KonzamRow> _redovi = new();
    [ObservableProperty] private KonzamRow? _selektovaniRed;

    public NalzamenakontaZamViewModel(string firmPath)
    {
        _firmPath = firmPath;
        UcitajKonzam();
    }

    private void UcitajKonzam()
    {
        var path = Path.Combine(_firmPath, "konzam.dbf");
        if (!File.Exists(path)) return;

        var rows = new List<KonzamRow>();
        foreach (var rec in new SimpleDbfReader(path).Zapisi())
        {
            rows.Add(new KonzamRow
            {
                Skonto  = rec.DajString("SKONTO").TrimEnd(),
                Deo     = rec.DajString("DEO").TrimEnd(),
                Konto   = rec.DajString("KONTO").TrimEnd(),
                Naziv   = rec.DajString("NAZIV").TrimEnd(),
                Nazkto1 = rec.DajString("NAZKTO1").TrimEnd(),
                Preneto = rec.DajString("PRENETO").TrimEnd(),
                Idbr    = rec.DajDecimal("IDBR"),
            });
        }
        Redovi = new ObservableCollection<KonzamRow>(rows);
        SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
    }

    private void SnimiKonzam()
    {
        var path = Path.Combine(_firmPath, "konzam.dbf");
        if (!File.Exists(path)) return;
        var schema = DbfTableWriter.LoadSchema(path);
        DbfTableWriter.WriteTable(path, schema, Redovi, (row, field) =>
            field.ToUpperInvariant() switch
            {
                "SKONTO"  => (object?)row.Skonto.PadRight(10),
                "DEO"     => row.Deo.PadRight(1),
                "KONTO"   => row.Konto.PadRight(10),
                "NAZIV"   => row.Naziv.PadRight(45),
                "NAZKTO1" => row.Nazkto1.PadRight(45),
                "PRENETO" => row.Preneto.PadRight(1),
                "IDBR"    => row.Idbr,
                _         => null,
            });
    }

    [RelayCommand]
    private void Dodaj()
    {
        var novi = new KonzamRow();
        Redovi.Add(novi);
        SelektovaniRed = novi;
        DodatRed?.Invoke();
    }

    [RelayCommand]
    private void BrisanjeReda()
    {
        if (SelektovaniRed == null) return;
        if (MessageBox.Show("Brisanje reda. Nastaviti?", "BRISANJE REDA",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        Redovi.Remove(SelektovaniRed);
        SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
        SnimiKonzam();
    }

    [RelayCommand]
    private void BrisanjeTabele()
    {
        if (MessageBox.Show("Brisanje KOMPLETNE tabele konta za zamenu. Nastaviti?",
                "BRISANJE TABELE", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        Redovi.Clear();
        SelektovaniRed = null;
        SnimiKonzam();
    }

    /// <summary>ZAMENI — replaces KONTO in nalnovi.dbf using the konzam mapping table</summary>
    [RelayCommand]
    private void Zameni()
    {
        if (Redovi.Count == 0)
        {
            MessageBox.Show("Nema konta za zamenu.", "ZAMENA KONTA",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var nalnoviPath = Path.Combine(_firmPath, "nalnovi.dbf");
        if (!File.Exists(nalnoviPath))
        {
            // Try nal.dbf as fallback
            nalnoviPath = Path.Combine(_firmPath, "nal.dbf");
            if (!File.Exists(nalnoviPath))
            {
                MessageBox.Show("nalnovi.dbf ni nal.dbf ne postoje.", "ZAMENA KONTA",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        if (MessageBox.Show(
                $"Biće izvršena zamena konta u '{Path.GetFileName(nalnoviPath)}' prema tabeli od {Redovi.Count} mappinga. Nastaviti?",
                "ZAMENA KONTA", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        SnimiKonzam();

        try
        {
            var schema = DbfTableWriter.LoadSchema(nalnoviPath);
            var rows = new SimpleDbfReader(nalnoviPath).Zapisi()
                .Select(Nalp2ViewModel.NalpRowFromRecord)
                .ToList();

            // Build mapping: SKONTO → KONTO
            var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var zam in Redovi)
                if (!string.IsNullOrWhiteSpace(zam.Skonto))
                    mapa[zam.Skonto.Trim()] = zam.Konto.Trim();

            var promenjeno = 0;
            foreach (var row in rows)
            {
                if (mapa.TryGetValue(row.Konto.Trim(), out var noviKonto))
                {
                    row.Konto = noviKonto;
                    promenjeno++;
                }
            }

            DbfTableWriter.WriteTable(nalnoviPath, schema, rows, Nalp2ViewModel.NalpRowFieldMapper);
            MessageBox.Show($"Zamena završena. Izmenjeno redova: {promenjeno}.",
                "ZAMENA KONTA", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri zameni konta:\n{ex.Message}", "ZAMENA KONTA",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Copies nalnovi.dbf content into nal.dbf</summary>
    [RelayCommand]
    private void KopirajUGlavnuKnjigu()
    {
        var nalPath    = Path.Combine(_firmPath, "nal.dbf");
        var nalnoviPath = Path.Combine(_firmPath, "nalnovi.dbf");

        if (!File.Exists(nalnoviPath))
        {
            MessageBox.Show("nalnovi.dbf ne postoji.", "KOPIRANJE",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show(
                "Sadržaj nalnovi.dbf biće prekopiran u nal.dbf (stari sadržaj biće obrisan). Nastaviti?",
                "KOPIRANJE GLAVNE KNJIGE U NOVU", MessageBoxButton.YesNo, MessageBoxImage.Warning) !=
            MessageBoxResult.Yes)
            return;

        try
        {
            var schema = DbfTableWriter.LoadSchema(nalPath);
            var rows = new SimpleDbfReader(nalnoviPath).Zapisi()
                .Select(Nalp2ViewModel.NalpRowFromRecord)
                .ToList();

            DbfTableWriter.WriteTable(nalPath, schema, rows, Nalp2ViewModel.NalpRowFieldMapper);
            MessageBox.Show($"Kopiranje završeno. Kopirano redova: {rows.Count}.",
                "KOPIRANJE", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri kopiranju:\n{ex.Message}", "KOPIRANJE",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand] private void IdiNaVrh() { if (Redovi.Count > 0) SelektovaniRed = Redovi[0]; }
    [RelayCommand] private void IdiNaDno()  { if (Redovi.Count > 0) SelektovaniRed = Redovi[^1]; }
    [RelayCommand] private void IdiGore()
    {
        if (SelektovaniRed == null) return;
        var idx = Redovi.IndexOf(SelektovaniRed);
        if (idx > 0) SelektovaniRed = Redovi[idx - 1];
    }
    [RelayCommand] private void IdiDole()
    {
        if (SelektovaniRed == null) return;
        var idx = Redovi.IndexOf(SelektovaniRed);
        if (idx < Redovi.Count - 1) SelektovaniRed = Redovi[idx + 1];
    }

    [RelayCommand]
    private void Izlaz()
    {
        SnimiKonzam();
        ZatvoriFormu?.Invoke();
    }
}
