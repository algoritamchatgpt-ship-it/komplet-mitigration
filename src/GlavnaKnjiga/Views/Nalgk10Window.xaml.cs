using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class Nalgk10Window : Window
{
    private readonly Nalgk10ViewModel _vm;

    public Nalgk10Window(Nalgk10ViewModel vm)
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
            case Key.F4:
                _vm.KontniPlanCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F5:
                _vm.KnjiziF5Command.Execute(null);
                e.Handled = true;
                break;
            case Key.F6 when Keyboard.Modifiers == ModifierKeys.None:
                _vm.KontoF6Command.Execute(null);
                e.Handled = true;
                break;
            case Key.F7 when Keyboard.Modifiers == ModifierKeys.None:
                _vm.TraziKontoF7Command.Execute(null);
                e.Handled = true;
                break;
            case Key.F7 when Keyboard.Modifiers == ModifierKeys.Shift:
                _vm.PrazniNalogCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F9:
                _vm.TraziNalogF9Command.Execute(null);
                e.Handled = true;
                break;
            case Key.F10:
                _vm.NalogF10Command.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void GrdMain_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_vm.SelectedRow == null) return;
        GrdMain.ScrollIntoView(_vm.SelectedRow);
    }

    private void GrdMain_CellEditEnding(object sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == System.Windows.Controls.DataGridEditAction.Commit)
            Dispatcher.InvokeAsync(() => _vm.SnimiNalgk10Dbf());
    }
}
