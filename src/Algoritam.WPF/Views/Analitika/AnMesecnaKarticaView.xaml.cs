using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Analitika;

public partial class AnMesecnaKarticaView : Window
{
    public AnMesecnaKarticaView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is AnMesecnaKarticaViewModel vm)
            vm.ZatvaranjeZahtevano += Close;
    }
}
