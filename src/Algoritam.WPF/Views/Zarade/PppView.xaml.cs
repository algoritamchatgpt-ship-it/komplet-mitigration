using System.ComponentModel;
using System.Windows;
using Algoritam.WPF.ViewModels;

namespace Algoritam.WPF.Views.Zarade;

public partial class PppView : Window
{
    public PppView() { InitializeComponent(); }

    private void ZatvoriClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is PppViewModel vm)
            vm.SacuvajNaDisk(false);

        Close();
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is PppViewModel vm)
            vm.SacuvajNaDisk(false);
    }
}
