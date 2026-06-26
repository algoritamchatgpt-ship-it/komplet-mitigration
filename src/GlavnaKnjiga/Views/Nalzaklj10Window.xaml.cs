using System.Windows;

namespace GlavnaKnjiga.Views;

public partial class Nalzaklj10Window : Window
{
    public Nalzaklj10Window() => InitializeComponent();

    private void BtnPregled_Click(object sender, RoutedEventArgs e)
    {
        var konp = TxtKonp.Text.Trim();
        var dat0 = TxtDat0.SelectedDate?.ToString("dd.MM.yyyy") ?? "";
        var dat1 = TxtDat1.SelectedDate?.ToString("dd.MM.yyyy") ?? "";

        var filter = $"KONTO[{konp}]  DAT[{dat0}..{dat1}]";
        MessageBox.Show($"Štampa NALZAKLJ010\n{filter}", "OSNOVNI ZAKLJUČNI LIST",
            MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }

    private void BtnIzlaz_Click(object sender, RoutedEventArgs e) => Close();
}
