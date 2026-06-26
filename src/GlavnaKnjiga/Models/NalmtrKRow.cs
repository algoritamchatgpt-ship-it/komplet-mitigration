using CommunityToolkit.Mvvm.ComponentModel;

namespace GlavnaKnjiga.Models;

public partial class NalmtrKRow : ObservableObject
{
    [ObservableProperty] private string _kontotr = string.Empty;
    [ObservableProperty] private string _naziv   = string.Empty;
}
