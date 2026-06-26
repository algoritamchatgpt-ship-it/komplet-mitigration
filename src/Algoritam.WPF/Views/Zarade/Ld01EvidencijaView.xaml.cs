using Algoritam.WPF.ViewModels;
using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class Ld01EvidencijaView : Window
{
    public Ld01EvidencijaView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is Ld01EvidencijaViewModel vm)
                vm.ZatvoriAction = Close;
        };
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e) => Close();
}
