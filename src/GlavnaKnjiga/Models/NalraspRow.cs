using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class NalraspRow : ObservableObject
{
    [ObservableProperty] private string   _konto   = string.Empty;
    [ObservableProperty] private string   _k9      = string.Empty;
    [ObservableProperty] private decimal  _ucesce;
    [ObservableProperty] private string   _dp      = string.Empty;
    [ObservableProperty] private decimal  _dug;
    [ObservableProperty] private decimal  _pot;
    [ObservableProperty] private string   _opis    = string.Empty;
    [ObservableProperty] private decimal  _mtr;
    [ObservableProperty] private DateTime? _datdok;
    [ObservableProperty] private string   _brnal   = string.Empty;
    [ObservableProperty] private string   _dok     = string.Empty;
    [ObservableProperty] private string   _k1      = string.Empty;
    [ObservableProperty] private string   _k2      = string.Empty;
    [ObservableProperty] private string   _k3      = string.Empty;
    [ObservableProperty] private string   _k4      = string.Empty;
    [ObservableProperty] private string   _k5      = string.Empty;
    [ObservableProperty] private string   _k6      = string.Empty;
    [ObservableProperty] private string   _preneto = string.Empty;
    [ObservableProperty] private decimal  _idbr;
}
