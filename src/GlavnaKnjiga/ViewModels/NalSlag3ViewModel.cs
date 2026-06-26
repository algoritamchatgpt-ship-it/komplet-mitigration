using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALSLAG3 — ZAMENA NALOGA IZ DRUGE TABELE: replaces NAL records for BRNALs in KOCKA</summary>
public partial class NalSlag3ViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly IReadOnlyList<NalslagRow> _kockaRows;
    public event Action? ZatvoriFormu;

    [ObservableProperty] private string _tabela    = string.Empty;
    [ObservableProperty] private string _lblStatus = string.Empty;

    public NalSlag3ViewModel(string firmPath, IEnumerable<NalslagRow> kockaRows)
    {
        _firmPath  = firmPath;
        _kockaRows = kockaRows.ToList();
    }

    [RelayCommand]
    private void Zamena()
    {
        if (string.IsNullOrWhiteSpace(Tabela))
        {
            MessageBox.Show("Unesite ime tabele.", "ZAMENA NALOGA", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        // TODO: foreach BRNAL in kocka: zero DUG/POT/DEVDUG/DEVPOT in nal.dbf, append from Tabela
        MessageBox.Show("ZAMENA NALOGA IZ DRUGE TABELE — u pripremi.", "Nalslag3", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand] private void Izlaz() => ZatvoriFormu?.Invoke();
}
