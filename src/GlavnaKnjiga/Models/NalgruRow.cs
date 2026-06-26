using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class NalgruRow : ObservableObject
{
    [ObservableProperty] private string _konto   = string.Empty;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private decimal _idbr;

    public NalgruRow Clone() => new() { Konto = Konto, Preneto = Preneto, Idbr = Idbr };
}
