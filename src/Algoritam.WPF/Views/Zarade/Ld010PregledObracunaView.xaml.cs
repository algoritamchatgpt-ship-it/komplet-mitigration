using Algoritam.WPF.ViewModels;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace Algoritam.WPF.Views.Zarade;

public partial class Ld010PregledObracunaView : Window
{
    public string FirmaNaziv  { get; }
    public string FirmaMesto  { get; }

    public IReadOnlyList<Ld010Red> Redovi { get; }

    public string SumCasuk   { get; }
    public string SumCasbol  { get; }
    public string SumBruto   { get; }
    public string SumPorez   { get; }
    public string SumDopsocr { get; }
    public string SumDoppr   { get; }
    public string SumDopzr   { get; }
    public string SumDopnr   { get; }
    public string SumNeto    { get; }

    public Ld010PregledObracunaView(
        IEnumerable<Ld0Stavka> stavke,
        string firmaNaziv,
        string firmaMesto)
    {
        var lista = stavke?.ToList() ?? [];
        var ci    = CultureInfo.CurrentCulture;
        string N2(decimal v) => v.ToString("N2", ci);
        string N6(decimal v) => v != 0m ? v.ToString("N6", ci) : "";

        FirmaNaziv = firmaNaziv;
        FirmaMesto = firmaMesto;

        Redovi = lista.Select(s => new Ld010Red
        {
            Brobrac  = s.Brobrac.ToString(),
            Mesec    = s.Mesec.ToString(),
            Nazmes   = s.Nazmes,
            Vrsta    = s.Vrsta,
            Casuk    = N2(s.Casuk),
            Casbol   = N2(s.Casbol),
            Bruto    = N2(s.Bruto),
            Porez    = N2(s.Porez),
            Dopsocr  = N2(s.Dopsocr),
            Doppr    = N2(s.Doppr),
            Dopzr    = N2(s.Dopzr),
            Dopnr    = N2(s.Dopnr),
            Neto     = N2(s.Neto),
            Cenarada = N6(s.Cenarada),
            Dat1     = s.Dat1?.ToString("dd.MM.yyyy") ?? "",
        }).ToList();

        SumCasuk   = N2(lista.Sum(s => s.Casuk));
        SumCasbol  = N2(lista.Sum(s => s.Casbol));
        SumBruto   = N2(lista.Sum(s => s.Bruto));
        SumPorez   = N2(lista.Sum(s => s.Porez));
        SumDopsocr = N2(lista.Sum(s => s.Dopsocr));
        SumDoppr   = N2(lista.Sum(s => s.Doppr));
        SumDopzr   = N2(lista.Sum(s => s.Dopzr));
        SumDopnr   = N2(lista.Sum(s => s.Dopnr));
        SumNeto    = N2(lista.Sum(s => s.Neto));

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
            dlg.PrintVisual(PrintRoot, "Evidencija svih obračunatih zarada");
    }

    public sealed class Ld010Red
    {
        public string Brobrac  { get; init; } = "";
        public string Mesec    { get; init; } = "";
        public string Nazmes   { get; init; } = "";
        public string Vrsta    { get; init; } = "";
        public string Casuk    { get; init; } = "";
        public string Casbol   { get; init; } = "";
        public string Bruto    { get; init; } = "";
        public string Porez    { get; init; } = "";
        public string Dopsocr  { get; init; } = "";
        public string Doppr    { get; init; } = "";
        public string Dopzr    { get; init; } = "";
        public string Dopnr    { get; init; } = "";
        public string Neto     { get; init; } = "";
        public string Cenarada { get; init; } = "";
        public string Dat1     { get; init; } = "";
    }
}
