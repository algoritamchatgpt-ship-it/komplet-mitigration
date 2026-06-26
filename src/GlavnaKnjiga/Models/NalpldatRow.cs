using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class NalpldatRow : ObservableObject
{
    [ObservableProperty] private DateTime? _dat0;
    [ObservableProperty] private DateTime? _dat1;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private decimal _idbr;
}
