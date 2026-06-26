using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.GlavnaKnjiga;

public partial class GkDnevnikView : Window
{
    public GkDnevnikView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is GkDnevnikViewModel vm)
            vm.ZatvaranjeZahtevano += Close;
    }
}
