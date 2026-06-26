using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class PutarKarticaView : Window
{
    public PutarKarticaView()
    {
        InitializeComponent();
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e) => Close();
}
