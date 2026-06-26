using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.GlavnaKnjiga;

public partial class GkNalogBlagajneView : Window
{
    public GkNalogBlagajneView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is GkNalogBlagajneViewModel vm)
            vm.ZatvaranjeZahtevano += Close;
    }
}
