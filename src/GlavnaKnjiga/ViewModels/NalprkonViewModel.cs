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

public partial class NalprkonViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly string _korisnik;
    private readonly string _firma;

    private enum SortRezim { ByKonto, BySaldo, ByNaziv }
    private SortRezim _sortRezim = SortRezim.ByKonto;

    public event Action? ZatvoriFormu;

    [ObservableProperty] private ObservableCollection<NalprkonRow> _redovi = new();
    [ObservableProperty] private NalprkonRow? _selektovaniRed;
    [ObservableProperty] private string _naslov = "PREGLED KONTA";

    public NalprkonViewModel(string firmPath, string korisnik, string firma)
    {
        _firmPath = firmPath;
        _korisnik = korisnik;
        _firma    = firma;
        Naslov = $"PREGLED KONTA   {korisnik} {firma}";
        UcitajNalprkon();
    }

    private void UcitajNalprkon()
    {
        var path = Path.Combine(_firmPath, "nalprkon.dbf");
        if (!File.Exists(path)) return;

        var rows = new List<NalprkonRow>();
        foreach (var rec in new SimpleDbfReader(path).Zapisi())
        {
            rows.Add(new NalprkonRow
            {
                Konto   = rec.DajString("KONTO").TrimEnd(),
                Dug     = rec.DajDecimal("DUG"),
                Pot     = rec.DajDecimal("POT"),
                Saldo   = rec.DajDecimal("SALDO"),
                Naziv   = rec.DajString("NAZIV").TrimEnd(),
                Preneto = rec.DajString("PRENETO").TrimEnd(),
                Idbr    = rec.DajDecimal("IDBR"),
            });
        }
        Redovi = new ObservableCollection<NalprkonRow>(rows);
        SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
    }

    private void SnimiNalprkon()
    {
        var path = Path.Combine(_firmPath, "nalprkon.dbf");
        if (!File.Exists(path)) return;
        var schema = DbfTableWriter.LoadSchema(path);
        DbfTableWriter.WriteTable(path, schema, Redovi, (row, field) =>
            field.ToUpperInvariant() switch
            {
                "KONTO"   => (object?)row.Konto.PadRight(10),
                "DUG"     => row.Dug,
                "POT"     => row.Pot,
                "SALDO"   => row.Saldo,
                "NAZIV"   => row.Naziv.PadRight(60),
                "PRENETO" => row.Preneto.PadRight(1),
                "IDBR"    => row.Idbr,
                _         => null,
            });
    }

    /// <summary>PREUZIMANJE — aggregate nal.dbf by KONTO, join NAZIV from konto.dbf, write to nalprkon.dbf</summary>
    [RelayCommand]
    private void Preuzimanje()
    {
        var nalPath     = Path.Combine(_firmPath, "nal.dbf");
        var kontoPath   = Path.Combine(_firmPath, "konto.dbf");
        var prkonPath   = Path.Combine(_firmPath, "nalprkon.dbf");

        if (!File.Exists(nalPath))
        {
            MessageBox.Show("nal.dbf ne postoji.", "PREUZIMANJE",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Aggregate nal.dbf TOTAL ON KONTO
            var agg = new SimpleDbfReader(nalPath).Zapisi()
                .Select(Nalp2ViewModel.NalpRowFromRecord)
                .GroupBy(r => r.Konto.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new NalprkonRow
                {
                    Konto  = g.Key,
                    Dug    = g.Sum(r => r.Dug),
                    Pot    = g.Sum(r => r.Pot),
                    Saldo  = g.Sum(r => r.Dug) - g.Sum(r => r.Pot),
                })
                .OrderBy(r => r.Konto, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Look up NAZIV from konto.dbf
            if (File.Exists(kontoPath))
            {
                var nazivi = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var rec in new SimpleDbfReader(kontoPath).Zapisi())
                    nazivi[rec.DajString("KONTO").Trim()] = rec.DajString("NAZIV").TrimEnd();

                foreach (var r in agg)
                    if (nazivi.TryGetValue(r.Konto, out var n))
                        r.Naziv = n;
            }

            // Write to nalprkon.dbf (if it exists as template)
            if (File.Exists(prkonPath))
            {
                var schema = DbfTableWriter.LoadSchema(prkonPath);
                DbfTableWriter.WriteTable(prkonPath, schema, agg, (row, field) =>
                    field.ToUpperInvariant() switch
                    {
                        "KONTO"   => (object?)row.Konto.PadRight(10),
                        "DUG"     => row.Dug,
                        "POT"     => row.Pot,
                        "SALDO"   => row.Saldo,
                        "NAZIV"   => row.Naziv.PadRight(60),
                        "PRENETO" => row.Preneto.PadRight(1),
                        "IDBR"    => row.Idbr,
                        _         => null,
                    });
            }

            Redovi = new ObservableCollection<NalprkonRow>(agg);
            SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri preuzimanju:\n{ex.Message}", "PREUZIMANJE",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>KARTICA F10 — show full ledger cartice for selected KONTO from nal.dbf</summary>
    [RelayCommand]
    private void KarticaF10()
    {
        if (SelektovaniRed == null) return;
        var konto   = SelektovaniRed.Konto.Trim();
        var nalPath = Path.Combine(_firmPath, "nal.dbf");
        if (!File.Exists(nalPath)) return;

        var rows = new SimpleDbfReader(nalPath).Zapisi()
            .Select(Nalp2ViewModel.NalpRowFromRecord)
            .Where(r => r.Konto.Trim().Equals(konto, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.Datdok)
            .ToList();

        // Compute running SALDO
        var saldo = 0m;
        foreach (var r in rows)
        {
            saldo += r.Dug - r.Pot;
            r.Dpsaldo = saldo;
        }

        var vm  = new NalogPregledViewModel($"Kartica: {konto} — {SelektovaniRed.Naziv}",
            rows.Select(NalogPregledViewModel.IzNalp));
        var win = new NalogPregledWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void Sort()
    {
        _sortRezim = _sortRezim switch
        {
            SortRezim.ByKonto  => SortRezim.BySaldo,
            SortRezim.BySaldo  => SortRezim.ByNaziv,
            _                  => SortRezim.ByKonto,
        };

        var sortirani = _sortRezim switch
        {
            SortRezim.BySaldo => Redovi.OrderBy(r => r.Saldo).ToList(),
            SortRezim.ByNaziv => Redovi.OrderBy(r => r.Naziv, StringComparer.OrdinalIgnoreCase).ToList(),
            _                 => Redovi.OrderBy(r => r.Konto, StringComparer.OrdinalIgnoreCase).ToList(),
        };

        Redovi = new ObservableCollection<NalprkonRow>(sortirani);
        SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
    }

    [RelayCommand]
    private void TraziF6()
    {
        var unos = Interaction.InputBox("Unesite konto za traženje:", "TRAŽENJE", "").Trim();
        if (string.IsNullOrEmpty(unos)) return;
        var nadjen = Redovi.FirstOrDefault(r =>
            r.Konto.StartsWith(unos, StringComparison.OrdinalIgnoreCase));
        if (nadjen != null)
            SelektovaniRed = nadjen;
        else
            MessageBox.Show($"Konto '{unos}' nije pronađen.", "TRAŽENJE",
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
        SnimiNalprkon();
        ZatvoriFormu?.Invoke();
    }
}
