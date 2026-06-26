using Algoritam.WPF.Utilities;
using Algoritam.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Algoritam.WPF.Views.Zarade;

public partial class KreditiView : Window
{
    public KreditiView()
    {
        InitializeComponent();
        Closing += (_, _) =>
        {
            (DataContext as KreditiViewModel)?.OslobodiLokove();
            WindowPlacement.Save(this, "Krediti");
        };
    }

    private async void KreditiView_Loaded(object sender, RoutedEventArgs e)
    {
        WindowPlacement.Restore(this, "Krediti", defaultWidth: 1520, defaultHeight: 820);
        if (DataContext is KreditiViewModel vm)
            await vm.InitAsync();
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void DataGridPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        if (sender is not DataGrid grid)
            return;

        if (grid.CurrentColumn is not DataGridBoundColumn { Binding: Binding binding })
            return;

        if (!string.Equals(binding.Path?.Path, "Sifra", StringComparison.OrdinalIgnoreCase))
            return;

        grid.CommitEdit(DataGridEditingUnit.Cell, true);
        grid.CommitEdit(DataGridEditingUnit.Row, true);

        if (DataContext is not KreditiViewModel vm)
            return;

        if (!vm.IzaberiPartneraCommand.CanExecute(null))
            return;

        vm.IzaberiPartneraCommand.Execute(null);
        e.Handled = true;
    }

    private void DataGridMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is not KreditiViewModel vm)
            return;

        if (!vm.JedanKreditCommand.CanExecute(null))
            return;

        vm.JedanKreditCommand.Execute(null);
    }
}
