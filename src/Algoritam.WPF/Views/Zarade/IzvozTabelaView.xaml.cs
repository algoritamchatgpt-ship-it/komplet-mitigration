using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class IzvozTabelaView : Window
{
    public IzvozTabelaView()
    {
        InitializeComponent();
    }

    private void IzlazClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
