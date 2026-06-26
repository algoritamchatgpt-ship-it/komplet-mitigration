using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdPripravniciView : Window
{
    public LdPripravniciView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LdPripravniciViewModel vm)
            vm.ZatvaranjeZatrazeno += HandleZatvaranjeZatrazeno;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LdPripravniciViewModel vm)
            vm.ZatvaranjeZatrazeno -= HandleZatvaranjeZatrazeno;
    }

    private void HandleZatvaranjeZatrazeno() => Close();

    private void IzlazClick(object sender, RoutedEventArgs e) => Close();
}
