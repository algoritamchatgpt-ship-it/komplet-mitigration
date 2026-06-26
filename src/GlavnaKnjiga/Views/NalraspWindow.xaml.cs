using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class NalraspWindow : Window
{
    private readonly NalraspViewModel _vm;

    public NalraspWindow(NalraspViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.ZatvoriFormu += () => Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F5:
                _vm.KnjizenjeCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F6:
                _vm.PreuzimanjeCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void GrdMain_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_vm.SelectedRow != null)
            GrdMain.ScrollIntoView(_vm.SelectedRow);
    }

    private void GrdMain_CellEditEnding(object sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == System.Windows.Controls.DataGridEditAction.Commit)
            Dispatcher.InvokeAsync(() => _vm.SnimiNalraspDbf());
    }
}
