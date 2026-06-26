using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class NalvrstaRow : ObservableObject
{
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
    [ObservableProperty] private string  _preneto = string.Empty;
    [ObservableProperty] private decimal _idbr;

    public NalvrstaRow Clone() => (NalvrstaRow)MemberwiseClone();
}
