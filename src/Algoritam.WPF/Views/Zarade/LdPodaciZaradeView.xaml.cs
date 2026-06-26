using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdPodaciZaradeView : Window
{
    public LdPodaciZaradeView()
    {
        InitializeComponent();
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
