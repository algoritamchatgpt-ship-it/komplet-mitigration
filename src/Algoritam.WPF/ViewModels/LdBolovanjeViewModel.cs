using Algoritam.Application;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Algoritam.WPF.Views.Zarade;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Fox forma LDBOLOVANJE00 - BOLOVANJE.
/// Preslikava meni i redosled akcija 1:1 i otvara odgovarajuce podforme.
/// </summary>
public partial class LdBolovanjeViewModel : ObservableObject
{
    private readonly AppState _appState;

    [ObservableProperty] private string _naslov = "BOLOVANJE";

    public LdBolovanjeViewModel(AppState appState)
    {
        _appState = appState;
    }

    [RelayCommand]
    private void BolovanjeOz7Novi()
    {
        if (!ImaPravoPristupa("NEMATE PRAVO PRISTUPA"))
            return;

        var vm = new LdOz07NoviViewModel(_appState);
        var view = new LdOz07NoviView { DataContext = vm };
        view.ShowDialog();
    }

    [RelayCommand]
    private void BolovanjeOz7()
    {
        if (!ImaPravoPristupa("NEMATE PRAVO PRISTUPA"))
            return;

        var vm = new LdOz07ViewModel(_appState);
        var view = new LdOz07View { DataContext = vm };
        view.ShowDialog();
    }

    [RelayCommand]
    private void BolovanjeOz8()
    {
        if (!ImaPravoPristupa("NEMATE PRAVO PRISTUPA"))
            return;

        var vm = new LdOz08ViewModel(_appState);
        var view = new LdOz08View { DataContext = vm };
        view.ShowDialog();
    }

    [RelayCommand]
    private void BolovanjeOz9()
    {
        if (!ImaPravoPristupa("NEMATE PRAVO PRISTUPA"))
            return;

        var vm = new LdOz09ViewModel(_appState);
        var view = new LdOz09View { DataContext = vm };
        view.ShowDialog();
    }

    [RelayCommand]
    private void BolovanjeOz10()
    {
        if (!ImaPravoPristupa("NEMATE PRAVO PRISTUPA SPISKU ZA BOLOVANJE"))
            return;

        var vm = new LdOz10ViewModel(_appState);
        var view = new LdOz10View { DataContext = vm };
        view.ShowDialog();
    }

    private bool ImaPravoPristupa(string porukaBezPrava)
    {
        var imaPravo = _appState.JeSupervizor || (_appState.TrenutniKorisnik?.PassLd ?? false);
        if (imaPravo)
            return true;

        MessageBox.Show(
            porukaBezPrava,
            "Algoritam - Zarade",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        return false;
    }
}
