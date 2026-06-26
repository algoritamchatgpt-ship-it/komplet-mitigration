using Algoritam.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LdObracunStavka = Algoritam.Domain.Entities.LdObracunStavka;

namespace Algoritam.WPF.Views.Zarade;

public partial class SviListiciPregledView : Window
{
    private readonly PlatniSpisakViewModel _vm;
    private readonly LdObracunStavka? _inicijalnaSelekcija;

    public SviListiciPregledView(PlatniSpisakViewModel vm)
    {
        _vm = vm;
        _inicijalnaSelekcija = vm.Selektovana;

        InitializeComponent();
        DataContext = vm;
    }

    private void OtvoriListicClick(object sender, RoutedEventArgs e)
    {
        OtvoriSelektovaniListic();
    }

    private void StampajListuClick(object sender, RoutedEventArgs e)
    {
        StampajListu();
    }

    private void GridStavkeMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        OtvoriSelektovaniListic();
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_inicijalnaSelekcija != null)
            _vm.Selektovana = _inicijalnaSelekcija;

        base.OnClosed(e);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F9)
        {
            StampajListu();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            OtvoriSelektovaniListic();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
    }

    private void OtvoriSelektovaniListic()
    {
        if (GridStavke.SelectedItem is not LdObracunStavka selektovani)
        {
            MessageBox.Show(
                "Izaberite radnika iz liste.",
                "Svi listici",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _vm.Selektovana = selektovani;
        if (_vm.JedanListicCommand.CanExecute(null))
            _vm.JedanListicCommand.Execute(null);
    }

    private void StampajListu()
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
            return;

        dialog.PrintVisual(GridStavke, "Svi listici - lista");
    }
}
