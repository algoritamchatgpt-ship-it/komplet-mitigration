using Algoritam.WPF.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Algoritam.WPF.Views.Zarade;

public partial class FoxPregledTabelaView : Window
{
    public string Naslov { get; }
    public string Podnaslov { get; }
    public string LabelIznos1 { get; }
    public string LabelIznos2 { get; }
    public ObservableCollection<PregledTabelaStavka> Stavke { get; }
    public string BrojStavkiTekst { get; }
    public string Ukupno1Tekst { get; }
    public string Ukupno2Tekst { get; }

    public FoxPregledTabelaView(
        string naslov,
        string podnaslov,
        IReadOnlyCollection<PregledTabelaStavka> stavke,
        string labelIznos1,
        string labelIznos2)
    {
        Naslov = naslov;
        Podnaslov = podnaslov;
        LabelIznos1 = string.IsNullOrWhiteSpace(labelIznos1) ? "IZNOS 1" : labelIznos1;
        LabelIznos2 = string.IsNullOrWhiteSpace(labelIznos2) ? "IZNOS 2" : labelIznos2;
        Stavke = new ObservableCollection<PregledTabelaStavka>(stavke);

        var ukupno1 = Stavke.Sum(s => s.Iznos1);
        var ukupno2 = Stavke.Sum(s => s.Iznos2);

        BrojStavkiTekst = $"Stavki: {Stavke.Count}";
        Ukupno1Tekst = $"{LabelIznos1}: {ukupno1:N2}";
        Ukupno2Tekst = $"{LabelIznos2}: {ukupno2:N2}";

        InitializeComponent();
        Iznos1Column.Header = LabelIznos1;
        Iznos2Column.Header = LabelIznos2;
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

        dialog.PrintVisual(PrintRoot, Naslov);
    }
}
