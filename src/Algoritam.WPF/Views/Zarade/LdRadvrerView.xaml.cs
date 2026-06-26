using Algoritam.Infrastructure.Dbf;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Algoritam.WPF.Views.Zarade;

public partial class LdRadvrerView : Window
{
    private List<LdRadStavka> _svi = [];

    public LdRadvrerView(string folderPath)
    {
        InitializeComponent();
        UcitajLdRad(folderPath);
    }

    // PROCEDURE Load: SET ORDER TO 2 (po imenu prezimenu)
    private void UcitajLdRad(string folderPath)
    {
        var dbfPath = Path.Combine(folderPath, "ldrad.dbf");
        if (!File.Exists(dbfPath))
        {
            TxtStatus.Text = "ldrad.dbf nije pronađen.";
            return;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);

            _svi = zapisi
                .Select(z => new LdRadStavka
                {
                    Broj      = DecToInt(z, "BROJ"),
                    ImePrez   = Str(z, "IME_PREZ"),
                    VrstaId   = Str(z, "VRSTAID"),
                    Prebival  = Str(z, "PREBIVAL"),
                    MaticniBr = Str(z, "MATICNIBR"),
                    Katalog   = Str(z, "KATALOG"),
                    VrstaPrim = Str(z, "VRSTAPRIM"),
                    OznVrPrih = Str(z, "OZNVRPRIH"),
                    OznOlaks  = Str(z, "OZNOLAKS"),
                    OznBen    = Str(z, "OZNBEN"),
                    StartBod  = DecToDecimal(z, "STARTBOD"),
                    Staz      = DecToInt(z, "STAZ"),
                    Vrsta     = Str(z, "VRSTA"),
                    Grupa     = DecToInt(z, "GRUPA"),
                    Grupa1    = DecToInt(z, "GRUPA1"),
                    Stepen    = Str(z, "STEPEN"),
                    BenProc   = DecToDecimal(z, "BENPROC"),
                    Mfp3Proc  = DecToDecimal(z, "MFP3PROC"),
                    Mfp6      = DecToDecimal(z, "MFP6"),
                    Mfp7      = DecToDecimal(z, "MFP7"),
                    Prevoz    = Str(z, "PREVOZ"),
                    Brisanje  = Str(z, "BRISANJE"),
                })
                // ORDER 2 — sortirano po imenu i prezimenu
                .OrderBy(s => s.ImePrez)
                .ToList();

            GrdLdrad.ItemsSource = _svi;
            TxtStatus.Text = $"Učitano {_svi.Count} radnika.";

            // GotFocus → GRDLDRAD.SETFOCUS
            GrdLdrad.Focus();
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Greška: {ex.Message}";
        }
    }

    // Pretraga po imenu (TXTIME_PREZ)
    private void PretragaChanged(object sender, TextChangedEventArgs e)
    {
        var term = TxtPretraga.Text.ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(term))
        {
            GrdLdrad.ItemsSource = _svi;
        }
        else
        {
            GrdLdrad.ItemsSource = _svi
                .Where(s => s.ImePrez.ToUpperInvariant().Contains(term) ||
                            s.Broj.ToString().Contains(term))
                .ToList();
        }
    }

    // PROCEDURE Unload: SET ORDER TO 1 (vraća na originalni redosled)
    // IZLAZ → Release THISFORM
    private void IzlazClick(object sender, RoutedEventArgs e) => Close();

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) ? Convert.ToString(v)?.Trim() ?? "" : "";

    private static int DecToInt(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v)) return 0;
        return v is decimal d ? (int)d : 0;
    }

    private static decimal DecToDecimal(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v)) return 0m;
        return v is decimal d ? d : 0m;
    }
}

public class LdRadStavka
{
    public int     Broj      { get; set; }
    public string  ImePrez   { get; set; } = "";
    public string  VrstaId   { get; set; } = "";
    public string  Prebival  { get; set; } = "";
    public string  MaticniBr { get; set; } = "";
    public string  Katalog   { get; set; } = "";
    public string  VrstaPrim { get; set; } = "";
    public string  OznVrPrih { get; set; } = "";
    public string  OznOlaks  { get; set; } = "";
    public string  OznBen    { get; set; } = "";
    public decimal StartBod  { get; set; }
    public int     Staz      { get; set; }
    public string  Vrsta     { get; set; } = "";
    public int     Grupa     { get; set; }
    public int     Grupa1    { get; set; }
    public string  Stepen    { get; set; } = "";
    public decimal BenProc   { get; set; }
    public decimal Mfp3Proc  { get; set; }
    public decimal Mfp6      { get; set; }
    public decimal Mfp7      { get; set; }
    public string  Prevoz    { get; set; } = "";
    public string  Brisanje  { get; set; } = "";
}
