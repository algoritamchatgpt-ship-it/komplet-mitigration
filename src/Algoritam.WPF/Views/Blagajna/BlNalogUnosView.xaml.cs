using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Blagajna;

public partial class BlNalogUnosView : Window
{
    public BlNalogUnosView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is BlNalogUnosViewModel vm)
            vm.ZatvaranjeZahtevano += Close;
    }
}
