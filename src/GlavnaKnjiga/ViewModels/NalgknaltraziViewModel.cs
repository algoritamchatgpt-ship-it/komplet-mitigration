using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalgknaltraziViewModel : ObservableObject
{
    private readonly ObservableCollection<Nalgk10Row> _redovi;
    private readonly Action<Nalgk10Row> _pozicioniraj;

    [ObservableProperty] private string _traziVrednost = string.Empty;

    public NalgknaltraziViewModel(ObservableCollection<Nalgk10Row> redovi, Action<Nalgk10Row> pozicioniraj)
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
            r.Brnal.Trim().StartsWith(tekst, StringComparison.OrdinalIgnoreCase));

        if (red != null)
        {
            _pozicioniraj(red);
            ZatvoriFormu?.Invoke();
        }
        else
        {
            MessageBox.Show($"Nalog '{tekst}' nije pronađen.", "TRAŽENJE",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public event Action? ZatvoriFormu;

    [RelayCommand]
    private void Zatvori() => ZatvoriFormu?.Invoke();
}
