using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LdObracunStavka = Algoritam.Domain.Entities.LdObracunStavka;

namespace Algoritam.WPF.Views.Zarade;

public partial class PregledKoloneView : Window
{
    private readonly IReadOnlyList<LdObracunStavka> _stavke;
    private readonly IReadOnlyList<KolonaDefinicija> _kolone;

    public PregledKoloneView(IEnumerable<LdObracunStavka> stavke)
    {
        _stavke = stavke.OrderBy(s => s.Broj).ToList();
        _kolone =
        [
            new("BRUTO", s => s.Bruto),
            new("NETO", s => s.Neto),
            new("ZA ISPLATU", s => s.Zaisplatu),
            new("POREZ", s => s.Porez),
            new("DOPRINOSI RADNIK", s => s.Dopsocr),
            new("DOPRINOSI FIRMA", s => s.Dopsocf),
            new("UKUPNE OBUSTAVE", s => s.Ukobust),
            new("CASOVI UKUPNO", s => s.Casuk),
            new("DINARSKI UKUPNO", s => s.Dinuk),
            new("FIKSNA ZARADA", s => s.Fiksna),
            new("PREVOZ", s => s.Prevoz),
            new("AKONTACIJA", s => s.Akontac),
            new("KREDITI", s => s.Krediti)
        ];

        InitializeComponent();

        KolonaCombo.ItemsSource = _kolone;
        KolonaCombo.DisplayMemberPath = nameof(KolonaDefinicija.Naziv);
        KolonaCombo.SelectedIndex = 0;

        UcitajKolonu();
    }

    private void KolonaComboSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UcitajKolonu();
    }

    private void UcitajClick(object sender, RoutedEventArgs e)
    {
        UcitajKolonu();
    }

    private void StampajClick(object sender, RoutedEventArgs e)
    {
        Stampaj();
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            UcitajKolonu();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            Stampaj();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
    }

    private void UcitajKolonu()
    {
        if (KolonaCombo.SelectedItem is not KolonaDefinicija kolona)
            return;

        var rezultat = _stavke
            .Select(s => new KolonaRezultat(s.Broj, s.ImePrez, kolona.Selektor(s)))
            .ToList();

        GridRezultati.ItemsSource = rezultat;

        var ukupno = rezultat.Sum(r => r.Vrednost);
        NaslovKoloneText.Text = $"PREGLED KOLONE: {kolona.Naziv}";
        UkupnoText.Text = ukupno.ToString("N2");
        StatusText.Text = $"Prikazano stavki: {rezultat.Count}. F5 osvezava prikaz.";
    }

    private void Stampaj()
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
            return;

        dialog.PrintVisual(PrintRoot, NaslovKoloneText.Text);
    }

    private sealed record KolonaDefinicija(string Naziv, Func<LdObracunStavka, decimal> Selektor);
    private sealed record KolonaRezultat(int Broj, string ImePrez, decimal Vrednost);
}
