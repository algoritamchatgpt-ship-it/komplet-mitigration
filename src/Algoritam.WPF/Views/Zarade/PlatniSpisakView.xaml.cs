using Algoritam.WPF.Utilities;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Algoritam.WPF.Views.Zarade;

public partial class PlatniSpisakView : Window
{
    public PlatniSpisakView()
    {
        InitializeComponent();
        Loaded += (_, _) => WindowPlacement.Restore(this, "PlatniSpisak", defaultWidth: 1500, defaultHeight: 820);
        Closing += (_, _) => WindowPlacement.Save(this, "PlatniSpisak");
    }

    private async void PlatniSpisakView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.PlatniSpisakViewModel vm) return;
        await vm.InitAsync();
        SetColumnHeader("ColNazo1", vm.NazoLabel1);
        SetColumnHeader("ColNazo2", vm.NazoLabel2);
        SetColumnHeader("ColNazo3", vm.NazoLabel3);
        SetColumnHeader("ColNazo4", vm.NazoLabel4);
        SetColumnHeader("ColNazo5", vm.NazoLabel5);
        SetColumnHeader("ColNazo6", vm.NazoLabel6);
    }

    private void SetColumnHeader(string columnName, string header)
    {
        if (FindName(columnName) is DataGridTextColumn col)
            col.Header = header;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F2
            && Keyboard.Modifiers == ModifierKeys.Shift
            && DataContext is ViewModels.PlatniSpisakViewModel vm0
            && vm0.PrenosKreditaCommand.CanExecute(null))
        {
            vm0.PrenosKreditaCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F3
            && Keyboard.Modifiers == ModifierKeys.Shift
            && DataContext is ViewModels.PlatniSpisakViewModel vm
            && vm.JedanListicCommand.CanExecute(null))
        {
            vm.JedanListicCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F4
            && Keyboard.Modifiers == ModifierKeys.Shift
            && DataContext is ViewModels.PlatniSpisakViewModel vm2
            && vm2.PregledSviListiciCommand.CanExecute(null))
        {
            vm2.PregledSviListiciCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F8
            && Keyboard.Modifiers == ModifierKeys.None
            && DataContext is ViewModels.PlatniSpisakViewModel vm3
            && vm3.PopuniKolonuCommand.CanExecute(null))
        {
            vm3.PopuniKolonuCommand.Execute(null);
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
    }

    private void GridPlatniSpisak_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid || e.OriginalSource is not DependencyObject source)
            return;

        // Otvaraj karticu samo kada je dvoklik nad stvarnim redom grida.
        if (FindVisualParent<DataGridRow>(source) == null || grid.SelectedItem == null)
            return;

        if (DataContext is ViewModels.PlatniSpisakViewModel vm
            && vm.OtvoriKarticuCommand.CanExecute(null))
        {
            vm.OtvoriKarticuCommand.Execute(null);
            e.Handled = true;
        }
    }

    private static TParent? FindVisualParent<TParent>(DependencyObject? child)
        where TParent : DependencyObject
    {
        while (child != null)
        {
            if (child is TParent typed)
                return typed;

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }
}
