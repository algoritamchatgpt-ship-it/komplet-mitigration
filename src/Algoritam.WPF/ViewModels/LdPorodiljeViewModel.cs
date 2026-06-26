using Algoritam.Application;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Fox forma LDPORODILJE00 - PORODILJE.
/// Preslikava meni i poruke 1:1; podforme ce biti vezane u sledecim koracima.
/// </summary>
public partial class LdPorodiljeViewModel : ObservableObject
{
    private readonly AppState _appState;

    [ObservableProperty] private string _naslov = "PORODILJE";
    [ObservableProperty] private string _poruka = "Izaberite opciju.";

    public LdPorodiljeViewModel(AppState appState)
    {
        _appState = appState;
    }

    [RelayCommand]
    private void PorodiljeNz1()
    {
        if (!ImaPravoPristupa("NEMATE PRAVO PRISTUPA SPISKU NZ-1"))
            return;

        var vm = new LdNz1ViewModel(_appState);
        var view = new Views.Zarade.LdNz1View { DataContext = vm };
        view.ShowDialog();
    }

    [RelayCommand]
    private void KretanjeZarada()
    {
        if (!ImaPravoPristupa("NEMATE PRAVO PRISTUPA POTVRDI O KRETANJU ZARADA"))
            return;

        var vm = new LdPorpotViewModel(_appState);
        var view = new Views.Zarade.LdPorpotView { DataContext = vm };
        view.ShowDialog();
    }

    [RelayCommand]
    private void Zarada12Meseci()
    {
        if (!ImaPravoPristupa("NEMATE PRAVO PRISTUPA POTVRDI O KRETANJU ZARADA"))
            return;

        var vm = new LdPorp12ViewModel(_appState);
        var view = new Views.Zarade.LdPorp12View { DataContext = vm };
        view.ShowDialog();
    }

    [RelayCommand]
    private void ZahtevZ1()
    {
        if (!ImaPravoPristupa("NEMATE PRAVO PRISTUPA ZAHTEVU Z-1"))
            return;

        var vm = new LdZ1ViewModel(_appState);
        var view = new Views.Zarade.LdZ1View { DataContext = vm };
        view.ShowDialog();
    }

    [RelayCommand]
    private void PotvrdaStazu()
    {
        if (!ImaPravoPristupa("NEMATE PRAVO PRISTUPA POTVRDI O STAZU"))
            return;

        var vm = new LdPorstViewModel(_appState);
        var view = new Views.Zarade.LdPorstView { DataContext = vm };
        view.ShowDialog();
    }

    private bool ImaPravoPristupa(string porukaBezPrava)
    {
        var imaPravo = _appState.JeSupervizor || (_appState.TrenutniKorisnik?.PassLd ?? false);
        if (imaPravo)
            return true;

        Poruka = porukaBezPrava;
        MessageBox.Show(
            porukaBezPrava,
            "Algoritam - Zarade",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        return false;
    }

}
