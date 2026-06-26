using Algoritam.WPF.Utilities;
using Algoritam.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Algoritam.WPF.Views.Zarade;

public partial class PartneriView : Window
{
    public PartneriView()
    {
        InitializeComponent();
        Closing += (_, _) => WindowPlacement.Save(this, "Partneri");
    }

    private async void PartneriView_Loaded(object sender, RoutedEventArgs e)
    {
        WindowPlacement.Restore(this, "Partneri", defaultWidth: 1360, defaultHeight: 760);
        if (DataContext is PartneriViewModel vm)
            await vm.InitAsync();
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is PartneriViewModel vm && vm.IzmeniCommand.CanExecute(null))
            vm.IzmeniCommand.Execute(null);
    }
}
