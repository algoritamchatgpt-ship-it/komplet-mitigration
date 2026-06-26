using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class Nalgk10Row : ObservableObject
{
    [ObservableProperty] private string   _konto   = string.Empty;
    [ObservableProperty] private decimal  _dug;
    [ObservableProperty] private decimal  _pot;
    [ObservableProperty] private string   _opis    = string.Empty;
    [ObservableProperty] private DateTime? _datdok;
    [ObservableProperty] private string   _brnal   = string.Empty;
    [ObservableProperty] private string   _arhiva  = string.Empty;
    [ObservableProperty] private DateTime? _datum;
    [ObservableProperty] private string   _vreme   = string.Empty;
    [ObservableProperty] private string   _preneto = string.Empty;
    [ObservableProperty] private decimal  _idbr;
}
