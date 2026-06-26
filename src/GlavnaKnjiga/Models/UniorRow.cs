using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class UniorRow : ObservableObject
{
    [ObservableProperty] private string   _vpdv       = string.Empty;
    [ObservableProperty] private string   _kontoa     = string.Empty;
    [ObservableProperty] private string   _konto      = string.Empty;
    [ObservableProperty] private string   _brnal      = string.Empty;
    [ObservableProperty] private DateTime? _datslanja;
    [ObservableProperty] private DateTime? _datpdv;
    [ObservableProperty] private DateTime? _datdok;
    [ObservableProperty] private string   _brrac      = string.Empty;
    [ObservableProperty] private string   _valuta     = string.Empty;
    [ObservableProperty] private string   _sifra      = string.Empty;
    [ObservableProperty] private decimal  _ukprod;
    [ObservableProperty] private decimal  _osn18;
    [ObservableProperty] private decimal  _pdv18;
    [ObservableProperty] private decimal  _osn8;
    [ObservableProperty] private decimal  _pdv8;
    [ObservableProperty] private decimal  _ukupno;
    [ObservableProperty] private decimal  _pdv;
    [ObservableProperty] private decimal  _osn0;
    [ObservableProperty] private string   _dok        = string.Empty;
    [ObservableProperty] private string   _dev        = string.Empty;
    [ObservableProperty] private decimal  _devkurs;
    [ObservableProperty] private decimal  _devdug;
    [ObservableProperty] private decimal  _devpot;
    [ObservableProperty] private decimal  _kurs;
    [ObservableProperty] private string   _arhiva     = string.Empty;
    [ObservableProperty] private string   _naziv      = string.Empty;
    [ObservableProperty] private string   _pib        = string.Empty;
    [ObservableProperty] private string   _pogon      = string.Empty;
    [ObservableProperty] private string   _povezanol  = string.Empty;
    [ObservableProperty] private string   _vrstaf     = string.Empty;
    [ObservableProperty] private string   _preneto    = string.Empty;
    [ObservableProperty] private decimal  _numred;
    [ObservableProperty] private decimal  _idbr;
}
