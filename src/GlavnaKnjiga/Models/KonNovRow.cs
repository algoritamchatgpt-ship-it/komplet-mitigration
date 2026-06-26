using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class KonNovRow : ObservableObject
{
    [ObservableProperty] private string _kod    = string.Empty;
    [ObservableProperty] private string _naziv  = string.Empty;
    [ObservableProperty] private string _nazkto1 = string.Empty;
}
