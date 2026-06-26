using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALPARAM — PARAMETRI KNJIŽENJA: sets PAR2 (prikaz naloga sa analitikom 1=DA)</summary>
public partial class NalParamViewModel : ObservableObject
{
    public event Action? ZatvoriFormu;

    [ObservableProperty] private string _par2 = "0";

    [RelayCommand]
    private void Izlaz()
    {
        // TODO: REPLACE PAR2 WITH par2 in datumi.dbf
        ZatvoriFormu?.Invoke();
    }
}
