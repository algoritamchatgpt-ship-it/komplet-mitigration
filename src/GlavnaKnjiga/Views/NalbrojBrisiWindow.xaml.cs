using Algoritam.Core.Services.Dbf;
using GlavnaKnjiga.Models;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.Views;

/// <summary>
/// NALBROJBRISI.SCX — brisanje nalbroj zapisa koji ne postoje u nal.dbf.
/// Ako user upiše 'D' i klikne POTVRDA:
///   1. Čita nal.dbf → skup BRNAL (DUG≠0 OR POT≠0, BRNAL≠'')
///   2. Čita nalbroj.dbf → filtrira samo zapise čiji BRNAL je u skupu
///   3. Snima filtrirane nazad u nalbroj.dbf
/// </summary>
public partial class NalbrojBrisiWindow : Window
{
    private readonly string _firmPath;

    public NalbrojBrisiWindow(string firmPath)
    {
        InitializeComponent();
        _firmPath = firmPath;
    }

    private void BtnPotvrda_Click(object sender, RoutedEventArgs e)
    {
        if (TxtDane.Text.Trim().ToUpper() != "D") { Close(); return; }

        var nalPath    = Path.Combine(_firmPath, "nal.dbf");
        var nalbrojPath = Path.Combine(_firmPath, "nalbroj.dbf");

        if (!File.Exists(nalPath) || !File.Exists(nalbrojPath))
        {
            MessageBox.Show("nal.dbf ili nalbroj.dbf nije pronađen.", "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
            return;
        }

        try
        {
            // 1. Skup BRNAL iz nal.dbf (DUG<>0 OR POT<>0, BRNAL<>'')
            var validBrnal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var nalReader  = new SimpleDbfReader(nalPath);
            foreach (var rec in nalReader.Zapisi())
            {
                var dug   = rec.DajDecimal("DUG");
                var pot   = rec.DajDecimal("POT");
                var brnal = rec.DajString("BRNAL").Trim();
                if ((dug != 0 || pot != 0) && !string.IsNullOrEmpty(brnal))
                    validBrnal.Add(brnal);
            }

            // 2. Čita nalbroj i filtrira
            var nbReader = new SimpleDbfReader(nalbrojPath);
            var zadrzani = nbReader.Zapisi()
                .Where(rec =>
                {
                    var brnal = rec.DajString("BRNAL").Trim();
                    return !string.IsNullOrEmpty(brnal) && validBrnal.Contains(brnal);
                })
                .Select(rec => new NalbrojRow
                {
                    Brnal   = rec.DajString("BRNAL"),
                    Datum   = rec.DajDate("DATUM"),
                    Vrnal   = rec.DajString("VRNAL"),
                    Opis    = rec.DajString("OPIS"),
                    Datod   = rec.DajDate("DATOD"),
                    Datdo   = rec.DajDate("DATDO"),
                    Dug     = rec.DajDecimal("DUG"),
                    Pot     = rec.DajDecimal("POT"),
                    Datknji = rec.DajDate("DATKNJI"),
                    Oper    = rec.DajString("OPER"),
                    Preneto = rec.DajString("PRENETO"),
                    Idbr    = rec.DajDecimal("IDBR"),
                })
                .ToList();

            // 3. Snima filtrirane nazad
            var schema = DbfTableWriter.LoadSchema(nalbrojPath);
            DbfTableWriter.WriteTable(nalbrojPath, schema, zadrzani, (r, f) => f switch
            {
                "BRNAL"   => (object)r.Brnal,
                "DATUM"   => r.Datum,
                "VRNAL"   => r.Vrnal,
                "OPIS"    => r.Opis,
                "DATOD"   => r.Datod,
                "DATDO"   => r.Datdo,
                "DUG"     => r.Dug,
                "POT"     => r.Pot,
                "DATKNJI" => r.Datknji,
                "OPER"    => r.Oper,
                "PRENETO" => r.Preneto,
                "IDBR"    => r.Idbr,
                _         => null
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri brisanju: {ex.Message}", "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        Close();
    }

    private void BtnIzlaz_Click(object sender, RoutedEventArgs e) => Close();
}
