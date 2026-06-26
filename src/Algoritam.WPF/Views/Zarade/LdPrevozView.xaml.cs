using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdPrevozView : Window
{
    public LdPrevozView()
    {
        InitializeComponent();
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
