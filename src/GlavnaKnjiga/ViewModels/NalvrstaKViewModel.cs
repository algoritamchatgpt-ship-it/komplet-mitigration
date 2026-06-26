using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;

namespace GlavnaKnjiga.ViewModels;

/// <summary>
/// Transcripcija NALVRSTAK.SCX — KARTICA VRSTE NALOGA (edit dialog).
/// Edituje sva polja jednog NalvrstaRow in-place.
/// </summary>
public partial class NalvrstaKViewModel : ObservableObject
{
    private readonly NalvrstaRow _red;

    [ObservableProperty] private string  _vrnal   = string.Empty;
    [ObservableProperty] private string  _naziv   = string.Empty;
    [ObservableProperty] private string  _dok     = string.Empty;
    [ObservableProperty] private string  _mp      = string.Empty;
    [ObservableProperty] private string  _obl     = string.Empty;
    [ObservableProperty] private decimal _period;
    [ObservableProperty] private string  _naldok  = string.Empty;
    [ObservableProperty] private decimal _znakovi;
    [ObservableProperty] private string  _pocsif  = string.Empty;
    [ObservableProperty] private string  _nauto   = string.Empty;
    [ObservableProperty] private string  _konto   = string.Empty;

    public event Action<bool>? ZatvoriFormu;

    public NalvrstaKViewModel(NalvrstaRow red)
    {
        _red    = red;
        Vrnal   = red.Vrnal;
        Naziv   = red.Naziv;
        Dok     = red.Dok;
        Mp      = red.Mp;
        Obl     = red.Obl;
        Period  = red.Period;
        Naldok  = red.Naldok;
        Znakovi = red.Znakovi;
        Pocsif  = red.Pocsif;
        Nauto   = red.Nauto;
        Konto   = red.Konto;
    }

    [RelayCommand]
    private void Snimi()
    {
        _red.Vrnal   = Vrnal;
        _red.Naziv   = Naziv;
        _red.Dok     = Dok;
        _red.Mp      = Mp;
        _red.Obl     = Obl;
        _red.Period  = Period;
        _red.Naldok  = Naldok;
        _red.Znakovi = Znakovi;
        _red.Pocsif  = Pocsif;
        _red.Nauto   = Nauto;
        _red.Konto   = Konto;
        ZatvoriFormu?.Invoke(true);
    }

    [RelayCommand]
    private void Odustani() => ZatvoriFormu?.Invoke(false);
}
