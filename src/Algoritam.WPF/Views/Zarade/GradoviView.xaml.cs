using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class GradoviView : Window
{
    public GradoviView()
    {
        InitializeComponent();
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
