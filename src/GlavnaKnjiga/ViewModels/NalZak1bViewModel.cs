using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALZAK1B — filter dialog for zaključni list (konto prefix + date range + exclusions)</summary>
public partial class NalZak1bViewModel : ObservableObject
{
    public event Action? ZatvoriFormu;
    public event Action? OtvoriNalZak2;

    [ObservableProperty] private string   _konp          = string.Empty;
    [ObservableProperty] private DateTime _dat0          = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime _dat1          = DateTime.Today;
    [ObservableProperty] private string   _brnal1        = string.Empty;
    [ObservableProperty] private string   _brnal2        = string.Empty;
    [ObservableProperty] private string   _brnal3        = string.Empty;
    [ObservableProperty] private string   _brnal4        = string.Empty;
    [ObservableProperty] private string   _dok           = string.Empty;
    [ObservableProperty] private string   _mp            = string.Empty;
    [ObservableProperty] private decimal  _mtr;
    [ObservableProperty] private string   _cifre         = "N";
    [ObservableProperty] private string   _nedovrseni    = "N";
    [ObservableProperty] private string   _automatski    = "N";

    [RelayCommand]
    private void Pregled()
    {
        // TODO: call nalzak1b.prg equivalent computation, then open Nalzak2
        MessageBox.Show("Zaključni list — u pripremi.", "Zaključni list", MessageBoxButton.OK, MessageBoxImage.Information);
        OtvoriNalZak2?.Invoke();
    }

    [RelayCommand] private void Izlaz() => ZatvoriFormu?.Invoke();
}
