using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class UniorkonRow : ObservableObject
{
    [ObservableProperty] private string  _vpdv      = string.Empty;
    [ObservableProperty] private string  _kontoa    = string.Empty;
    [ObservableProperty] private string  _konto     = string.Empty;
    [ObservableProperty] private string  _pogon     = string.Empty;
    [ObservableProperty] private string  _povezanol = string.Empty;
    [ObservableProperty] private string  _vrstaf    = string.Empty;
    [ObservableProperty] private string  _preneto   = string.Empty;
    [ObservableProperty] private decimal _numred;
    [ObservableProperty] private decimal _idbr;
}
