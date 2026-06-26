using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Komunal;

public partial class UsKorisniciView : Window
{
    public UsKorisniciView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is UsKorisniciViewModel vm)
            vm.ZatvaranjeZahtevano += Close;
    }
}
