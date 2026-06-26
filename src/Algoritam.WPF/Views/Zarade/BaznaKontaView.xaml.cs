using Algoritam.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Algoritam.WPF.Views.Zarade;

public partial class BaznaKontaView : Window
{
    public BaznaKontaView()
    {
        InitializeComponent();
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e) => Close();

    private void PregledKontaClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not BaznaKontaViewModel vm || vm.Stavke.Count == 0)
        {
            MessageBox.Show("Nema konta za prikaz.", "Pregled konta",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var pregled = new BaznaKontaPregledView(vm.Stavke);
        pregled.Owner = this;
        pregled.ShowDialog();
    }
}
