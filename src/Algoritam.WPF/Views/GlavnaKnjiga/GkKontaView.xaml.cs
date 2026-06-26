using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.GlavnaKnjiga;

public partial class GkKontaView : Window
{
    public GkKontaView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is GkKontaViewModel vm)
            vm.ZatvaranjeZahtevano += Close;
    }
}
