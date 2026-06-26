using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class NalprilRow : ObservableObject
{
    [ObservableProperty] private string   _konto   = string.Empty;
    [ObservableProperty] private string   _sifra   = string.Empty;
    [ObservableProperty] private decimal  _dug;
    [ObservableProperty] private decimal  _pot;
    [ObservableProperty] private decimal  _dugpre;
    [ObservableProperty] private decimal  _potpre;
    [ObservableProperty] private string   _naziv   = string.Empty;
    [ObservableProperty] private string   _brrac   = string.Empty;
    [ObservableProperty] private string   _pauto   = string.Empty;
    [ObservableProperty] private string   _opis    = string.Empty;
    [ObservableProperty] private DateTime? _dat0;
    [ObservableProperty] private DateTime? _dat1;
    [ObservableProperty] private string   _k1      = string.Empty;
    [ObservableProperty] private string   _k2      = string.Empty;
    [ObservableProperty] private string   _k3      = string.Empty;
    [ObservableProperty] private string   _grupa   = string.Empty;
    [ObservableProperty] private string   _preneto = string.Empty;
    [ObservableProperty] private decimal  _idbr;
}
