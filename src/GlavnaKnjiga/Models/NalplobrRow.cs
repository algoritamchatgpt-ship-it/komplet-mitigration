using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class NalplobrRow : ObservableObject
{
    [ObservableProperty] private string   _k2      = string.Empty;
    [ObservableProperty] private string   _dugpot  = string.Empty;
    [ObservableProperty] private string   _gnaz    = string.Empty;
    [ObservableProperty] private decimal  _dug;
    [ObservableProperty] private decimal  _pot;
    [ObservableProperty] private decimal  _saldo;
    [ObservableProperty] private DateTime? _dat0;
    [ObservableProperty] private DateTime? _dat1;
    [ObservableProperty] private string   _preneto = string.Empty;
    [ObservableProperty] private decimal  _idbr;
}
