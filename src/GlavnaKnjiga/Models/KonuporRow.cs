using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class KonuporRow : ObservableObject
{
    [ObservableProperty] private string  _skonto  = string.Empty;
    [ObservableProperty] private string  _deo     = string.Empty;
    [ObservableProperty] private string  _sopis   = string.Empty;
    [ObservableProperty] private string  _konto   = string.Empty;
    [ObservableProperty] private string  _opis    = string.Empty;
    [ObservableProperty] private string  _preneto = string.Empty;
    [ObservableProperty] private decimal _numred;
    [ObservableProperty] private decimal _idbr;
}
