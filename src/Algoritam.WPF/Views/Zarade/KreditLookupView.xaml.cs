using Algoritam.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Algoritam.WPF.Views.Zarade;

public partial class KreditLookupView : Window
{
    public KreditLookupView()
    {
        InitializeComponent();
    }

    private void DataGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not KreditLookupViewModel vm || vm.Izabrana == null)
            return;

        DialogResult = true;
    }
}
