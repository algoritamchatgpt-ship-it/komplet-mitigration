using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NaluniorkonViewModel : ObservableObject
{
    private readonly string _firmPath;

    public event Action? ZatvoriFormu;
    public event Action? DodatRed;

    [ObservableProperty] private ObservableCollection<UniorkonRow> _redovi = new();
    [ObservableProperty] private UniorkonRow? _selektovaniRed;

    public NaluniorkonViewModel(string firmPath)
    {
        _firmPath = firmPath;
        Ucitaj();
    }

    private string TablePath => Path.Combine(_firmPath, "uniorkon.dbf");

    private void Ucitaj()
    {
        if (!File.Exists(TablePath)) return;
        var rows = new List<UniorkonRow>();
        foreach (var rec in new SimpleDbfReader(TablePath).Zapisi())
        {
            rows.Add(new UniorkonRow
            {
                Vpdv      = rec.DajString("VPDV").TrimEnd(),
                Kontoa    = rec.DajString("KONTOA").TrimEnd(),
                Konto     = rec.DajString("KONTO").TrimEnd(),
                Pogon     = rec.DajString("POGON").TrimEnd(),
                Povezanol = rec.DajString("POVEZANOL").TrimEnd(),
                Vrstaf    = rec.DajString("VRSTAF").TrimEnd(),
                Preneto   = rec.DajString("PRENETO").TrimEnd(),
                Numred    = rec.DajDecimal("NUMRED"),
                Idbr      = rec.DajDecimal("IDBR"),
            });
        }
        Redovi = new ObservableCollection<UniorkonRow>(rows);
        SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
    }

    private void Snimi()
    {
        if (!File.Exists(TablePath)) return;
        var schema = DbfTableWriter.LoadSchema(TablePath);
        DbfTableWriter.WriteTable(TablePath, schema, Redovi, (row, field) =>
            field.ToUpperInvariant() switch
            {
                "VPDV"      => (object?)row.Vpdv,
                "KONTOA"    => row.Kontoa,
                "KONTO"     => row.Konto,
                "POGON"     => row.Pogon,
                "POVEZANOL" => row.Povezanol,
                "VRSTAF"    => row.Vrstaf,
                "PRENETO"   => row.Preneto,
                "NUMRED"    => row.Numred,
                "IDBR"      => row.Idbr,
                _           => null,
            });
    }

    [RelayCommand]
    private void Dodaj()
    {
        var novi = new UniorkonRow();
        Redovi.Add(novi);
        SelektovaniRed = novi;
        DodatRed?.Invoke();
    }

    [RelayCommand]
    private void BrisanjeZadnje()
    {
        if (Redovi.Count == 0) return;
        Redovi.RemoveAt(Redovi.Count - 1);
        SelektovaniRed = Redovi.Count > 0 ? Redovi[^1] : null;
        Snimi();
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
