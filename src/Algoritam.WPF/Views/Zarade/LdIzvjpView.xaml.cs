using System.Windows;
using System.Windows.Controls;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdIzvjpView : Window
{
    public LdIzvjpView()
    {
        InitializeComponent();
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e) => Close();

    private void StampaClick(object sender, RoutedEventArgs e)
    {
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() != true) return;
        dlg.PrintVisual(StavkeGrid, "Izjava o isplaćenim zaradama");
    }
}
