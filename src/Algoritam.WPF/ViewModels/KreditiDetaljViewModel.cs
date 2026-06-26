using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Algoritam.WPF.ViewModels;

public partial class KreditiDetaljViewModel : ObservableObject
{
    [ObservableProperty] private KreditStavka _stavka;
    [ObservableProperty] private string _radnikTekst;
    [ObservableProperty] private string _partnerTekst;

    public KreditiDetaljViewModel(KreditStavka stavka, string radnikTekst, string partnerTekst)
    {
        _stavka = stavka;
        _radnikTekst = radnikTekst;
        _partnerTekst = partnerTekst;
    }

    [RelayCommand]
    private void Potvrdi(System.Windows.Window? window)
    {
        if (window != null)
            window.DialogResult = true;
    }

    [RelayCommand]
    private void Otkazi(System.Windows.Window? window)
    {
        if (window != null)
            window.DialogResult = false;
    }
}
