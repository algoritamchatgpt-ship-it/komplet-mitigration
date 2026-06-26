using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdOz07View : Window
{
    public LdOz07View()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LdOz07ViewModel vm)
            vm.ZatvaranjeZatrazeno += HandleZatvaranjeZatrazeno;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LdOz07ViewModel vm)
            vm.ZatvaranjeZatrazeno -= HandleZatvaranjeZatrazeno;
    }

    private void HandleZatvaranjeZatrazeno() => Close();

    private void IzlazClick(object sender, RoutedEventArgs e) => Close();
}
