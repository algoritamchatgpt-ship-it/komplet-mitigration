using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class OpjView : Window
{
    public OpjView() { InitializeComponent(); }
    private void ZatvoriClick(object sender, RoutedEventArgs e) => Close();
}
