using Algoritam.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdPorp12ReportView : Window
{
    public ObservableCollection<LdPorp12Stavka> Stavke { get; }
    public string InfoTekst { get; }

    public LdPorp12ReportView(IReadOnlyCollection<LdPorp12Stavka> stavke, decimal avgBruto, decimal avgNeto)
    {
        Stavke = new ObservableCollection<LdPorp12Stavka>(stavke);
        InfoTekst = $"Stavki: {Stavke.Count} | Prosek bruto: {avgBruto:N2} | Prosek neto: {avgNeto:N2}";

        InitializeComponent();
        DataContext = this;
    }

    private void StampajClick(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
            return;

        dialog.PrintVisual(PrintRoot, "POTVRDA - ZARADA ZA 12 MESECI");
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e) => Close();
}
