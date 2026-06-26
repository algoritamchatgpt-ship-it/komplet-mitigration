using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdZ1View : Window
{
    public LdZ1View()
    {
        InitializeComponent();
    }

    private void IzlazClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.LdZ1ViewModel vm)
            vm.ZatvaranjeZatrazeno += Close;

        Close();
    }
}
