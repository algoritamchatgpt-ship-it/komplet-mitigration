using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Veleprodaja;

public partial class TvRobaView : Window
{
    public TvRobaView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is TvRobaViewModel vm)
            vm.ZatvaranjeZahtevano += Close;
    }
}
