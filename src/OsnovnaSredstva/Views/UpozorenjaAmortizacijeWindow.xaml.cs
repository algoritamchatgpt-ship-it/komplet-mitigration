using OsnovnaSredstva.ViewModels;
using System.Windows;

namespace OsnovnaSredstva.Views;

public partial class UpozorenjaAmortizacijeWindow : Window
{
    public UpozorenjaAmortizacijeWindow(IReadOnlyList<UpozorenjeAmortizacije> stavke)
    {
        InitializeComponent();
        DgUpozorenja.ItemsSource = stavke;
        TxtBrojac.Text = $"Ukupno problematičnih kartica: {stavke.Count}";
    }

    private void OnZatvoriClick(object sender, RoutedEventArgs e) => Close();
}
