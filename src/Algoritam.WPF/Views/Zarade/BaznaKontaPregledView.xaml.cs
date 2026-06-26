using Algoritam.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Algoritam.WPF.Views.Zarade;

public partial class BaznaKontaPregledView : Window
{
    private readonly IReadOnlyList<BaznaKontaStavka> _stavke;

    public BaznaKontaPregledView(ObservableCollection<BaznaKontaStavka> stavke)
    {
        InitializeComponent();
        _stavke = stavke;
        PopuniPanel();
        TxtBroj.Text = $"Ukupno: {stavke.Count} konta";
    }

    private void PopuniPanel()
    {
        PanelSadrzaj.Children.Clear();

        // ── Naslov ──
        PanelSadrzaj.Children.Add(new TextBlock
        {
            Text = "BAZNI ŠIFARNIK KONTA KNJIŽENJA",
            FontFamily = new FontFamily("Tahoma"),
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 4),
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        PanelSadrzaj.Children.Add(new TextBlock
        {
            Text = $"Datum: {DateTime.Today:dd.MM.yyyy}",
            FontFamily = new FontFamily("Tahoma"),
            FontSize = 10,
            Foreground = Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12),
        });

        // ── Header tabele ──
        var grid = new Grid { Width = double.NaN };
        AddColumns(grid, 55, 80, double.NaN, 110, 110);

        grid.Children.Add(MakeHeader("Vrsta",  0));
        grid.Children.Add(MakeHeader("Kod",    1));
        grid.Children.Add(MakeHeader("Opis",   2));
        grid.Children.Add(MakeHeader("Konto",  3));
        grid.Children.Add(MakeHeader("Kontop", 4));

        PanelSadrzaj.Children.Add(grid);

        // ── Separator ──
        PanelSadrzaj.Children.Add(new Separator
        {
            Background = Brushes.DarkSlateBlue,
            Height = 1,
            Margin = new Thickness(0, 2, 0, 0),
        });

        // ── Redovi ──
        bool alt = false;
        foreach (var s in _stavke)
        {
            var row = new Grid { Width = double.NaN, Background = alt ? new SolidColorBrush(Color.FromRgb(245, 249, 255)) : Brushes.White };
            AddColumns(row, 55, 80, double.NaN, 110, 110);

            row.Children.Add(MakeCell(s.Vrsta,  0, alt));
            row.Children.Add(MakeCell(s.Kod,    1, alt));
            row.Children.Add(MakeCell(s.Opis,   2, alt));
            row.Children.Add(MakeCell(s.Konto,  3, alt));
            row.Children.Add(MakeCell(s.Kontop, 4, alt));

            PanelSadrzaj.Children.Add(row);
            alt = !alt;
        }

        // ── Footer ──
        PanelSadrzaj.Children.Add(new Separator
        {
            Background = Brushes.DarkSlateBlue,
            Height = 1,
            Margin = new Thickness(0, 4, 0, 4),
        });

        PanelSadrzaj.Children.Add(new TextBlock
        {
            Text = $"Ukupno konta: {_stavke.Count}",
            FontFamily = new FontFamily("Tahoma"),
            FontSize = 11,
            FontWeight = FontWeights.Bold,
        });
    }

    private static void AddColumns(Grid g, params double[] widths)
    {
        foreach (var w in widths)
            g.ColumnDefinitions.Add(double.IsNaN(w)
                ? new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                : new ColumnDefinition { Width = new GridLength(w) });
    }

    private static TextBlock MakeHeader(string text, int col)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily("Tahoma"),
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
            Padding = new Thickness(4, 2, 4, 2),
        };
        Grid.SetColumn(tb, col);
        return tb;
    }

    private static TextBlock MakeCell(string text, int col, bool alt)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily("Tahoma"),
            FontSize = 10,
            Padding = new Thickness(4, 1, 4, 1),
            Foreground = Brushes.Black,
        };
        Grid.SetColumn(tb, col);
        return tb;
    }

    private void ZatvoriClick(object sender, RoutedEventArgs e) => Close();

    private void StampajClick(object sender, RoutedEventArgs e)
    {
        var pd = new PrintDialog();
        if (pd.ShowDialog() != true)
            return;

        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Tahoma"),
            FontSize = 10,
            PagePadding = new Thickness(40),
            ColumnWidth = double.MaxValue,
        };

        var title = new Paragraph(new Run("BAZNI ŠIFARNIK KONTA KNJIŽENJA"))
        {
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8),
        };
        doc.Blocks.Add(title);

        var table = new Table { CellSpacing = 0, BorderBrush = Brushes.DarkSlateBlue, BorderThickness = new Thickness(1) };
        table.Columns.Add(new TableColumn { Width = new GridLength(40) });
        table.Columns.Add(new TableColumn { Width = new GridLength(60) });
        table.Columns.Add(new TableColumn { Width = new GridLength(180) });
        table.Columns.Add(new TableColumn { Width = new GridLength(80) });
        table.Columns.Add(new TableColumn { Width = new GridLength(80) });

        var rg = new TableRowGroup();

        var hRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(21, 101, 192)) };
        foreach (var h in new[] { "Vrsta", "Kod", "Opis", "Konto", "Kontop" })
            hRow.Cells.Add(new TableCell(new Paragraph(new Run(h)) { FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(4, 2, 4, 2) }));
        rg.Rows.Add(hRow);

        bool alt = false;
        foreach (var s in _stavke)
        {
            var r = new TableRow { Background = alt ? new SolidColorBrush(Color.FromRgb(245, 249, 255)) : Brushes.White };
            foreach (var v in new[] { s.Vrsta, s.Kod, s.Opis, s.Konto, s.Kontop })
                r.Cells.Add(new TableCell(new Paragraph(new Run(v)) { Margin = new Thickness(4, 1, 4, 1) }));
            rg.Rows.Add(r);
            alt = !alt;
        }

        table.RowGroups.Add(rg);
        doc.Blocks.Add(table);

        var docPaginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
        docPaginator.PageSize = new Size(pd.PrintableAreaWidth, pd.PrintableAreaHeight);
        pd.PrintDocument(docPaginator, "Bazni šifarnik konta");
    }
}
