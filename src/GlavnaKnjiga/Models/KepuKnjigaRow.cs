using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class KepuKnjigaRow : ObservableObject
{
    [ObservableProperty] private bool _izabrana;
    [ObservableProperty] private string _sifprod = string.Empty;
    [ObservableProperty] private string _naziv = string.Empty;
    [ObservableProperty] private string _mesto = string.Empty;
    [ObservableProperty] private string _kontoPazara = string.Empty;
    [ObservableProperty] private string _kontoUsluga = string.Empty;
}
