using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class NalprkonRow : ObservableObject
{
    [ObservableProperty] private string  _konto   = string.Empty;
    [ObservableProperty] private decimal _dug;
    [ObservableProperty] private decimal _pot;
    [ObservableProperty] private decimal _saldo;
    [ObservableProperty] private string  _naziv   = string.Empty;
    [ObservableProperty] private string  _preneto = string.Empty;
    [ObservableProperty] private decimal _idbr;
}
