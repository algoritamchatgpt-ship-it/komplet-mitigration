using System.Windows;

namespace GlavnaKnjiga.Views;

public partial class NalzakljWindow : Window
{
    public NalzakljWindow() => InitializeComponent();

    private void BtnPregled_Click(object sender, RoutedEventArgs e)
    {
        var konp    = TxtKonp.Text.Trim();
        var dat0    = TxtDat0.SelectedDate?.ToString("dd.MM.yyyy") ?? "";
        var dat1    = TxtDat1.SelectedDate?.ToString("dd.MM.yyyy") ?? "";
        var dok     = TxtDok.Text.Trim();
        var salobj  = TxtSaldoObj.Text.Trim().ToUpper();
        var mp      = TxtMp.Text.Trim();
        var mtr     = TxtMtr.Text.Trim();
        var kurs    = TxtKurs.Text.Trim().ToUpper();
        var brnal1  = TxtBrnal1.Text.Trim();
        var brnal2  = TxtBrnal2.Text.Trim();

        var filter = $"KONTO[{konp}]  DAT[{dat0}..{dat1}]  DOK[{dok}]  " +
                     $"SALOBJ[{salobj}]  MP[{mp}]  MTR[{mtr}]  KURS[{kurs}]  " +
                     $"IZUZETI[{brnal1},{brnal2}]";

        MessageBox.Show($"Štampa NALZAKLJ0\n{filter}", "OSNOVNI ZAKLJUČNI LIST",
            MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }

    private void BtnIzlaz_Click(object sender, RoutedEventArgs e) => Close();
}
