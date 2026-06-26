using System.Windows;
using System.Windows.Input;

namespace Algoritam.WPF.Views.Zarade;

public partial class PlatniSpisakPreglediView : Window
{
    public PlatniSpisakPreglediView()
    {
        InitializeComponent();
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
    }
}
