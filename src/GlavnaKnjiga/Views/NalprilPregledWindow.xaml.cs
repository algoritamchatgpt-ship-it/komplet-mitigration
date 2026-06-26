using GlavnaKnjiga.ViewModels;
using System.Printing;
using System.Windows;
using System.Windows.Controls;

namespace GlavnaKnjiga.Views;

public partial class NalprilPregledWindow : Window
{
    public NalprilPregledWindow(NalprilPregledViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
    }

    private void OnStampajClick(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;
        dialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
        dialog.PrintVisual(GridPregled, DataContext is NalprilPregledViewModel vm
            ? vm.Naslov
            : "Plan likvidnosti");
    }
}
