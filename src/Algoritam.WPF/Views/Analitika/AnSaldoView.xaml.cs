using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Analitika;

public partial class AnSaldoView : Window
{
    public AnSaldoView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is AnSaldoViewModel vm)
            vm.ZatvaranjeZahtevano += Close;
    }
}
