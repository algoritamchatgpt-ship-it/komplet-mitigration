using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.GlavnaKnjiga;

public partial class GkZakljucakView : Window
{
    public GkZakljucakView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is GkZakljucakViewModel vm)
            vm.ZatvaranjeZahtevano += Close;
    }
}
