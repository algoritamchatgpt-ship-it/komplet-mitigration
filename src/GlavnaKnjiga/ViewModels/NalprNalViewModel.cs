using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using GlavnaKnjiga.Views;
using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalprNalViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly string _korisnik;
    private readonly string _firma;

    private enum SortRezim { ByBrnal, ByDatdok, ByDug, ByPot }
    private SortRezim _sortRezim = SortRezim.ByBrnal;

    public event Action? ZatvoriFormu;

    [ObservableProperty] private ObservableCollection<NalprNalRow> _redovi = new();
    [ObservableProperty] private NalprNalRow? _selektovaniRed;
    [ObservableProperty] private string _naslov = "PREGLED NALOGA";

    public NalprNalViewModel(string firmPath, string korisnik, string firma)
    {
        _firmPath = firmPath;
        _korisnik = korisnik;
        _firma    = firma;
        Naslov = $"PREGLED NALOGA   {korisnik} {firma}";
        UcitajNalprNal();
    }

    private void UcitajNalprNal()
    {
        var path = Path.Combine(_firmPath, "nalprnal.dbf");
        if (!File.Exists(path)) return;

        var rows = new List<NalprNalRow>();
        foreach (var rec in new SimpleDbfReader(path).Zapisi())
        {
            rows.Add(new NalprNalRow
            {
                Brnal    = rec.DajString("BRNAL").TrimEnd(),
                Datdok   = rec.DajDate("DATDOK"),
                Dug      = rec.DajDecimal("DUG"),
                Pot      = rec.DajDecimal("POT"),
                Opis     = rec.DajString("OPIS").TrimEnd(),
                Dok      = rec.DajString("DOK").TrimEnd(),
                Mp       = rec.DajString("MP").TrimEnd(),
                Mtr      = rec.DajDecimal("MTR"),
                Automnal = rec.DajString("AUTOMNAL").TrimEnd(),
                Vrnal    = rec.DajString("VRNAL").TrimEnd(),
                Naziv    = rec.DajString("NAZIV").TrimEnd(),
                Obl      = rec.DajString("OBL").TrimEnd(),
                Period   = rec.DajDecimal("PERIOD"),
                Naldok   = rec.DajString("NALDOK").TrimEnd(),
                Znakovi  = rec.DajDecimal("ZNAKOVI"),
                Pocsif   = rec.DajString("POCSIF").TrimEnd(),
                Nauto    = rec.DajString("NAUTO").TrimEnd(),
                Konto    = rec.DajString("KONTO").TrimEnd(),
                Saldo    = rec.DajDecimal("SALDO"),
                Datknji  = rec.DajDate("DATKNJI"),
                Oper     = rec.DajString("OPER").TrimEnd(),
                Preneto  = rec.DajString("PRENETO").TrimEnd(),
                Idbr     = rec.DajDecimal("IDBR"),
            });
        }
        Redovi = new ObservableCollection<NalprNalRow>(rows);
        SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
    }

    private void SnimiNalprNal()
    {
        var path = Path.Combine(_firmPath, "nalprnal.dbf");
        if (!File.Exists(path)) return;
        var schema = DbfTableWriter.LoadSchema(path);
        DbfTableWriter.WriteTable(path, schema, Redovi, (row, field) =>
            field.ToUpperInvariant() switch
            {
                "BRNAL"    => (object?)row.Brnal.PadRight(6),
                "DATDOK"   => row.Datdok,
                "DUG"      => row.Dug,
                "POT"      => row.Pot,
                "OPIS"     => row.Opis.PadRight(30),
                "DOK"      => row.Dok.PadRight(3),
                "MP"       => row.Mp.PadRight(2),
                "MTR"      => row.Mtr,
                "AUTOMNAL" => row.Automnal.PadRight(1),
                "VRNAL"    => row.Vrnal.PadRight(3),
                "NAZIV"    => row.Naziv.PadRight(30),
                "OBL"      => row.Obl.PadRight(1),
                "PERIOD"   => row.Period,
                "NALDOK"   => row.Naldok.PadRight(1),
                "ZNAKOVI"  => row.Znakovi,
                "POCSIF"   => row.Pocsif.PadRight(3),
                "NAUTO"    => row.Nauto.PadRight(1),
                "KONTO"    => row.Konto.PadRight(10),
                "SALDO"    => row.Saldo,
                "DATKNJI"  => row.Datknji,
                "OPER"     => row.Oper.PadRight(2),
                "PRENETO"  => row.Preneto.PadRight(1),
                "IDBR"     => row.Idbr,
                _          => null,
            });
    }

    /// <summary>PREUZIMANJE — aggregate nal.dbf by BRNAL, join from nalbroj + nalvrsta</summary>
    [RelayCommand]
    private void Preuzimanje()
    {
        var nalPath    = Path.Combine(_firmPath, "nal.dbf");
        var nbPath     = Path.Combine(_firmPath, "nalbroj.dbf");
        var nvsPath    = Path.Combine(_firmPath, "nalvrsta.dbf");
        var prNalPath  = Path.Combine(_firmPath, "nalprnal.dbf");

        if (!File.Exists(nalPath))
        {
            MessageBox.Show("nal.dbf ne postoji.", "PREUZIMANJE",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Aggregate TOTAL ON BRNAL
            var agg = new SimpleDbfReader(nalPath).Zapisi()
                .Select(Nalp2ViewModel.NalpRowFromRecord)
                .GroupBy(r => r.Brnal.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new NalprNalRow
                {
                    Brnal  = g.Key,
                    Datdok = g.Min(r => r.Datdok),
                    Dug    = g.Sum(r => r.Dug),
                    Pot    = g.Sum(r => r.Pot),
                    Opis   = g.First().Opis.TrimEnd(),
                    Dok    = g.First().Dok.TrimEnd(),
                    Mp     = g.First().Mp.ToString().TrimEnd(),
                    Mtr    = g.First().Mtr,
                    Saldo  = g.Sum(r => r.Dug) - g.Sum(r => r.Pot),
                })
                .OrderBy(r => r.Brnal, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Join VRNAL + DATKNJI from nalbroj
            if (File.Exists(nbPath))
            {
                var nalbroj = new Dictionary<string, (string Vrnal, DateTime? Datknji)>(StringComparer.OrdinalIgnoreCase);
                foreach (var rec in new SimpleDbfReader(nbPath).Zapisi())
                    nalbroj[rec.DajString("BRNAL").Trim()] =
                        (rec.DajString("VRNAL").Trim(), rec.DajDate("DATKNJI"));

                foreach (var r in agg)
                    if (nalbroj.TryGetValue(r.Brnal, out var nb))
                    {
                        r.Vrnal   = nb.Vrnal;
                        r.Datknji = nb.Datknji;
                    }
            }

            // Join NAZIV + OBL + PERIOD + NALDOK + POCSIF + NAUTO + KONTO from nalvrsta
            if (File.Exists(nvsPath))
            {
                var nalvrsta = new Dictionary<string, NalvrstaRow>(StringComparer.OrdinalIgnoreCase);
                foreach (var rec in new SimpleDbfReader(nvsPath).Zapisi())
                {
                    var row = new NalvrstaRow();
                    row.Vrnal  = rec.DajString("VRNAL").Trim();
                    row.Naziv  = rec.DajString("NAZIV").TrimEnd();
                    row.Obl    = rec.DajString("OBL").TrimEnd();
                    row.Period = (int)rec.DajDecimal("PERIOD");
                    row.Naldok = rec.DajString("NALDOK").TrimEnd();
                    row.Pocsif = rec.DajString("POCSIF").TrimEnd();
                    row.Nauto  = rec.DajString("NAUTO").TrimEnd();
                    row.Konto  = rec.DajString("KONTO").TrimEnd();
                    nalvrsta[row.Vrnal] = row;
                }

                foreach (var r in agg)
                    if (!string.IsNullOrEmpty(r.Vrnal) && nalvrsta.TryGetValue(r.Vrnal, out var nv))
                    {
                        r.Naziv  = nv.Naziv;
                        r.Obl    = nv.Obl;
                        r.Period = nv.Period;
                        r.Naldok = nv.Naldok;
                        r.Pocsif = nv.Pocsif;
                        r.Nauto  = nv.Nauto;
                        r.Konto  = nv.Konto;
                    }
            }

            if (File.Exists(prNalPath))
                SnimiAgg(prNalPath, agg);

            Redovi = new ObservableCollection<NalprNalRow>(agg);
            SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri preuzimanju:\n{ex.Message}", "PREUZIMANJE",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SnimiAgg(string path, IEnumerable<NalprNalRow> agg)
    {
        var schema = DbfTableWriter.LoadSchema(path);
        DbfTableWriter.WriteTable(path, schema, agg.ToList(), (row, field) =>
            field.ToUpperInvariant() switch
            {
                "BRNAL"    => (object?)row.Brnal.PadRight(6),
                "DATDOK"   => row.Datdok,
                "DUG"      => row.Dug,
                "POT"      => row.Pot,
                "OPIS"     => row.Opis.PadRight(30),
                "DOK"      => row.Dok.PadRight(3),
                "MP"       => row.Mp.PadRight(2),
                "MTR"      => row.Mtr,
                "AUTOMNAL" => row.Automnal.PadRight(1),
                "VRNAL"    => row.Vrnal.PadRight(3),
                "NAZIV"    => row.Naziv.PadRight(30),
                "OBL"      => row.Obl.PadRight(1),
                "PERIOD"   => row.Period,
                "NALDOK"   => row.Naldok.PadRight(1),
                "ZNAKOVI"  => row.Znakovi,
                "POCSIF"   => row.Pocsif.PadRight(3),
                "NAUTO"    => row.Nauto.PadRight(1),
                "KONTO"    => row.Konto.PadRight(10),
                "SALDO"    => row.Saldo,
                "DATKNJI"  => row.Datknji,
                "OPER"     => row.Oper.PadRight(2),
                "PRENETO"  => row.Preneto.PadRight(1),
                "IDBR"     => row.Idbr,
                _          => null,
            });
    }

    /// <summary>NALOG SINTETIKA F10 — show nal.dbf rows for selected BRNAL</summary>
    [RelayCommand]
    private void NalogSintetikaF10()
    {
        if (SelektovaniRed == null) return;
        var brnal   = SelektovaniRed.Brnal.Trim();
        var nalPath = Path.Combine(_firmPath, "nal.dbf");
        if (!File.Exists(nalPath)) return;

        var rows = new SimpleDbfReader(nalPath).Zapisi()
            .Select(Nalp2ViewModel.NalpRowFromRecord)
            .Where(r => r.Brnal.Trim().Equals(brnal, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.Konto)
            .ToList();

        var saldo = 0m;
        foreach (var r in rows) { saldo += r.Dug - r.Pot; r.Dpsaldo = saldo; }

        var vm  = new NalogPregledViewModel($"Sintetika naloga: {brnal} — {SelektovaniRed.Naziv}",
            rows.Select(NalogPregledViewModel.IzNalp));
        new NalogPregledWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void Sort()
    {
        _sortRezim = _sortRezim switch
        {
            SortRezim.ByBrnal  => SortRezim.ByDatdok,
            SortRezim.ByDatdok => SortRezim.ByDug,
            SortRezim.ByDug    => SortRezim.ByPot,
            _                  => SortRezim.ByBrnal,
        };

        var sortirani = _sortRezim switch
        {
            SortRezim.ByDatdok => Redovi.OrderBy(r => r.Datdok).ToList(),
            SortRezim.ByDug    => Redovi.OrderByDescending(r => r.Dug).ToList(),
            SortRezim.ByPot    => Redovi.OrderByDescending(r => r.Pot).ToList(),
            _                  => Redovi.OrderBy(r => r.Brnal, StringComparer.OrdinalIgnoreCase).ToList(),
        };

        Redovi = new ObservableCollection<NalprNalRow>(sortirani);
        SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
    }

    [RelayCommand]
    private void TraziF6()
    {
        var unos = Interaction.InputBox("Unesite broj naloga za traženje:", "TRAŽENJE", "").Trim();
        if (string.IsNullOrEmpty(unos)) return;
        var nadjen = Redovi.FirstOrDefault(r =>
            r.Brnal.StartsWith(unos, StringComparison.OrdinalIgnoreCase));
        if (nadjen != null)
            SelektovaniRed = nadjen;
        else
            MessageBox.Show($"Nalog '{unos}' nije pronađen.", "TRAŽENJE",
                MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand] private void IdiGore()
    {
        if (SelektovaniRed == null || Redovi.Count == 0) return;
        var idx = Redovi.IndexOf(SelektovaniRed);
        if (idx > 0) SelektovaniRed = Redovi[idx - 1];
    }

    [RelayCommand] private void IdiDole()
    {
        if (SelektovaniRed == null || Redovi.Count == 0) return;
        var idx = Redovi.IndexOf(SelektovaniRed);
        if (idx < Redovi.Count - 1) SelektovaniRed = Redovi[idx + 1];
    }

    [RelayCommand] private void IdiNaVrh() { if (Redovi.Count > 0) SelektovaniRed = Redovi[0]; }
    [RelayCommand] private void IdiNaDno()  { if (Redovi.Count > 0) SelektovaniRed = Redovi[^1]; }

    [RelayCommand]
    private void Izlaz()
    {
        SnimiNalprNal();
        ZatvoriFormu?.Invoke();
    }
}
