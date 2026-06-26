using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

public partial class PartnerKarticaViewModel : ObservableObject
{
    [ObservableProperty] private PartnerStavka _stavka;
    [ObservableProperty] private string _naslov;

    public PartnerKarticaViewModel(PartnerStavka stavka, bool noviUnos)
    {
        _stavka = stavka;
        _naslov = noviUnos ? "KARTICA PARTNERA - NOVI UNOS (AN0K2)" : "KARTICA PARTNERA - IZMENA (AN0K2)";
    }

    [RelayCommand]
    private void Potvrdi(Window? window)
    {
        if (string.IsNullOrWhiteSpace(Stavka.Sifra))
        {
            MessageBox.Show("Sifra partnera je obavezna.", "Partneri", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(Stavka.Naziv))
        {
            MessageBox.Show("Naziv partnera je obavezan.", "Partneri", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(Stavka.ZiroRac))
        {
            MessageBox.Show("Ziro racun partnera je obavezan.", "Partneri", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(Stavka.Pib2))
            Stavka.Pib2 = Stavka.Sifra.Trim();

        if (window != null)
            window.DialogResult = true;
    }

    [RelayCommand]
    private void Otkazi(Window? window)
    {
        if (window != null)
            window.DialogResult = false;
    }
}
