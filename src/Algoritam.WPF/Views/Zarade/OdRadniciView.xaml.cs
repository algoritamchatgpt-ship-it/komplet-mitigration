using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class OdRadniciView : Window
{
    public OdRadniciView()
    {
        InitializeComponent();
    }

    private void Zatvori_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
