using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALPSIFDEV — SIFARNIK DEVIZA: grid editor for dev.dbf</summary>
public partial class NalPSifDevViewModel : ObservableObject
{
    private readonly string _dbfPath;
    public event Action? ZatvoriFormu;

    public ObservableCollection<DevRow> Rows { get; } = new();
    [ObservableProperty] private DevRow? _selectedRow;

    public NalPSifDevViewModel(string firmPath)
    {
        _dbfPath = Path.Combine(firmPath, "dev.dbf");
        Ucitaj();
    }

    private void Ucitaj()
    {
        Rows.Clear();
        if (!File.Exists(_dbfPath)) return;
        var r = new SimpleDbfReader(_dbfPath);
        foreach (var rec in r.Zapisi())
            Rows.Add(MapRecord(rec));
        if (Rows.Count > 0) SelectedRow = Rows[0];
    }

    private static DevRow MapRecord(DbfRecord rec) => new()
    {
        Dev     = rec.DajString("DEV"),
        Datdok  = rec.DajDate("DATDOK"),
        Kurs    = rec.DajDecimal("KURS"),
        Skurs   = rec.DajDecimal("SKURS"),
        Preneto = rec.DajString("PRENETO"),
        Idbr    = rec.DajDecimal("IDBR"),
    };

    private static object? FieldMapper(DevRow r, string field) => field switch
    {
        "DEV"     => r.Dev,   "DATDOK"  => r.Datdok,
        "KURS"    => r.Kurs,  "SKURS"   => r.Skurs,
        "PRENETO" => r.Preneto, "IDBR"  => r.Idbr,
        _         => null,
    };

    [RelayCommand]
    private void DodajNov()
    {
        var row = new DevRow { Datdok = DateTime.Today };
        Rows.Add(row);
        SelectedRow = row;
    }

    [RelayCommand] private void Izlaz()
    {
        Snimi();
        ZatvoriFormu?.Invoke();
    }

    private void Snimi()
    {
        if (!File.Exists(_dbfPath)) return;
        try
        {
            var schema = DbfTableWriter.LoadSchema(_dbfPath);
            DbfTableWriter.WriteTable(_dbfPath, schema, Rows, FieldMapper);
        }
        catch { }
    }
}
