using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Analitika;

public partial class AnKarticaView : Window
{
    public AnKarticaView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is AnKarticaViewModel vm)
            vm.ZatvaranjeZahtevano += Close;
    }
}
