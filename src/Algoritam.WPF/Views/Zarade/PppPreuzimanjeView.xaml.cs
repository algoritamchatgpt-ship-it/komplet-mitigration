using System.ComponentModel;
using System.Windows;
using Algoritam.WPF.ViewModels;

namespace Algoritam.WPF.Views.Zarade;

public partial class PppPreuzimanjeView : Window
{
    public PppPreuzimanjeView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PppPreuzimanjeViewModel vm)
            vm.ZatvoriAction = Close;
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is PppPreuzimanjeViewModel vm)
            vm.SacuvajNaDisk(false);
    }
}

