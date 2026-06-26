using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALPRIL0 — DODAVANJE KONTA ANALITIKE (nalpril0.dbf grid editor)</summary>
public partial class Nalpril0ViewModel : ObservableObject
{
    private readonly string _dbfPath;
    public event Action? ZatvoriFormu;

    public ObservableCollection<Nalpril0Row> Rows { get; } = new();

    [ObservableProperty] private Nalpril0Row? _selectedRow;

    public Nalpril0ViewModel(string firmPath)
    {
        _dbfPath = Path.Combine(firmPath, "nalpril0.dbf");
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

    private static Nalpril0Row MapRecord(DbfRecord rec) => new()
    {
        Konto   = rec.DajString("KONTO"),
        Dp      = rec.DajString("DP"),
        Preneto = rec.DajString("PRENETO"),
        Idbr    = rec.DajDecimal("IDBR"),
    };

    private static object? FieldMapper(Nalpril0Row r, string field) => field switch
    {
        "KONTO"   => r.Konto,
        "DP"      => r.Dp,
        "PRENETO" => r.Preneto,
        "IDBR"    => r.Idbr,
        _         => null,
    };

    [RelayCommand]
    private void Dodavanje()
    {
        var row = new Nalpril0Row();
        Rows.Add(row);
        SelectedRow = row;
    }

    [RelayCommand]
    private void BrisanjeReda()
    {
        if (SelectedRow == null) return;
        Rows.Remove(SelectedRow);
        SelectedRow = Rows.Count > 0 ? Rows[^1] : null;
        Snimi();
    }

    [RelayCommand] private void Dole()   { if (SelectedRow != null) { int i = Rows.IndexOf(SelectedRow); if (i < Rows.Count - 1) SelectedRow = Rows[i + 1]; } }
    [RelayCommand] private void Gore()   { if (SelectedRow != null) { int i = Rows.IndexOf(SelectedRow); if (i > 0) SelectedRow = Rows[i - 1]; } }
    [RelayCommand] private void Zadnji() { if (Rows.Count > 0) SelectedRow = Rows[^1]; }
    [RelayCommand] private void Prvi()   { if (Rows.Count > 0) SelectedRow = Rows[0]; }

    [RelayCommand]
    private void Izlaz()
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
