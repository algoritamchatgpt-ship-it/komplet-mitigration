using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class KontonRow : ObservableObject
{
    [ObservableProperty] private string _konto = string.Empty;
    [ObservableProperty] private string _naziv = string.Empty;
    [ObservableProperty] private string _k1 = string.Empty;
    [ObservableProperty] private string _k2 = string.Empty;
    [ObservableProperty] private string _k3 = string.Empty;
    [ObservableProperty] private string _k4 = string.Empty;
    [ObservableProperty] private string _k5 = string.Empty;
    [ObservableProperty] private string _k6 = string.Empty;
    [ObservableProperty] private string _kod = string.Empty;
    [ObservableProperty] private string _nazkto4 = string.Empty;
    [ObservableProperty] private string _skonto = string.Empty;
    [ObservableProperty] private string _jed = string.Empty;
    [ObservableProperty] private string _konton = string.Empty;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private decimal _numred;
    [ObservableProperty] private decimal _idbr;
}
