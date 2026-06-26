using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalkontraziViewModel : ObservableObject
{
    private readonly ObservableCollection<NalpRow> _redovi;
    private readonly Action<NalpRow> _pozicioniraj;

    [ObservableProperty] private string _traziVrednost = string.Empty;

    public NalkontraziViewModel(ObservableCollection<NalpRow> redovi, Action<NalpRow> pozicioniraj)
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
            r.Konto.Trim().StartsWith(tekst, StringComparison.OrdinalIgnoreCase));

        if (red != null)
        {
            _pozicioniraj(red);
            ZatvoriFormu?.Invoke();
        }
        else
        {
            MessageBox.Show($"Konto '{tekst}' nije pronađen.", "TRAŽENJE",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public event Action? ZatvoriFormu;

    [RelayCommand]
    private void Zatvori() => ZatvoriFormu?.Invoke();
}
