using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class DevRow : ObservableObject
{
    [ObservableProperty] private string   _dev     = string.Empty;
    [ObservableProperty] private DateTime? _datdok;
    [ObservableProperty] private decimal  _kurs;
    [ObservableProperty] private decimal  _skurs;
    [ObservableProperty] private string   _preneto = string.Empty;
    [ObservableProperty] private decimal  _idbr;
}
