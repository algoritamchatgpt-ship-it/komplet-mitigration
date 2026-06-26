using Algoritam.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// ViewModel za dijalog "Obračun Bruto" — ekvivalent FoxPro forme LDOBRACUN.SCX.
/// Korisnik unosi cenu rada, grupu i opcije pre pokretanja obračuna.
/// </summary>
public partial class ObracunBrutoViewModel : ObservableObject
{
    [ObservableProperty] private decimal _cenaRada;
    [ObservableProperty] private int _grupa;
    [ObservableProperty] private bool _obracunatiNaknade = true;
    [ObservableProperty] private bool _obracunatiOdUkupneObaveze;
    [ObservableProperty] private decimal _neoporez;
    [ObservableProperty] private decimal _neoporezP;
    [ObservableProperty] private string _statusPoruka = "";

    public bool Potvrdjen { get; private set; }

    public ObracunBrutoViewModel(LdParametar param)
    {
        CenaRada = param.Cenarada;
        Neoporez = param.Neoporez;
        NeoporezP = param.Neoporezp;
    }

    [RelayCommand]
    private void Potvrdi(System.Windows.Window? window)
    {
        if (CenaRada <= 0)
        {
            StatusPoruka = "Cena rada mora biti veća od 0!";
            return;
        }

        Potvrdjen = true;
        if (window != null) window.DialogResult = true;
    }

    [RelayCommand]
    private void Odustani(System.Windows.Window? window)
    {
        if (window != null) window.DialogResult = false;
    }
}
