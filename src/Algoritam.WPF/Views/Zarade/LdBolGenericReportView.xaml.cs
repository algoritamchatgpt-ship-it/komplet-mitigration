using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdBolGenericReportView : Window
{
    public string Naslov { get; }
    public string BrojStavkiTekst { get; }
    public IEnumerable Stavke { get; }

    public LdBolGenericReportView(string naslov, IEnumerable stavke, int brojStavki)
    {
        Naslov = naslov;
        Stavke = stavke;
        BrojStavkiTekst = $"Stavki: {brojStavki}";

        InitializeComponent();
        Title = Naslov;
        DataContext = this;
    }

    private void StampajClick(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
            return;

        dialog.PrintVisual(PrintRoot, Naslov);
    }

    private void DataGridAutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.Column is not DataGridTextColumn textColumn || textColumn.Binding is not Binding binding)
            return;

        var tip = Nullable.GetUnderlyingType(e.PropertyType) ?? e.PropertyType;
        if (tip == typeof(decimal) || tip == typeof(double) || tip == typeof(float))
        {
            binding.StringFormat = "N2";
            binding.ConverterCulture = CultureInfo.GetCultureInfo("sr-RS");
            return;
        }

        if (tip == typeof(DateTime))
            binding.StringFormat = "dd.MM.yyyy";
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e) => Close();
}
