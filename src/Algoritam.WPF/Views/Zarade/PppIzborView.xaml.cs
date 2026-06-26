using System.Windows;
using Algoritam.WPF.ViewModels;

namespace Algoritam.WPF.Views.Zarade;

public partial class PppIzborView : Window
{
    public PppIzborView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PppIzborViewModel vm)
            vm.ZatvoriAction = Close;
    }
}

