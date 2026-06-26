using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalmtrWindow : Window
{
    private readonly NalmtrViewModel _vm;
    private readonly DataGridTextColumn[] _iznCols;

    public NalmtrWindow(NalmtrViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        _iznCols = new[]
        {
            ColIznos01, ColIznos02, ColIznos03, ColIznos04, ColIznos05,
            ColIznos06, ColIznos07, ColIznos08, ColIznos09, ColIznos10,
            ColIznos11, ColIznos12, ColIznos13, ColIznos14, ColIznos15,
            ColIznos16, ColIznos17, ColIznos18, ColIznos19, ColIznos20,
            ColIznos21, ColIznos22, ColIznos23, ColIznos24, ColIznos25,
            ColIznos26, ColIznos27, ColIznos28, ColIznos29, ColIznos30,
        };

        vm.ZatvoriFormu    += Close;
        vm.KoloneAzuriraneEvent += ApplyKoloneNazive;
        Loaded             += (_, _) => ApplyKoloneNazive();
    }

    private void ApplyKoloneNazive()
    {
        var nazivi = _vm.KoloneNazivi;
        for (int i = 0; i < _iznCols.Length && i < nazivi.Count; i++)
            _iznCols[i].Header = nazivi[i];
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5) { _vm.TraziKontoF5Command.Execute(null); e.Handled = true; }
        if (e.Key == Key.F9) { _vm.TraziNalogF9Command.Execute(null); e.Handled = true; }
        if (e.Key == Key.F8) { _vm.TraziDatumF8Command.Execute(null); e.Handled = true; }
    }

    private void GrdMain_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit && e.Row.Item is GlavnaKnjiga.Models.NalmtrRow row)
        {
            Dispatcher.InvokeAsync(() =>
            {
                row.RecalcUkupno();
                _vm.SnimiNalmtr();
            });
        }
    }

    private void GrdMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var total = GrdMain.Items.Count;
        var idx   = GrdMain.SelectedIndex;
        LblRec.Text = idx >= 0 ? $" {idx + 1,6}/{total,6} " : string.Empty;
    }
}
