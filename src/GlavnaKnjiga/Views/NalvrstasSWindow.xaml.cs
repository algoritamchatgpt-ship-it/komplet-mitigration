using Algoritam.Core.Services.Dbf;
using GlavnaKnjiga.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.Views;

/// <summary>
/// NALVRSTAS.SCX — UNOS VRSTE NALOGA PO ŠABLONU.
/// MOBJ=V/M, MRED=redni broj (2 cifre).
/// UNESI: dodaje 3 zapisa u nalvrsta.dbf (Kalkulacije/Racuni/Nivelacije za VP ili MP).
/// VRNAL=STR(RECNO(),3,0) → u FoxPro je RECCOUNT+1 za svaki APPEND BLANK.
/// Mi koristimo sljedeći slobodni redni broj.
/// </summary>
public partial class NalvrstasSWindow : Window
{
    private readonly string   _dbfPath;
    private readonly ObservableCollection<NalvrstaRow> _redovi;

    public NalvrstasSWindow(string dbfPath, ObservableCollection<NalvrstaRow> redovi)
    {
        InitializeComponent();
        _dbfPath = dbfPath;
        _redovi  = redovi;
    }

    private void BtnUnesi_Click(object sender, RoutedEventArgs e)
    {
        var mobj = TxtObj.Text.Trim().ToUpper();
        var mred = TxtRed.Text.Trim();

        if (mobj != "V" && mobj != "M")
        {
            MessageBox.Show("Unesite V (Veleprodaja) ili M (Maloprodaja).", "NALVRSTAS",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(mred))
        {
            MessageBox.Show("Unesite redni broj objekta.", "NALVRSTAS",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Sljedeći VRNAL = sljedeći slobodni redni broj u listi
        int sledeciVrnal = _redovi.Count + 1;

        if (mobj == "V")
        {
            DodajRed(ref sledeciVrnal, $"KALKULACIJE VP {mred}", mobj + mred, "PN", "K", "*", 2, mobj + mred, "*");
            DodajRed(ref sledeciVrnal, $"RACUNI VP {mred}",      mobj + mred, "PN", "R", "*", 2, "F"  + mred, "*");
            DodajRed(ref sledeciVrnal, $"NIVELACIJE VP {mred}",  mobj + mred, "PN", "N", "*", 2, "N"  + mred, "*");
        }
        else // M
        {
            DodajRed(ref sledeciVrnal, $"KALKULACIJE MP {mred}", mobj + mred, "PN", "K", "*", 2, mobj + mred, "*");
            DodajRed(ref sledeciVrnal, $"RACUNI MP {mred}",      mobj + mred, "PN", "R", "*", 2, "R"  + mred, "*");
            DodajRed(ref sledeciVrnal, $"NIVELACIJE MP {mred}",  mobj + mred, "PN", "N", "*", 2, "L"  + mred, "*");
        }

        // Snimi
        if (File.Exists(_dbfPath))
        {
            try
            {
                var schema = DbfTableWriter.LoadSchema(_dbfPath);
                DbfTableWriter.WriteTable(_dbfPath, schema, _redovi.ToList(), (r, f) => f switch
                {
                    "VRNAL"   => (object)r.Vrnal,
                    "NAZIV"   => r.Naziv,
                    "DOK"     => r.Dok,
                    "MP"      => r.Mp,
                    "OBL"     => r.Obl,
                    "PERIOD"  => r.Period,
                    "NALDOK"  => r.Naldok,
                    "ZNAKOVI" => r.Znakovi,
                    "POCSIF"  => r.Pocsif,
                    "NAUTO"   => r.Nauto,
                    "KONTO"   => r.Konto,
                    "PRENETO" => r.Preneto,
                    "IDBR"    => r.Idbr,
                    _         => null
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri snimanju: {ex.Message}", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        Close();
    }

    private void DodajRed(ref int vrnalBroj, string naziv, string dok, string mp,
        string obl, string naldok, int znakovi, string pocsif, string nauto)
    {
        var r = new NalvrstaRow
        {
            Vrnal   = vrnalBroj.ToString().PadLeft(3),
            Naziv   = naziv,
            Dok     = dok,
            Mp      = mp,
            Obl     = obl,
            Naldok  = naldok,
            Znakovi = znakovi,
            Pocsif  = pocsif,
            Nauto   = nauto,
        };
        _redovi.Add(r);
        vrnalBroj++;
    }

    private void BtnIzlaz_Click(object sender, RoutedEventArgs e) => Close();
}
