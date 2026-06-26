using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GlavnaKnjiga.Views;

public partial class NalmatWindow : Window
{
    private readonly NalmatViewModel _vm;

    public NalmatWindow(NalmatViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }

    private void GrdMain_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit)
            Dispatcher.InvokeAsync(_vm.SnimiIzmene);
    }
}
