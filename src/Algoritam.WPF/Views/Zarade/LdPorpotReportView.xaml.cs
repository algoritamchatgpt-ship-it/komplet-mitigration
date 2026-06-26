using Algoritam.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdPorpotReportView : Window
{
    public ObservableCollection<LdPorpotStavka> Stavke { get; }
    public string InfoTekst { get; }

    public LdPorpotReportView(IReadOnlyCollection<LdPorpotStavka> stavke)
    {
        Stavke = new ObservableCollection<LdPorpotStavka>(stavke);
        InfoTekst = $"Stavki: {Stavke.Count} | Ukupno radnik din: {Stavke.Sum(s => s.Raddin):N2} | Ukupno firma din: {Stavke.Sum(s => s.Firdin):N2}";

        InitializeComponent();
        DataContext = this;
    }

    private void StampajClick(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
            return;

        dialog.PrintVisual(PrintRoot, "POTVRDA - KRETANJE ZARADA");
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e) => Close();
}
