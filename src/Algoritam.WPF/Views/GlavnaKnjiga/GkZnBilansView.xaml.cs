using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.GlavnaKnjiga;

public partial class GkZnBilansView : Window
{
    public GkZnBilansView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is GkZnBilansViewModel vm)
            vm.ZatvaranjeZahtevano += Close;
    }
}
