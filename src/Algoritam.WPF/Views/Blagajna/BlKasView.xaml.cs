using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Blagajna;

public partial class BlKasView : Window
{
    public BlKasView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is BlKasViewModel vm)
            vm.ZatvaranjeZahtevano += Close;
    }
}
