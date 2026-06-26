using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class ZdravstveneKnjiziceView : Window
{
    public ZdravstveneKnjiziceView()
    {
        InitializeComponent();
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e) => Close();
}
