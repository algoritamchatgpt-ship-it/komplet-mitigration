using System.Windows;
using System.Windows.Controls;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdOsView : Window
{
    public LdOsView()
    {
        InitializeComponent();
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e) => Close();

    private void StampaClick(object sender, RoutedEventArgs e)
    {
        var dlg = new PrintDialog();
        if (dlg.ShowDialog() != true) return;
        dlg.PrintVisual(StavkeGrid, "Obrazac OS — ZSP umanjenje poreza");
    }
}
