using GlavnaKnjiga.ViewModels;
using System.Windows;

namespace GlavnaKnjiga.Views;

public partial class NalanparWindow : Window
{
    public NalanparWindow(NalanparViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }

    private void TxtVrnal_LostFocus(object sender, RoutedEventArgs e)
        => ((NalanparViewModel)DataContext).OnVrnalLostFocus();
}
