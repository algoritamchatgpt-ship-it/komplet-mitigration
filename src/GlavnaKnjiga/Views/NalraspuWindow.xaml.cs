using GlavnaKnjiga.ViewModels;
using System.Windows;

namespace GlavnaKnjiga.Views;

public partial class NalraspuWindow : Window
{
    private readonly NalraspViewModel _vm;

    public NalraspuWindow(NalraspViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
    }

    private void BtnPreuzmi_Click(object sender, RoutedEventArgs e)
    {
        var dat0  = TxtDat0.SelectedDate ?? DateTime.Today;
        var dat1  = TxtDat1.SelectedDate ?? DateTime.Today;
        var brnal = TxtBrnal.Text.Trim();
        decimal.TryParse(TxtMtr.Text.Trim(), out var mtr);

        Close();
        _vm.IzvrsiPreuzimanje(dat0, dat1, mtr, brnal);
    }

    private void BtnIzlaz_Click(object sender, RoutedEventArgs e) => Close();
}
