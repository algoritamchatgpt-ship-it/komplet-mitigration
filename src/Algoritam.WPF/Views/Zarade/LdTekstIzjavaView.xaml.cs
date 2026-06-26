using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdTekstIzjavaView : Window
{
    public LdTekstIzjavaView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LdTekstIzjavaViewModel vm)
        {
            vm.ZatvaranjeZatrazeno += HandleZatvaranjeZatrazeno;
            PrimeniBrojTekstKolona(vm);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LdTekstIzjavaViewModel vm)
            vm.ZatvaranjeZatrazeno -= HandleZatvaranjeZatrazeno;
    }

    private void HandleZatvaranjeZatrazeno() => Close();

    private void IzlazClick(object sender, RoutedEventArgs e) => Close();

    private void PrimeniBrojTekstKolona(LdTekstIzjavaViewModel vm)
    {
        for (var i = 0; i < TekstGrid.Columns.Count; i++)
        {
            TekstGrid.Columns[i].Visibility =
                i < vm.BrojTekstPolja ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
