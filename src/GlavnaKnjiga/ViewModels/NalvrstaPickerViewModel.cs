using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;

namespace GlavnaKnjiga.ViewModels;

public partial class NalvrstaPickerViewModel : ObservableObject
{
    public event Action<bool>? ZatvoriFormu;

    public ObservableCollection<NalvrstaRow> Redovi { get; }
    public string? IzabraniVrnal { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(IzaberiCommand))]
    private NalvrstaRow? _selectedRow;

    public NalvrstaPickerViewModel(IEnumerable<NalvrstaRow> rows)
    {
        Redovi = new ObservableCollection<NalvrstaRow>(
            rows.OrderBy(r => r.Vrnal.Trim(), StringComparer.OrdinalIgnoreCase));
        SelectedRow = Redovi.FirstOrDefault();
    }

    private bool MozeIzaberi() => SelectedRow != null;

    [RelayCommand(CanExecute = nameof(MozeIzaberi))]
    private void Izaberi()
    {
        if (SelectedRow == null) return;
        IzabraniVrnal = SelectedRow.Vrnal.Trim();
        ZatvoriFormu?.Invoke(true);
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke(false);
}
