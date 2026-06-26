using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class Nalpep03Row : ObservableObject
{
    [ObservableProperty] private string _racund = string.Empty;
    [ObservableProperty] private string _dp = string.Empty;
    [ObservableProperty] private string _prazn01 = string.Empty;
    [ObservableProperty] private string _datum = string.Empty;
    [ObservableProperty] private string _prazno5 = string.Empty;
    [ObservableProperty] private string _naziv = string.Empty;
    [ObservableProperty] private string _prazno4 = string.Empty;
    [ObservableProperty] private string _iznos = string.Empty;
    [ObservableProperty] private string _prazn02 = string.Empty;
    [ObservableProperty] private string _modelz = string.Empty;
    [ObservableProperty] private string _pozivz = string.Empty;
    [ObservableProperty] private string _modelp = string.Empty;
    [ObservableProperty] private string _pozivp = string.Empty;
    [ObservableProperty] private string _svrha = string.Empty;
    [ObservableProperty] private string _prazn03 = string.Empty;
    [ObservableProperty] private decimal _dug;
    [ObservableProperty] private decimal _pot;
    [ObservableProperty] private string _racunp2 = string.Empty;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private decimal _idbr;
}
