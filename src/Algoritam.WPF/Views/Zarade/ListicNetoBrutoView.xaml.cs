using Algoritam.Domain.Entities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Algoritam.WPF.Views.Zarade;

public partial class ListicNetoBrutoView : Window
{
    public string FirmaTekst { get; }
    public string RadnikTekst { get; }
    public string PeriodTekst { get; }
    public string DatumIsplateTekst { get; }
    public string Napomena { get; }
    public ObservableCollection<RowStavka> Stavke { get; }

    public ListicNetoBrutoView(
        LdObracunStavka stavka,
        Radnik? radnik,
        LdParametar? parametar,
        Firma? firma,
        DateTime? datumIsplate)
    {
        FirmaTekst = string.IsNullOrWhiteSpace(firma?.Naziv) ? "Firma" : firma.Naziv!;
        RadnikTekst = $"{stavka.Broj} - {(string.IsNullOrWhiteSpace(radnik?.ImePrezime) ? stavka.ImePrez : radnik!.ImePrezime)}";
        PeriodTekst = $"Mesec {stavka.Mesec} / Isplata {stavka.Isplata} / Godina {stavka.Godina}";
        DatumIsplateTekst = datumIsplate.HasValue
            ? $"Datum isplate: {datumIsplate.Value.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture)}"
            : "Datum isplate: -";

        var netoSaPrevozom = stavka.Netosve != 0m ? stavka.Netosve : stavka.Neto + stavka.Prevoz;
        var ukupnoDavanja = stavka.Porez + stavka.Dopsocr;
        var brutoNetoKoef = stavka.Bruto == 0m ? 0m : (stavka.Neto / stavka.Bruto) * 100m;
        var trosakPoslodavca = stavka.Bruto + stavka.Dopsocf;

        Stavke =
        [
            new RowStavka("Bruto zarada", stavka.Bruto),
            new RowStavka("Neto zarada", stavka.Neto),
            new RowStavka("Neto + prevoz", netoSaPrevozom),
            new RowStavka("Porez", stavka.Porez),
            new RowStavka("Doprinosi radnik", stavka.Dopsocr),
            new RowStavka("Doprinosi firma", stavka.Dopsocf),
            new RowStavka("Ukupno davanja radnik (porez + doprinosi)", ukupnoDavanja),
            new RowStavka("Ukupne obustave", stavka.Ukobust),
            new RowStavka("Za isplatu", stavka.Zaisplatu),
            new RowStavka("Ukupan trosak poslodavca", trosakPoslodavca),
            new RowStavka("Efektivni neto/bruto (%)", brutoNetoKoef)
        ];

        var vrstaTekst = string.IsNullOrWhiteSpace(parametar?.Vrstaplate) ? "-" : parametar!.Vrstaplate;
        Napomena = $"Vrsta isplate: {vrstaTekst}. Ovaj pregled prikazuje kljucne NETO/BRUTO odnose za izabranog radnika.";

        InitializeComponent();
        DataContext = this;
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

    private void Stampaj()
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
            return;

        dialog.PrintVisual(PrintRoot, $"Listic neto-bruto - {RadnikTekst}");
    }

    public sealed class RowStavka(string naziv, decimal vrednost)
    {
        public string Naziv { get; } = naziv;
        public decimal Vrednost { get; } = vrednost;
    }
}
