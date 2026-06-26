using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class OpnrView : Window
{
    public OpnrView() { InitializeComponent(); }
    private void ZatvoriClick(object sender, RoutedEventArgs e) => Close();
}
