using Algoritam.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdNz1ReportView : Window
{
    public ObservableCollection<LdNz1Stavka> Stavke { get; }
    public string InfoTekst { get; }

    public LdNz1ReportView(IReadOnlyCollection<LdNz1Stavka> stavke)
    {
        Stavke = new ObservableCollection<LdNz1Stavka>(stavke);
        InfoTekst = $"Stavki: {Stavke.Count} | Ukupno obaveze: {Stavke.Sum(s => s.Obaveze):N2}";

        InitializeComponent();
        DataContext = this;
    }

    private void StampajClick(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
            return;

        dialog.PrintVisual(PrintRoot, "OBRAZAC NZ1-1");
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e) => Close();
}
