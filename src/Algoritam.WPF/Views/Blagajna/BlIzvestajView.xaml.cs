using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Blagajna;

public partial class BlIzvestajView : Window
{
    public BlIzvestajView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is BlIzvestajViewModel vm)
            vm.ZatvaranjeZahtevano += Close;
    }
}
