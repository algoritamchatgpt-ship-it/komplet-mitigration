using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalzamkonuporViewModel : ObservableObject
{
    private readonly string _firmPath;

    public event Action? ZatvoriFormu;
    public event Action? DodatRed;

    [ObservableProperty] private ObservableCollection<KonuporRow> _redovi = new();
    [ObservableProperty] private KonuporRow? _selektovaniRed;

    public NalzamkonuporViewModel(string firmPath)
    {
        _firmPath = firmPath;
        Ucitaj();
    }

    private string TablePath => Path.Combine(_firmPath, "konupor.dbf");

    private void Ucitaj()
    {
        if (!File.Exists(TablePath)) return;
        var rows = new List<KonuporRow>();
        foreach (var rec in new SimpleDbfReader(TablePath).Zapisi())
        {
            rows.Add(new KonuporRow
            {
                Skonto  = rec.DajString("SKONTO").TrimEnd(),
                Deo     = rec.DajString("DEO").TrimEnd(),
                Sopis   = rec.DajString("SOPIS").TrimEnd(),
                Konto   = rec.DajString("KONTO").TrimEnd(),
                Opis    = rec.DajString("OPIS").TrimEnd(),
                Preneto = rec.DajString("PRENETO").TrimEnd(),
                Numred  = rec.DajDecimal("NUMRED"),
                Idbr    = rec.DajDecimal("IDBR"),
            });
        }
        Redovi = new ObservableCollection<KonuporRow>(rows);
        SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
    }

    private void Snimi()
    {
        if (!File.Exists(TablePath)) return;
        var schema = DbfTableWriter.LoadSchema(TablePath);
        DbfTableWriter.WriteTable(TablePath, schema, Redovi, (row, field) =>
            field.ToUpperInvariant() switch
            {
                "SKONTO"  => (object?)row.Skonto,
                "DEO"     => row.Deo,
                "SOPIS"   => row.Sopis,
                "KONTO"   => row.Konto,
                "OPIS"    => row.Opis,
                "PRENETO" => row.Preneto,
                "NUMRED"  => row.Numred,
                "IDBR"    => row.Idbr,
                _         => null,
            });
    }

    [RelayCommand]
    private void Dodaj()
    {
        var novi = new KonuporRow();
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

    [RelayCommand]
    private void PreuzmiUporednu()
    {
        var txtPath = Path.Combine(_firmPath, "konupor.txt");
        if (!File.Exists(txtPath))
        {
            MessageBox.Show("Fajl konupor.txt ne postoji.", "PREUZIMANJE",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        MessageBox.Show("Preuzimanje iz konupor.txt nije implementirano.", "PREUZIMANJE",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void KopirajUTekst()
    {
        MessageBox.Show("Kopiranje u konupor.txt nije implementirano.", "KOPIRANJE",
            MessageBoxButton.OK, MessageBoxImage.Information);
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
