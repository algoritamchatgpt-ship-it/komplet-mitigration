using Algoritam.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdParametriWindow : Window
{
    public LdParametriWindow(LdParametriViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnMesecSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not LdParametriViewModel vm || !vm.JeIzmena)
            return;

        if (sender is ComboBox combo && combo.SelectedValue is int mesec)
            vm.Parametar.Mesec = mesec;

        vm.OsveziMesec();
    }

    private void OnGodinaSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not LdParametriViewModel vm || !vm.JeIzmena)
            return;

        if (sender is ComboBox combo && combo.SelectedItem is string godina)
            vm.Parametar.Godina = godina;

        vm.OsveziMesec();
    }

    private void OnZatvoriClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

}
