using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class NalpepRow : ObservableObject
{
    [ObservableProperty] private string _konto = string.Empty;
    [ObservableProperty] private decimal _dug;
    [ObservableProperty] private decimal _pot;
    [ObservableProperty] private string _opis = string.Empty;
    [ObservableProperty] private string _sifra = string.Empty;
    [ObservableProperty] private string _naziv = string.Empty;
    [ObservableProperty] private string _brrac = string.Empty;
    [ObservableProperty] private DateTime? _datdok;
    [ObservableProperty] private DateTime? _valuta;
    [ObservableProperty] private string _brnal = string.Empty;
    [ObservableProperty] private string _pozivz = string.Empty;
    [ObservableProperty] private string _pozivp = string.Empty;
    [ObservableProperty] private string _mp = string.Empty;
    [ObservableProperty] private decimal _mtr;
    [ObservableProperty] private string _dok = string.Empty;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private decimal _idbr;
}
