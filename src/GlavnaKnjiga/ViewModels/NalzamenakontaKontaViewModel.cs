using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>
/// Generic editor for kon1n/kon2n/kon3n/konton.dbf — used by nalzamenakonta01/02/03/10.
/// </summary>
public partial class NalzamenakontaKontaViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly string _tableName;    // e.g. "kon1n"
    private readonly string _codeField;    // e.g. "K1"
    private readonly string _oldTable;     // e.g. "kon1" — target for Zamena

    public string Naslov { get; }

    public event Action? ZatvoriFormu;
    public event Action? DodatRed;

    [ObservableProperty] private ObservableCollection<KonNovRow> _redovi = new();
    [ObservableProperty] private KonNovRow? _selektovaniRed;

    public NalzamenakontaKontaViewModel(
        string firmPath, string tableName, string codeField, string oldTable, string naslov)
    {
        _firmPath  = firmPath;
        _tableName = tableName;
        _codeField = codeField;
        _oldTable  = oldTable;
        Naslov     = naslov;
        Ucitaj();
    }

    private string TablePath    => Path.Combine(_firmPath, _tableName + ".dbf");
    private string OldTablePath => Path.Combine(_firmPath, _oldTable  + ".dbf");

    private void Ucitaj()
    {
        if (!File.Exists(TablePath)) return;
        var rows = new List<KonNovRow>();
        foreach (var rec in new SimpleDbfReader(TablePath).Zapisi())
        {
            rows.Add(new KonNovRow
            {
                Kod    = rec.DajString(_codeField).TrimEnd(),
                Naziv  = rec.DajString("NAZIV").TrimEnd(),
                Nazkto1 = rec.DajString("NAZKTO1").TrimEnd(),
            });
        }
        Redovi = new ObservableCollection<KonNovRow>(rows);
        SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
    }

    private void Snimi()
    {
        if (!File.Exists(TablePath)) return;
        var schema = DbfTableWriter.LoadSchema(TablePath);
        var codeField = _codeField;
        DbfTableWriter.WriteTable(TablePath, schema, Redovi, (row, field) =>
            field.ToUpperInvariant() switch
            {
                var f when f == codeField.ToUpperInvariant()
                        => (object?)row.Kod.PadRight(row.Kod.Length),
                "NAZIV"   => row.Naziv.PadRight(45),
                "NAZKTO1" => row.Nazkto1.PadRight(45),
                _         => null,
            });
    }

    [RelayCommand]
    private void Dodaj()
    {
        var novi = new KonNovRow();
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
        Snimi();
    }

    [RelayCommand]
    private void BrisanjeTabele()
    {
        if (MessageBox.Show("Brisanje KOMPLETNE tabele. Nastaviti?", "BRISANJE TABELE",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        Redovi.Clear();
        SelektovaniRed = null;
        Snimi();
    }

    /// <summary>Copies all records from _tableName into _oldTable (ZAMENA KLASA/GRUPE/SINTETIKA)</summary>
    [RelayCommand]
    private void Zamena()
    {
        if (!File.Exists(OldTablePath))
        {
            MessageBox.Show($"Tabela {_oldTable}.dbf ne postoji.", "ZAMENA",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show(
                $"Sadržaj tabele {_tableName} biće prekopiran u {_oldTable}. Nastaviti?",
                "ZAMENA KONTA", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        Snimi();

        try
        {
            var schema = DbfTableWriter.LoadSchema(OldTablePath);
            var codeField = _codeField;
            DbfTableWriter.WriteTable(OldTablePath, schema, Redovi, (row, field) =>
                field.ToUpperInvariant() switch
                {
                    var f when f == codeField.ToUpperInvariant()
                            => (object?)row.Kod,
                    "NAZIV"   => row.Naziv,
                    "NAZKTO1" => row.Nazkto1,
                    _         => null,
                });

            MessageBox.Show($"Zamena završena. Prebačeno redova: {Redovi.Count}.",
                "ZAMENA KONTA", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri zameni:\n{ex.Message}", "ZAMENA KONTA",
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
        Snimi();
        ZatvoriFormu?.Invoke();
    }
}
