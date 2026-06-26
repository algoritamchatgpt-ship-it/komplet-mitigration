using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class NalprNalRow : ObservableObject
{
    [ObservableProperty] private string    _brnal    = string.Empty;
    [ObservableProperty] private DateTime? _datdok;
    [ObservableProperty] private decimal   _dug;
    [ObservableProperty] private decimal   _pot;
    [ObservableProperty] private string    _opis     = string.Empty;
    [ObservableProperty] private string    _dok      = string.Empty;
    [ObservableProperty] private string    _mp       = string.Empty;
    [ObservableProperty] private decimal   _mtr;
    [ObservableProperty] private string    _automnal = string.Empty;
    [ObservableProperty] private string    _vrnal    = string.Empty;
    [ObservableProperty] private string    _naziv    = string.Empty;
    [ObservableProperty] private string    _obl      = string.Empty;
    [ObservableProperty] private decimal   _period;
    [ObservableProperty] private string    _naldok   = string.Empty;
    [ObservableProperty] private decimal   _znakovi;
    [ObservableProperty] private string    _pocsif   = string.Empty;
    [ObservableProperty] private string    _nauto    = string.Empty;
    [ObservableProperty] private string    _konto    = string.Empty;
    [ObservableProperty] private decimal   _saldo;
    [ObservableProperty] private DateTime? _datknji;
    [ObservableProperty] private string    _oper     = string.Empty;
    [ObservableProperty] private string    _preneto  = string.Empty;
    [ObservableProperty] private decimal   _idbr;
}
