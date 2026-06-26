using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class KonzamRow : ObservableObject
{
    [ObservableProperty] private string  _skonto  = string.Empty;
    [ObservableProperty] private string  _deo     = string.Empty;
    [ObservableProperty] private string  _konto   = string.Empty;
    [ObservableProperty] private string  _naziv   = string.Empty;
    [ObservableProperty] private string  _nazkto1 = string.Empty;
    [ObservableProperty] private string  _preneto = string.Empty;
    [ObservableProperty] private decimal _idbr;
}
