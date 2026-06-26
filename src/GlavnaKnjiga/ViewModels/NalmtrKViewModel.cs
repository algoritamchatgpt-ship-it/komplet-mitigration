using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalmtrKViewModel : ObservableObject
{
    private readonly string _dbfPath;

    public ObservableCollection<NalmtrKRow> Redovi { get; } = new();

    [ObservableProperty] private NalmtrKRow? _selectedRow;

    public event Action? ZatvoriFormu;

    public NalmtrKViewModel(string firmPath)
    {
        _dbfPath = Path.Combine(firmPath, "nalmtrk.dbf");
        Ucitaj();
    }

    private void Ucitaj()
    {
        Redovi.Clear();
        if (!File.Exists(_dbfPath)) return;
        try
        {
            var r = new SimpleDbfReader(_dbfPath);
            foreach (var rec in r.Zapisi())
                Redovi.Add(new NalmtrKRow
                {
                    Kontotr = rec.DajString("KONTOTR"),
                    Naziv   = rec.DajString("NAZIV"),
                });
        }
        catch (Exception ex) { MessageBox.Show($"Greška pri čitanju nalmtrk.dbf: {ex.Message}"); }
    }

    [RelayCommand]
    private void IdiGore()
    {
        if (SelectedRow == null && Redovi.Count > 0) { SelectedRow = Redovi[0]; return; }
        var idx = SelectedRow != null ? Redovi.IndexOf(SelectedRow) : -1;
        if (idx > 0) SelectedRow = Redovi[idx - 1];
    }

    [RelayCommand]
    private void IdiDole()
    {
        var idx = SelectedRow != null ? Redovi.IndexOf(SelectedRow) : -1;
        if (idx < Redovi.Count - 1) SelectedRow = Redovi[idx + 1];
    }

    [RelayCommand]
    private void IdiNaVrh() { if (Redovi.Count > 0) SelectedRow = Redovi[0]; }

    [RelayCommand]
    private void IdiNaDno() { if (Redovi.Count > 0) SelectedRow = Redovi[^1]; }

    [RelayCommand]
    private void Dodaj()
    {
        var row = new NalmtrKRow();
        Redovi.Add(row);
        SelectedRow = row;
    }

    [RelayCommand]
    private void Brisanje()
    {
        if (SelectedRow == null) return;
        if (MessageBox.Show("Brisanje reda?", "BRISANJE", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Redovi.Remove(SelectedRow);
        SnimiNalmtrK();
    }

    [RelayCommand]
    private void Snimi()
    {
        SnimiNalmtrK();
        MessageBox.Show("Snimljeno.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void BrisanjeTabele()
    {
        if (MessageBox.Show("Brisanje cele tabele NALMTRK?", "BRISANJE", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Redovi.Clear();
        SnimiNalmtrK();
    }

    [RelayCommand]
    private void Izlaz()
    {
        SnimiNalmtrK();
        ZatvoriFormu?.Invoke();
    }

    internal void SnimiNalmtrK()
    {
        if (!File.Exists(_dbfPath)) return;
        try
        {
            var schema = DbfTableWriter.LoadSchema(_dbfPath);
            DbfTableWriter.WriteTable(_dbfPath, schema, Redovi.ToList(), NalmtrKFieldMapper);
        }
        catch (Exception ex) { MessageBox.Show($"Greška pri snimanju nalmtrk.dbf: {ex.Message}"); }
    }

    private static object? NalmtrKFieldMapper(NalmtrKRow r, string field) => field switch
    {
        "KONTOTR" => r.Kontotr,
        "NAZIV"   => r.Naziv,
        _         => null
    };

    public IReadOnlyList<string> DajKoloneNazive()
    {
        var result = new List<string>(30);
        for (int i = 0; i < 30; i++)
        {
            if (i < Redovi.Count)
                result.Add($"{Redovi[i].Kontotr.Trim()} {Redovi[i].Naziv.Trim()}".Trim());
            else
                result.Add($"Iznos{(i + 1):D2}");
        }
        return result;
    }
}
