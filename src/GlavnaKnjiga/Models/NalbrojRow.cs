using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class NalbrojRow : ObservableObject
{
    [ObservableProperty] private string  _brnal   = string.Empty;
    [ObservableProperty] private DateTime? _datum;
    [ObservableProperty] private string  _vrnal   = string.Empty;
    [ObservableProperty] private string  _opis    = string.Empty;
    [ObservableProperty] private DateTime? _datod;
    [ObservableProperty] private DateTime? _datdo;
    [ObservableProperty] private decimal _dug;
    [ObservableProperty] private decimal _pot;
    [ObservableProperty] private DateTime? _datknji;
    [ObservableProperty] private string  _oper    = string.Empty;
    [ObservableProperty] private string  _preneto = string.Empty;
    [ObservableProperty] private decimal _idbr;
}
