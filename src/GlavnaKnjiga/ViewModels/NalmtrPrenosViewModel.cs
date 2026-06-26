using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

// NALMTRPRENOS — Prenos knjiženja iz nal.dbf u nalmtr.dbf za period
public partial class NalmtrPrenosViewModel : ObservableObject
{
    private readonly string _firmPath;

    [ObservableProperty] private DateTime? _dat0;
    [ObservableProperty] private DateTime? _dat1;

    public event Action? ZatvoriFormu;

    public NalmtrPrenosViewModel(string firmPath)
    {
        _firmPath = firmPath;
    }

    [RelayCommand]
    private void Prenos()
    {
        if (Dat0 == null || Dat1 == null)
        {
            MessageBox.Show("Unesite početni i zadnji datum.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var mdat0 = Dat0.Value.Date;
        var mdat1 = Dat1.Value.Date;

        var nalPath    = Path.Combine(_firmPath, "nal.dbf");
        var nalmtrPath = Path.Combine(_firmPath, "nalmtr.dbf");
        var kontoPath  = Path.Combine(_firmPath, "konto.dbf");

        if (!File.Exists(nalPath)) { MessageBox.Show("nal.dbf nije pronađen."); return; }

        try
        {
            // Load konto.dbf for NAZIV lookup
            var kontoNaziv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(kontoPath))
            {
                foreach (var rec in new SimpleDbfReader(kontoPath).Zapisi())
                    kontoNaziv[rec.DajString("KONTO").Trim()] = rec.DajString("NAZIV").Trim();
            }

            // Load existing nalmtr BRNALs
            var postojeciBrnali = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var postojeciRedovi = new List<NalmtrRow>();
            if (File.Exists(nalmtrPath))
            {
                foreach (var rec in new SimpleDbfReader(nalmtrPath).Zapisi())
                {
                    var row = CitajNalmtrRec(rec);
                    postojeciRedovi.Add(row);
                    postojeciBrnali.Add(row.Brnal.Trim());
                }
            }

            // Read nal.dbf filtered by KONTO class 5 or 6 and date range
            // Group by KONTO+BRNAL → sum DUG, POT
            var grupeDatdok = new Dictionary<string, DateTime?>(StringComparer.OrdinalIgnoreCase);
            var grupeOpisDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var grupeKontoNal = new Dictionary<string, (decimal Dug, decimal Pot)>(StringComparer.OrdinalIgnoreCase);
            var brnaloviBrznal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rec in new SimpleDbfReader(nalPath).Zapisi())
            {
                var konto  = rec.DajString("KONTO").Trim();
                if (konto.Length == 0) continue;
                var klasa = konto[0];
                if (klasa != '5' && klasa != '6') continue;

                var datdok = rec.DajDate("DATDOK");
                if (datdok == null) continue;
                if (datdok.Value.Date < mdat0 || datdok.Value.Date > mdat1) continue;

                var brnal = rec.DajString("BRNAL").Trim();
                var opis  = rec.DajString("OPIS").Trim();
                var dug   = rec.DajDecimal("DUG");
                var pot   = rec.DajDecimal("POT");

                brnaloviBrznal.Add(brnal);

                var kbKey = konto + "\x00" + brnal;
                if (!grupeKontoNal.TryGetValue(kbKey, out var ag))
                    ag = (0m, 0m);
                grupeKontoNal[kbKey] = (ag.Dug + dug, ag.Pot + pot);

                if (!grupeDatdok.ContainsKey(brnal))
                    grupeDatdok[brnal] = datdok;

                if (!grupeOpisDict.ContainsKey(kbKey))
                    grupeOpisDict[kbKey] = opis;
            }

            // For each new BRNAL not in nalmtr: add rows per KONTO
            int dodato = 0;
            foreach (var brnal in brnaloviBrznal)
            {
                if (postojeciBrnali.Contains(brnal)) continue;

                var datdok = grupeDatdok.TryGetValue(brnal, out var d) ? d : null;

                foreach (var kv in grupeKontoNal)
                {
                    var parts = kv.Key.Split('\x00');
                    if (parts.Length < 2 || parts[1] != brnal) continue;
                    var konto = parts[0];

                    kontoNaziv.TryGetValue(konto, out var naziv);
                    grupeOpisDict.TryGetValue(kv.Key, out var opis);

                    var saldo = kv.Value.Dug - kv.Value.Pot;
                    var row = new NalmtrRow
                    {
                        Konto   = konto.PadRight(10),
                        Datdok  = datdok,
                        Brnal   = brnal.PadRight(6),
                        Dug     = kv.Value.Dug,
                        Pot     = kv.Value.Pot,
                        Saldo   = saldo,
                        Naziv   = (naziv ?? string.Empty).PadRight(60),
                        Opis    = (opis ?? string.Empty),
                        Razlika = saldo,
                    };
                    postojeciRedovi.Add(row);
                    dodato++;
                }
            }

            // Sort by DATDOK then KONTO (like SORT ON DATDOK,KONTO in FoxPro)
            var sortirani = postojeciRedovi
                .OrderBy(r => r.Datdok ?? DateTime.MinValue)
                .ThenBy(r => r.Konto)
                .ToList();

            // Recalculate SALDO, UKUPNO, RAZLIKA for each row
            foreach (var row in sortirani)
            {
                row.Saldo = row.Dug - row.Pot;
                row.RecalcUkupno();
            }

            // Write back nalmtr.dbf (must exist as template)
            if (!File.Exists(nalmtrPath))
            {
                var tmpl = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NALTEMPLATE", "nalmtr.dbf");
                if (!File.Exists(tmpl)) { MessageBox.Show("nalmtr.dbf template nije pronađen."); return; }
                File.Copy(tmpl, nalmtrPath);
            }

            var schema = DbfTableWriter.LoadSchema(nalmtrPath);
            DbfTableWriter.WriteTable(nalmtrPath, schema, sortirani, NalmtrRowFieldMapper);

            MessageBox.Show($"Prenos završen. Dodato {dodato} novih redova.", "Prenos", MessageBoxButton.OK, MessageBoxImage.Information);
            ZatvoriFormu?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri prenosu: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    internal static NalmtrRow CitajNalmtrRec(DbfRecord rec) => new()
    {
        Konto   = rec.DajString("KONTO"),
        Datdok  = rec.DajDate("DATDOK"),
        Brnal   = rec.DajString("BRNAL"),
        Dug     = rec.DajDecimal("DUG"),
        Pot     = rec.DajDecimal("POT"),
        Saldo   = rec.DajDecimal("SALDO"),
        Iznos01 = rec.DajDecimal("IZNOS01"),
        Iznos02 = rec.DajDecimal("IZNOS02"),
        Iznos03 = rec.DajDecimal("IZNOS03"),
        Iznos04 = rec.DajDecimal("IZNOS04"),
        Iznos05 = rec.DajDecimal("IZNOS05"),
        Iznos06 = rec.DajDecimal("IZNOS06"),
        Iznos07 = rec.DajDecimal("IZNOS07"),
        Iznos08 = rec.DajDecimal("IZNOS08"),
        Iznos09 = rec.DajDecimal("IZNOS09"),
        Iznos10 = rec.DajDecimal("IZNOS10"),
        Iznos11 = rec.DajDecimal("IZNOS11"),
        Iznos12 = rec.DajDecimal("IZNOS12"),
        Iznos13 = rec.DajDecimal("IZNOS13"),
        Iznos14 = rec.DajDecimal("IZNOS14"),
        Iznos15 = rec.DajDecimal("IZNOS15"),
        Iznos16 = rec.DajDecimal("IZNOS16"),
        Iznos17 = rec.DajDecimal("IZNOS17"),
        Iznos18 = rec.DajDecimal("IZNOS18"),
        Iznos19 = rec.DajDecimal("IZNOS19"),
        Iznos20 = rec.DajDecimal("IZNOS20"),
        Iznos21 = rec.DajDecimal("IZNOS21"),
        Iznos22 = rec.DajDecimal("IZNOS22"),
        Iznos23 = rec.DajDecimal("IZNOS23"),
        Iznos24 = rec.DajDecimal("IZNOS24"),
        Iznos25 = rec.DajDecimal("IZNOS25"),
        Iznos26 = rec.DajDecimal("IZNOS26"),
        Iznos27 = rec.DajDecimal("IZNOS27"),
        Iznos28 = rec.DajDecimal("IZNOS28"),
        Iznos29 = rec.DajDecimal("IZNOS29"),
        Iznos30 = rec.DajDecimal("IZNOS30"),
        Naziv   = rec.DajString("NAZIV"),
        Opis    = rec.DajString("OPIS"),
        Ukupno  = rec.DajDecimal("UKUPNO"),
        Razlika = rec.DajDecimal("RAZLIKA"),
        Arhiva  = rec.DajString("ARHIVA"),
    };

    internal static object? NalmtrRowFieldMapper(NalmtrRow r, string field) => field switch
    {
        "KONTO"   => r.Konto,
        "DATDOK"  => r.Datdok,
        "BRNAL"   => r.Brnal,
        "DUG"     => r.Dug,
        "POT"     => r.Pot,
        "SALDO"   => r.Saldo,
        "IZNOS01" => r.Iznos01,
        "IZNOS02" => r.Iznos02,
        "IZNOS03" => r.Iznos03,
        "IZNOS04" => r.Iznos04,
        "IZNOS05" => r.Iznos05,
        "IZNOS06" => r.Iznos06,
        "IZNOS07" => r.Iznos07,
        "IZNOS08" => r.Iznos08,
        "IZNOS09" => r.Iznos09,
        "IZNOS10" => r.Iznos10,
        "IZNOS11" => r.Iznos11,
        "IZNOS12" => r.Iznos12,
        "IZNOS13" => r.Iznos13,
        "IZNOS14" => r.Iznos14,
        "IZNOS15" => r.Iznos15,
        "IZNOS16" => r.Iznos16,
        "IZNOS17" => r.Iznos17,
        "IZNOS18" => r.Iznos18,
        "IZNOS19" => r.Iznos19,
        "IZNOS20" => r.Iznos20,
        "IZNOS21" => r.Iznos21,
        "IZNOS22" => r.Iznos22,
        "IZNOS23" => r.Iznos23,
        "IZNOS24" => r.Iznos24,
        "IZNOS25" => r.Iznos25,
        "IZNOS26" => r.Iznos26,
        "IZNOS27" => r.Iznos27,
        "IZNOS28" => r.Iznos28,
        "IZNOS29" => r.Iznos29,
        "IZNOS30" => r.Iznos30,
        "NAZIV"   => r.Naziv,
        "OPIS"    => r.Opis,
        "UKUPNO"  => r.Ukupno,
        "RAZLIKA" => r.Razlika,
        "ARHIVA"  => r.Arhiva,
        _         => null
    };
}
