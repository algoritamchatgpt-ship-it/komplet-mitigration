using System.Windows;

namespace GlavnaKnjiga.Views;

public partial class NalzakljbWindow : Window
{
    public NalzakljbWindow() => InitializeComponent();

    private void BtnPregled_Click(object sender, RoutedEventArgs e)
    {
        var dat0 = TxtDat0.SelectedDate?.ToString("dd.MM.yyyy") ?? "";
        var dat1 = TxtDat1.SelectedDate?.ToString("dd.MM.yyyy") ?? "";
        MessageBox.Show($"Štampa NALZAKLJ0B\nDAT[{dat0}..{dat1}]", "BRZI ZAKLJUČNI LIST",
            MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }

    private void BtnIzlaz_Click(object sender, RoutedEventArgs e) => Close();
}
