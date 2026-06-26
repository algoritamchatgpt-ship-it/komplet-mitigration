using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class Kon2Row : ObservableObject
{
    [ObservableProperty] private string _k2 = string.Empty;
    [ObservableProperty] private string _naziv = string.Empty;
    [ObservableProperty] private string _nazkto2 = string.Empty;
    [ObservableProperty] private string _k2n = string.Empty;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private decimal _numred;
    [ObservableProperty] private decimal _idbr;
}
