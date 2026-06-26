using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NaldattraziViewModel : ObservableObject
{
    private readonly ObservableCollection<NalpRow> _redovi;
    private readonly Action<NalpRow> _pozicioniraj;

    [ObservableProperty] private DateTime? _traziVrednost;

    public NaldattraziViewModel(ObservableCollection<NalpRow> redovi, Action<NalpRow> pozicioniraj)
    {
        _redovi = redovi;
        _pozicioniraj = pozicioniraj;
    }

    [RelayCommand]
    private void Nadji()
    {
        var datum = TraziVrednost;
        if (datum == null) return;

        var red = _redovi.FirstOrDefault(r =>
            r.Datdok?.Date == datum.Value.Date);

        if (red != null)
        {
            _pozicioniraj(red);
            ZatvoriFormu?.Invoke();
        }
        else
        {
            MessageBox.Show($"Datum '{datum:dd.MM.yyyy}' nije pronađen.", "TRAŽENJE",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public event Action? ZatvoriFormu;

    [RelayCommand]
    private void Zatvori() => ZatvoriFormu?.Invoke();
}
