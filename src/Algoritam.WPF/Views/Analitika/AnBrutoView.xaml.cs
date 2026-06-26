using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Analitika;

public partial class AnBrutoView : Window
{
    public AnBrutoView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is AnBrutoViewModel vm)
            vm.ZatvaranjeZahtevano += Close;
    }
}
