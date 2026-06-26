using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class Kon3Row : ObservableObject
{
    [ObservableProperty] private string _k3 = string.Empty;
    [ObservableProperty] private string _naziv = string.Empty;
    [ObservableProperty] private string _nazkto3 = string.Empty;
    [ObservableProperty] private string _k3n = string.Empty;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private decimal _numred;
    [ObservableProperty] private decimal _idbr;
}
