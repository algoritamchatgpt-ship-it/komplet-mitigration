using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdRadvreDodajView : Window
{
    public DateTime IzabraniDatum { get; private set; } = DateTime.Today;
    public int      IzabraniPocSat { get; private set; } = 0;

    public LdRadvreDodajView()
    {
        InitializeComponent();
        DatumPicker.SelectedDate = DateTime.Today;
    }

    // DODAVANJE: preuzima vrednosti i zatvara sa DialogResult=true
    private void DodavanjeClick(object sender, RoutedEventArgs e)
    {
        IzabraniDatum = DatumPicker.SelectedDate ?? DateTime.Today;

        if (!int.TryParse(TxtPocSat.Text.Trim(), out int pocSat))
        {
            MessageBox.Show("Unesite ispravan sat početka (broj).", "Greška",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtPocSat.Focus();
            return;
        }

        IzabraniPocSat = pocSat;
        DialogResult = true;
        Close();
    }

    // IZLAZ: Release THISFORM
    private void IzlazClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
