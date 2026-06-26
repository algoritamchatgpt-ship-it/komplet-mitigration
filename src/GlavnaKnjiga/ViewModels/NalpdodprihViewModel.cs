using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALPDODPRIH — enters DODDUG or DODPOT amount on the current NalpRow</summary>
public partial class NalpdodprihViewModel : ObservableObject
{
    private readonly NalpRow _row;
    private readonly bool    _isDug;  // APRIH='D' → sets Doddug, else sets Dodpot

    public string Naslov => "KNJIŽENJE DODATNIH PRIHODA I RASHODA";

    public event Action? ZatvoriFormu;

    [ObservableProperty] private decimal _iznos;

    public NalpdodprihViewModel(NalpRow row, bool isDug)
    {
        _row   = row;
        _isDug = isDug;
        Iznos  = isDug ? row.Doddug : row.Dodpot;
    }

    [RelayCommand]
    private void Knjizi()
    {
        if (_isDug) { _row.Doddug = Iznos; _row.Dodpot = 0; }
        else        { _row.Dodpot = Iznos; _row.Doddug = 0; }
        ZatvoriFormu?.Invoke();
    }

    [RelayCommand] private void Izlaz() => ZatvoriFormu?.Invoke();
}
