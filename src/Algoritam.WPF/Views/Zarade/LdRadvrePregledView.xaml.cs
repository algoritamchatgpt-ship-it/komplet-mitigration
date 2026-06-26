using Algoritam.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdRadvrePregledView : Window
{
    private readonly ObservableCollection<RadnoVremeStavka> _sveStavke;

    public LdRadvrePregledView(ObservableCollection<RadnoVremeStavka> stavke)
    {
        InitializeComponent();
        _sveStavke = stavke;

        // Init: dat0 = prvi datum u tabeli, dat1 = zadnji
        var datumi = stavke.Select(s => s.Datum).Where(d => d > DateTime.MinValue).ToList();
        DatumOd.SelectedDate = datumi.Count > 0 ? datumi.Min() : DateTime.Today;
        DatumDo.SelectedDate = datumi.Count > 0 ? datumi.Max() : DateTime.Today;
    }

    // PREGLED: REPORT FORM LDRADVRE PREVIEW FOR datum>=mdat0 AND datum<=mdat1
    private void PregledClick(object sender, RoutedEventArgs e)
    {
        var dat0 = DatumOd.SelectedDate ?? DateTime.MinValue;
        var dat1 = DatumDo.SelectedDate ?? DateTime.MaxValue;

        var filtered = _sveStavke
            .Where(s => s.Datum.Date >= dat0.Date && s.Datum.Date <= dat1.Date)
            .ToList();

        GrdPregled.ItemsSource = filtered;
        TxtStatus.Text = $"Prikazano {filtered.Count} zapisa od {dat0:dd.MM.yyyy} do {dat1:dd.MM.yyyy}.";
    }

    // PREGLED SUMARNO: REPORT ... SUMMARY — sumira sate po radniku
    private void PregledSumarnoClick(object sender, RoutedEventArgs e)
    {
        var dat0 = DatumOd.SelectedDate ?? DateTime.MinValue;
        var dat1 = DatumDo.SelectedDate ?? DateTime.MaxValue;

        var sumarno = _sveStavke
            .Where(s => s.Datum.Date >= dat0.Date && s.Datum.Date <= dat1.Date)
            .GroupBy(s => new { s.Broj, s.ImePrez })
            .Select(g => new RadnoVremeStavka
            {
                Broj    = g.Key.Broj,
                ImePrez = g.Key.ImePrez,
                Datum   = dat0,
                PocSat  = 0,
                ZadSat  = 0,
                Sati    = g.Sum(s => s.Sati),
            })
            .OrderBy(s => s.Broj)
            .ToList();

        GrdPregled.ItemsSource = sumarno;
        TxtStatus.Text = $"Sumarno {sumarno.Count} radnika, period {dat0:dd.MM.yyyy}–{dat1:dd.MM.yyyy}.";
    }

    // IZLAZ: Release THISFORM
    private void IzlazClick(object sender, RoutedEventArgs e) => Close();
}
