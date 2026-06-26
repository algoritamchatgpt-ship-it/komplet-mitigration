using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NaluniorractraziViewModel : ObservableObject
{
    private readonly ObservableCollection<UniorRow> _redovi;
    private readonly Action<UniorRow> _pozicioniraj;

    [ObservableProperty] private string _traziVrednost = string.Empty;

    public NaluniorractraziViewModel(ObservableCollection<UniorRow> redovi, Action<UniorRow> pozicioniraj)
    {
        _redovi = redovi;
        _pozicioniraj = pozicioniraj;
    }

    [RelayCommand]
    private void Nadji()
    {
        var tekst = TraziVrednost.Trim();
        if (string.IsNullOrEmpty(tekst)) return;

        var red = _redovi.FirstOrDefault(r =>
            r.Brrac.Trim().StartsWith(tekst, StringComparison.OrdinalIgnoreCase));

        if (red != null)
        {
            _pozicioniraj(red);
            ZatvoriFormu?.Invoke();
        }
        else
        {
            MessageBox.Show($"Račun '{tekst}' nije pronađen.", "TRAŽENJE",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public event Action? ZatvoriFormu;

    [RelayCommand]
    private void Zatvori() => ZatvoriFormu?.Invoke();
}
