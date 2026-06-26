using System.ComponentModel;
using System.Windows;
using Algoritam.WPF.ViewModels;

namespace Algoritam.WPF.Views.Zarade;

public partial class PppPrenosParametriView : Window
{
    public PppPrenosParametriView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PppPrenosParametriViewModel vm)
            vm.ZatvoriAction = Close;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is PppPrenosParametriViewModel vm)
            vm.SacuvajCommand.Execute(null);
    }
}

