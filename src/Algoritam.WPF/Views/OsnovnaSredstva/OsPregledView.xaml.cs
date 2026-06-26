using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.OsnovnaSredstva;

public partial class OsPregledView : Window
{
    public OsPregledView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is OsPregledViewModel vm)
            vm.ZatvaranjeZahtevano += Close;
    }
}
