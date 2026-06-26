using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GlavnaKnjiga.Views;

public partial class Nalp2Window : Window
{
    private Nalp2ViewModel Vm => (Nalp2ViewModel)DataContext;

    public Nalp2Window(Nalp2ViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }

    // ── Grid SelectionChanged → AfterRowColChange ─────────────
    private void Grd0_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectedRow je already updated via TwoWay binding; VM handles the rest.
    }

    // ── CellEditEnding → poziva poslovne događaje ─────────────
    private void Grd0_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit) return;
        if (e.Row.Item is not NalpRow row) return;

        var colIdx = e.Column.DisplayIndex;

        // Commit edit value to the binding before we call business logic
        e.Column.GetCellContent(e.Row)?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

        switch (colIdx)
        {
            case 0: // Konto
                Dispatcher.InvokeAsync(() => Vm.OnKontoLostFocus(row));
                break;
            case 1: // Dug
                Dispatcher.InvokeAsync(() => Vm.OnDugLostFocus(row));
                break;
            case 2: // Pot
                Dispatcher.InvokeAsync(() => Vm.OnPotLostFocus(row));
                break;
            case 9: // Dev
                Dispatcher.InvokeAsync(() => Vm.OnDevLostFocus(row));
                break;
        }
    }

    // ── KeyDown — F5, Shift+F5, Shift+F7, F4 ─────────────────
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        bool shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        bool alt   = Keyboard.IsKeyDown(Key.LeftAlt)   || Keyboard.IsKeyDown(Key.RightAlt);
        bool ctrl  = Keyboard.IsKeyDown(Key.LeftCtrl)  || Keyboard.IsKeyDown(Key.RightCtrl);

        if (e.Key == Key.F5 && !shift && !alt && !ctrl)
        {
            Vm.KnjiziCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F5 && shift)
        {
            Vm.KopirajRedCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F7 && shift)
        {
            Vm.PrazniNalogCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F6)
        {
            Vm.DodajZbirCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F4)
        {
            Vm.KontniPlanCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F10 && alt)
        {
            Vm.DevizniNalogCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F10)
        {
            Vm.NalogF10Command.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F9 && alt)
        {
            Vm.ParametriCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F2 && alt)
        {
            Vm.NalpMatulCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F3 && alt)
        {
            Vm.NalpMatizCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F6 && alt)
        {
            Vm.NalpDopanCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Vm.IzlazCommand.Execute(null);
            e.Handled = true;
        }
    }
}
