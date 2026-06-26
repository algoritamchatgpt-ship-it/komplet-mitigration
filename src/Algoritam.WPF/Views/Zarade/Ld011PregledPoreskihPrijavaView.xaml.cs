using Algoritam.WPF.ViewModels;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace Algoritam.WPF.Views.Zarade;

public partial class Ld011PregledPoreskihPrijavaView : Window
{
    public string FirmaNaziv { get; }
    public string FirmaMesto { get; }

    public IReadOnlyList<Ld011Red> Redovi { get; }

    public string SumBruto  { get; }
    public string SumPorez  { get; }
    public string SumDoppr  { get; }
    public string SumDopzr  { get; }
    public string SumDopnr  { get; }

    public Ld011PregledPoreskihPrijavaView(
        IEnumerable<Ld0Stavka> stavke,
        string firmaNaziv,
        string firmaMesto)
    {
        var lista = stavke?.ToList() ?? [];
        var ci    = CultureInfo.CurrentCulture;
        string N2(decimal v) => v.ToString("N2", ci);

        FirmaNaziv = firmaNaziv;
        FirmaMesto = firmaMesto;

        Redovi = lista.Select(s => new Ld011Red
        {
            Brobrac  = s.Brobrac.ToString(),
            Mesec    = s.Mesec.ToString(),
            Nazmes   = s.Nazmes,
            Vrsta    = s.Vrsta,
            Bruto    = N2(s.Bruto),
            Porez    = N2(s.Porez),
            Doppr    = N2(s.Doppr),
            Dopzr    = N2(s.Dopzr),
            Dopnr    = N2(s.Dopnr),
            Dat1     = s.Dat1?.ToString("dd.MM.yyyy") ?? "",
            Ppopj1   = s.Ppopj1,
            Ppod01   = s.Ppod01,
            Ppod01v  = s.Ppod01v,
            Ppod11   = s.Ppod11,
            Godina   = s.Godina,
        }).ToList();

        SumBruto  = N2(lista.Sum(s => s.Bruto));
        SumPorez  = N2(lista.Sum(s => s.Porez));
        SumDoppr  = N2(lista.Sum(s => s.Doppr));
        SumDopzr  = N2(lista.Sum(s => s.Dopzr));
        SumDopnr  = N2(lista.Sum(s => s.Dopnr));

        InitializeComponent();
        DataContext = this;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F10) { Stampaj(); e.Handled = true; }
    }

    private void Stampaj_Click(object sender, RoutedEventArgs e) => Stampaj();
    private void Zatvori_Click(object sender, RoutedEventArgs e) => Close();

    private void Stampaj()
    {
        var dlg = new System.Windows.Controls.PrintDialog();
        if (dlg.ShowDialog() == true)
            dlg.PrintVisual(PrintRoot, "Evidencija svih poreskih prijava");
    }

    public sealed class Ld011Red
    {
        public string Brobrac { get; init; } = "";
        public string Mesec   { get; init; } = "";
        public string Nazmes  { get; init; } = "";
        public string Vrsta   { get; init; } = "";
        public string Bruto   { get; init; } = "";
        public string Porez   { get; init; } = "";
        public string Doppr   { get; init; } = "";
        public string Dopzr   { get; init; } = "";
        public string Dopnr   { get; init; } = "";
        public string Dat1    { get; init; } = "";
        public string Ppopj1  { get; init; } = "";
        public string Ppod01  { get; init; } = "";
        public string Ppod01v { get; init; } = "";
        public string Ppod11  { get; init; } = "";
        public string Godina  { get; init; } = "";
    }
}
