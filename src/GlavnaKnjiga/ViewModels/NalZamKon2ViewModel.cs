using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALZAMKON2 — UNOS DOKUMENTA ZA ODREDJENI KONTO: sets DOK on all NAL rows matching KONTO</summary>
public partial class NalZamKon2ViewModel : ObservableObject
{
    private readonly string _firmPath;
    public event Action? ZatvoriFormu;

    [ObservableProperty] private string _konto = string.Empty;
    [ObservableProperty] private string _dok   = string.Empty;

    public NalZamKon2ViewModel(string firmPath)
    {
        _firmPath = firmPath;
    }

    [RelayCommand]
    private void Unos()
    {
        var nalPath = Path.Combine(_firmPath, "nal.dbf");
        if (!File.Exists(nalPath)) { MessageBox.Show("nal.dbf ne postoji."); return; }

        var mkonto = Konto;
        var mdok   = Dok;

        var reader = new SimpleDbfReader(nalPath);
        var rows   = reader.Zapisi().Select(Nalp2ViewModel.NalpRowFromRecord).ToList();
        foreach (var r in rows)
            if (r.Konto == mkonto)
                r.Dok = mdok;

        var schema = DbfTableWriter.LoadSchema(nalPath);
        DbfTableWriter.WriteTable(nalPath, schema, rows, Nalp2ViewModel.NalpRowFieldMapper);
        ZatvoriFormu?.Invoke();
    }

    [RelayCommand] private void Izlaz() => ZatvoriFormu?.Invoke();
}
