using GlavnaKnjiga.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GlavnaKnjiga.Views;

public partial class NalmtrPregledWindow : Window
{
    public NalmtrPregledWindow(NalmtrPregledViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.ZatvoriFormu += Close;
        FormirajKolone(vm.KoloneNazivi);
    }

    private void FormirajKolone(IReadOnlyList<string> nazivi)
    {
        GrdMain.Columns.Add(Kolona("Konto", "Konto", 100));
        GrdMain.Columns.Add(Kolona("Naziv", "Naziv", 220));
        GrdMain.Columns.Add(Kolona("Duguje", "Dug", 110, "N2"));
        GrdMain.Columns.Add(Kolona("Potražuje", "Pot", 110, "N2"));
        GrdMain.Columns.Add(Kolona("Saldo", "Saldo", 110, "N2"));

        for (var i = 1; i <= 30; i++)
        {
            var naslov = i <= nazivi.Count && !string.IsNullOrWhiteSpace(nazivi[i - 1])
                ? nazivi[i - 1]
                : $"Iznos{i:D2}";
            GrdMain.Columns.Add(Kolona(naslov, $"Iznos{i:D2}", 115, "N2"));
        }

        GrdMain.Columns.Add(Kolona("Ukupno", "Ukupno", 120, "N2"));
    }

    private static DataGridTextColumn Kolona(
        string header, string path, double width, string? format = null)
    {
        var binding = new Binding(path);
        if (format != null) binding.StringFormat = format;
        return new DataGridTextColumn
        {
            Header = header,
            Binding = binding,
            Width = width,
        };
    }
}
