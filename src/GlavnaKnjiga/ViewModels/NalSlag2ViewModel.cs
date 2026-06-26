using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALSLAG2 — shows KOCKA (BRNAL Dug/Pot totals) after slaganje computation</summary>
public partial class NalSlag2ViewModel : ObservableObject
{
    private readonly string _firmPath;
    public event Action? ZatvoriFormu;
    public event Action? OtvoriNalSlag3;

    public ObservableCollection<NalslagRow> Rows { get; }

    [ObservableProperty] private NalslagRow? _selectedRow;

    public NalSlag2ViewModel(string firmPath, IEnumerable<NalslagRow> kockaRows)
    {
        _firmPath = firmPath;
        Rows      = new ObservableCollection<NalslagRow>(kockaRows);
        if (Rows.Count > 0) SelectedRow = Rows[0];
    }

    [RelayCommand]
    private void Stampa()
    {
        MessageBox.Show("NALSLAG0 stampa — u pripremi.", "Nalslag2", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void UbaciUNal()
    {
        // TODO: SCATTER NAME polja / GATHER NAME polja — copy KOCKA rows to nalzam.dbf
        MessageBox.Show("UBACI U NAL — u pripremi.", "Nalslag2", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand] private void OtvoriSlag3() => OtvoriNalSlag3?.Invoke();

    [RelayCommand] private void Dole()   { if (SelectedRow != null) { int i = Rows.IndexOf(SelectedRow); if (i < Rows.Count - 1) SelectedRow = Rows[i + 1]; } }
    [RelayCommand] private void Gore()   { if (SelectedRow != null) { int i = Rows.IndexOf(SelectedRow); if (i > 0) SelectedRow = Rows[i - 1]; } }
    [RelayCommand] private void Zadnji() { if (Rows.Count > 0) SelectedRow = Rows[^1]; }
    [RelayCommand] private void Prvi()   { if (Rows.Count > 0) SelectedRow = Rows[0]; }

    [RelayCommand] private void Izlaz() => ZatvoriFormu?.Invoke();
}
