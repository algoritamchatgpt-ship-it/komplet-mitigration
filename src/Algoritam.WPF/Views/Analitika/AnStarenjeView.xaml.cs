using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Analitika;

public partial class AnStarenjeView : Window
{
    public AnStarenjeView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is AnStarenjeViewModel vm)
            vm.ZatvaranjeZahtevano += Close;
    }
}
