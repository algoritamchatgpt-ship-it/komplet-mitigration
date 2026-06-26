using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NaluniorViewModel : ObservableObject
{
    private readonly string _firmPath;

    public event Action? ZatvoriFormu;

    [ObservableProperty] private ObservableCollection<UniorRow> _redovi = new();
    [ObservableProperty] private UniorRow? _selektovaniRed;
    [ObservableProperty] private string _lblRec = string.Empty;

    public NaluniorViewModel(string firmPath)
    {
        _firmPath = firmPath;
        Ucitaj();
    }

    private string TablePath => Path.Combine(_firmPath, "unior.dbf");

    private void Ucitaj()
    {
        if (!File.Exists(TablePath)) return;
        var rows = new List<UniorRow>();
        foreach (var rec in new SimpleDbfReader(TablePath).Zapisi())
        {
            rows.Add(MapUniorRowFromRecord(rec));
        }
        Redovi = new ObservableCollection<UniorRow>(rows);
        SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
        AzurirajLblRec();
    }

    internal static UniorRow MapUniorRowFromRecord(DbfRecord rec) => new()
    {
        Vpdv      = rec.DajString("VPDV").TrimEnd(),
        Kontoa    = rec.DajString("KONTOA").TrimEnd(),
        Konto     = rec.DajString("KONTO").TrimEnd(),
        Brnal     = rec.DajString("BRNAL").TrimEnd(),
        Datslanja = rec.DajDate("DATSLANJA"),
        Datpdv    = rec.DajDate("DATPDV"),
        Datdok    = rec.DajDate("DATDOK"),
        Brrac     = rec.DajString("BRRAC").TrimEnd(),
        Valuta    = rec.DajString("VALUTA").TrimEnd(),
        Sifra     = rec.DajString("SIFRA").TrimEnd(),
        Ukprod    = rec.DajDecimal("UKPROD"),
        Osn18     = rec.DajDecimal("OSN18"),
        Pdv18     = rec.DajDecimal("PDV18"),
        Osn8      = rec.DajDecimal("OSN8"),
        Pdv8      = rec.DajDecimal("PDV8"),
        Ukupno    = rec.DajDecimal("UKUPNO"),
        Pdv       = rec.DajDecimal("PDV"),
        Osn0      = rec.DajDecimal("OSN0"),
        Dok       = rec.DajString("DOK").TrimEnd(),
        Dev       = rec.DajString("DEV").TrimEnd(),
        Devkurs   = rec.DajDecimal("DEVKURS"),
        Devdug    = rec.DajDecimal("DEVDUG"),
        Devpot    = rec.DajDecimal("DEVPOT"),
        Kurs      = rec.DajDecimal("KURS"),
        Arhiva    = rec.DajString("ARHIVA").TrimEnd(),
        Naziv     = rec.DajString("NAZIV").TrimEnd(),
        Pib       = rec.DajString("PIB").TrimEnd(),
        Pogon     = rec.DajString("POGON").TrimEnd(),
        Povezanol = rec.DajString("POVEZANOL").TrimEnd(),
        Vrstaf    = rec.DajString("VRSTAF").TrimEnd(),
        Preneto   = rec.DajString("PRENETO").TrimEnd(),
        Numred    = rec.DajDecimal("NUMRED"),
        Idbr      = rec.DajDecimal("IDBR"),
    };

    internal static object? UniorFieldMapper(UniorRow row, string field) =>
        field.ToUpperInvariant() switch
        {
            "VPDV"      => (object?)row.Vpdv,
            "KONTOA"    => row.Kontoa,
            "KONTO"     => row.Konto,
            "BRNAL"     => row.Brnal,
            "DATSLANJA" => row.Datslanja,
            "DATPDV"    => row.Datpdv,
            "DATDOK"    => row.Datdok,
            "BRRAC"     => row.Brrac,
            "VALUTA"    => row.Valuta,
            "SIFRA"     => row.Sifra,
            "UKPROD"    => row.Ukprod,
            "OSN18"     => row.Osn18,
            "PDV18"     => row.Pdv18,
            "OSN8"      => row.Osn8,
            "PDV8"      => row.Pdv8,
            "UKUPNO"    => row.Ukupno,
            "PDV"       => row.Pdv,
            "OSN0"      => row.Osn0,
            "DOK"       => row.Dok,
            "DEV"       => row.Dev,
            "DEVKURS"   => row.Devkurs,
            "DEVDUG"    => row.Devdug,
            "DEVPOT"    => row.Devpot,
            "KURS"      => row.Kurs,
            "ARHIVA"    => row.Arhiva,
            "NAZIV"     => row.Naziv,
            "PIB"       => row.Pib,
            "POGON"     => row.Pogon,
            "POVEZANOL" => row.Povezanol,
            "VRSTAF"    => row.Vrstaf,
            "PRENETO"   => row.Preneto,
            "NUMRED"    => row.Numred,
            "IDBR"      => row.Idbr,
            _           => null,
        };

    private void Snimi()
    {
        if (!File.Exists(TablePath)) return;
        var schema = DbfTableWriter.LoadSchema(TablePath);
        DbfTableWriter.WriteTable(TablePath, schema, Redovi, UniorFieldMapper);
    }

    private void AzurirajLblRec()
    {
        LblRec = SelektovaniRed != null
            ? $"Rec: {Redovi.IndexOf(SelektovaniRed) + 1}/{Redovi.Count}"
            : $"0/{Redovi.Count}";
    }

    partial void OnSelektovaniRedChanged(UniorRow? value) => AzurirajLblRec();

    [RelayCommand]
    private void Dodaj()
    {
        var novi = new UniorRow();
        Redovi.Add(novi);
        SelektovaniRed = novi;
    }

    /// <summary>Sifra LostFocus — lookup AN0 to set Naziv + Pib</summary>
    [RelayCommand]
    private void SifraLostFocus()
    {
        if (SelektovaniRed == null || string.IsNullOrWhiteSpace(SelektovaniRed.Sifra)) return;
        var mSifra = SelektovaniRed.Sifra.TrimEnd().PadLeft(5, '0');
        SelektovaniRed.Sifra = mSifra;

        var an0Path = Path.Combine(_firmPath, "an0.dbf");
        if (!File.Exists(an0Path)) return;

        foreach (var rec in new SimpleDbfReader(an0Path).Zapisi())
        {
            if (rec.DajString("SIFRA").Trim() == mSifra.Trim())
            {
                SelektovaniRed.Naziv = rec.DajString("NAZIV").TrimEnd();
                SelektovaniRed.Pib   = rec.DajString("PIB").TrimEnd();
                break;
            }
        }
    }

    /// <summary>Knjiženje — posts selected BRNAL's unarchived rows to pdvi.dbf + nalp.dbf</summary>
    [RelayCommand]
    private void Knjizenje()
    {
        if (SelektovaniRed == null) return;
        var mBrnal = SelektovaniRed.Brnal.Trim();

        Snimi();

        var devPath   = Path.Combine(_firmPath, "dev.dbf");
        var pdviPath  = Path.Combine(_firmPath, "pdvi.dbf");
        var nalpPath  = Path.Combine(_firmPath, "nalp.dbf");
        var aaanPath  = Path.Combine(_firmPath, "aaan.dbf");

        var devRates = new Dictionary<(string dev, DateTime date), decimal>();
        if (File.Exists(devPath))
        {
            foreach (var rec in new SimpleDbfReader(devPath).Zapisi())
            {
                var d = rec.DajDate("DATDOK");
                if (d.HasValue)
                    devRates[(rec.DajString("DEV").Trim(), d.Value.Date)] = rec.DajDecimal("KURS");
            }
        }

        // Load AAAN mappings: kontoa → sifprod
        var aaanMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(aaanPath))
        {
            foreach (var rec in new SimpleDbfReader(aaanPath).Zapisi())
                aaanMap[rec.DajString("KONTO").Trim()] = rec.DajString("SIFPROD").Trim();
        }

        // Load existing NALP BRNALs to detect duplicates
        var nalpBrnals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(nalpPath))
        {
            foreach (var rec in new SimpleDbfReader(nalpPath).Zapisi())
                nalpBrnals.Add(rec.DajString("BRNAL").Trim());
        }

        var rowsToBook = Redovi
            .Where(r => r.Brnal.Trim() == mBrnal
                     && r.Arhiva.Trim() != "*"
                     && r.Konto.Trim().Length == 10)
            .ToList();

        if (rowsToBook.Count == 0)
        {
            MessageBox.Show("Nema stavki za knjiženje (proveri da li je konto = 10 cifara i arhiva = ' ').",
                "KNJIŽENJE", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            foreach (var row in rowsToBook)
            {
                var mDev    = row.Dev.Trim();
                var mDatpdv = row.Datpdv;
                var mKurs   = row.Kurs;

                // Exchange rate lookup
                decimal mDevkurs = 0;
                if (mDatpdv.HasValue && devRates.TryGetValue((mDev, mDatpdv.Value.Date), out var rate))
                    mDevkurs = rate;

                row.Devkurs = mDevkurs;
                if (row.Devdug != 0)
                {
                    row.Osn18  = mDevkurs * row.Devdug;
                    row.Ukprod = mDevkurs * row.Devdug;
                }

                decimal mDug = row.Osn18 + row.Pdv18 + row.Osn0;

                // Append to pdvi.dbf
                if (File.Exists(pdviPath))
                {
                    var pdviSchema = DbfTableWriter.LoadSchema(pdviPath);
                    var pdviRows = new SimpleDbfReader(pdviPath).Zapisi()
                        .Select(MapPdvi).ToList();
                    pdviRows.Add(MapUniorToPdvi(row, mDatpdv));
                    DbfTableWriter.WriteTable(pdviPath, pdviSchema, pdviRows, (r, f) =>
                        f.ToUpperInvariant() switch
                        {
                            "VPDV"      => (object?)r.Vpdv,
                            "KONTOA"    => r.Kontoa,
                            "KONTO"     => r.Konto,
                            "BRNAL"     => r.Brnal,
                            "DATSLANJA" => r.Datslanja,
                            "DATPDV"    => r.Datpdv,
                            "DATDOK"    => r.Datdok,
                            "BRRAC"     => r.Brrac,
                            "VALUTA"    => r.Valuta,
                            "SIFRA"     => r.Sifra,
                            "UKPROD"    => r.Ukprod,
                            "OSN18"     => r.Osn18,
                            "PDV18"     => r.Pdv18,
                            "OSN8"      => r.Osn8,
                            "PDV8"      => r.Pdv8,
                            "UKUPNO"    => r.Ukupno,
                            "PDV"       => r.Pdv,
                            "OSN0"      => r.Osn0,
                            "DOK"       => r.Dok,
                            "DEV"       => r.Dev,
                            "DEVKURS"   => r.Devkurs,
                            "DEVDUG"    => r.Devdug,
                            "DEVPOT"    => r.Devpot,
                            "KURS"      => r.Kurs,
                            "ARHIVA"    => r.Arhiva,
                            "NAZIV"     => r.Naziv,
                            "PIB"       => r.Pib,
                            "POGON"     => r.Pogon,
                            "POVEZANOL" => r.Povezanol,
                            "VRSTAF"    => r.Vrstaf,
                            "PRENETO"   => r.Preneto,
                            "NUMRED"    => r.Numred,
                            "IDBR"      => r.Idbr,
                            "DATPRI"    => r.Datpdv,
                            "OPS"       => mDev != "" ? "O" : "",
                            "UKUPNOP"   => row.Osn18 + row.Pdv18 + row.Osn0,
                            _           => null,
                        });
                }

                // Write to nalp.dbf header row (first row for this BRNAL only)
                if (File.Exists(nalpPath) && !nalpBrnals.Contains(mBrnal))
                {
                    nalpBrnals.Add(mBrnal);
                    var nalpSchema = DbfTableWriter.LoadSchema(nalpPath);
                    var nalpRows = new SimpleDbfReader(nalpPath).Zapisi()
                        .Select(Nalp2ViewModel.NalpRowFromRecord).ToList();

                    var newNalpRow = new NalpRow
                    {
                        Datdok = row.Datdok ?? DateTime.Today,
                        Brnal  = mBrnal.PadRight(6),
                        Opis   = "IZLAZNI RACUNI",
                        Kurs   = mKurs,
                    };
                    if (mDug != 0)
                        newNalpRow.Kursdug = mKurs * mDug;
                    else
                        newNalpRow.Kurspot = mKurs * row.Pdv;

                    nalpRows.Add(newNalpRow);
                    DbfTableWriter.WriteTable(nalpPath, nalpSchema, nalpRows, Nalp2ViewModel.NalpRowFieldMapper);
                }

                row.Arhiva = "*";
            }

            Snimi();
            MessageBox.Show($"Knjiženje završeno. Proknjiženo stavki: {rowsToBook.Count}.",
                "KNJIŽENJE", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju:\n{ex.Message}", "KNJIŽENJE",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static UniorRow MapPdvi(DbfRecord rec) => MapUniorRowFromRecord(rec);
    private static UniorRow MapUniorToPdvi(UniorRow row, DateTime? datpdv) => new()
    {
        Vpdv      = row.Vpdv,
        Kontoa    = row.Kontoa,
        Konto     = row.Konto,
        Brnal     = row.Brnal,
        Datslanja = row.Datslanja,
        Datpdv    = datpdv,
        Datdok    = row.Datdok,
        Brrac     = row.Brrac,
        Valuta    = row.Valuta,
        Sifra     = row.Sifra,
        Ukprod    = row.Osn18 + row.Pdv18 + row.Osn0,
        Osn18     = row.Osn18,
        Pdv18     = row.Pdv18,
        Osn8      = row.Osn8,
        Pdv8      = row.Pdv8,
        Ukupno    = row.Ukupno,
        Pdv       = row.Pdv,
        Osn0      = row.Osn0,
        Dok       = row.Dok,
        Dev       = row.Dev,
        Devkurs   = row.Devkurs,
        Devdug    = row.Devdug,
        Devpot    = row.Devpot,
        Kurs      = row.Kurs,
        Arhiva    = row.Arhiva,
        Naziv     = row.Naziv,
        Pib       = row.Pib,
        Pogon     = row.Pogon,
        Povezanol = row.Povezanol,
        Vrstaf    = row.Vrstaf,
        Preneto   = row.Preneto,
        Numred    = row.Numred,
        Idbr      = row.Idbr,
    };

    [RelayCommand]
    private void BrisanjeProknjizenih()
    {
        if (MessageBox.Show("Brisanje svih proknjiženih stavki (ARHIVA='*' i BRNAL≠'      '). Nastaviti?",
                "BRISANJE PROKNJIŽENIH", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var toRemove = Redovi
            .Where(r => r.Brnal.Trim() != string.Empty && r.Arhiva.Trim() == "*")
            .ToList();

        foreach (var r in toRemove) Redovi.Remove(r);
        SelektovaniRed = Redovi.Count > 0 ? Redovi[0] : null;
        Snimi();
        AzurirajLblRec();
    }

    [RelayCommand]
    private void TraziDatpdv()
    {
        var input = Interaction.InputBox("Unesite datum PDV (dd.MM.yyyy):", "TRAŽENJE DATUMA PDV", string.Empty);
        if (string.IsNullOrWhiteSpace(input)) return;
        if (!DateTime.TryParse(input, out var dat)) return;
        var found = Redovi.FirstOrDefault(r => r.Datpdv?.Date == dat.Date);
        if (found != null) SelektovaniRed = found;
    }

    [RelayCommand]
    private void TraziRacun()
    {
        var input = Interaction.InputBox("Unesite broj računa:", "TRAŽENJE RAČUNA", string.Empty);
        if (string.IsNullOrWhiteSpace(input)) return;
        var found = Redovi.FirstOrDefault(r => r.Brrac.Trim().Equals(input.Trim(), StringComparison.OrdinalIgnoreCase));
        if (found != null) SelektovaniRed = found;
    }

    [RelayCommand]
    private void TraziNalog()
    {
        var input = Interaction.InputBox("Unesite broj naloga:", "TRAŽENJE NALOGA", string.Empty);
        if (string.IsNullOrWhiteSpace(input)) return;
        var found = Redovi.FirstOrDefault(r => r.Brnal.Trim().Equals(input.Trim(), StringComparison.OrdinalIgnoreCase));
        if (found != null) SelektovaniRed = found;
    }

    [RelayCommand]
    private void UnosKursa()
    {
        var vm = new NaluniorKursViewModel(_firmPath, Redovi);
        vm.ObradaZavrsena += Snimi;
        new Views.NaluniorKursWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void OtvoriKontiranje()
    {
        var vm  = new NaluniorkonViewModel(_firmPath);
        new Views.NaluniorkonWindow(vm).ShowDialog();
    }

    [RelayCommand] private void IdiNaVrh() { if (Redovi.Count > 0) SelektovaniRed = Redovi[0]; }
    [RelayCommand] private void IdiNaDno()  { if (Redovi.Count > 0) SelektovaniRed = Redovi[^1]; }
    [RelayCommand] private void IdiGore()
    {
        if (SelektovaniRed == null) return;
        var idx = Redovi.IndexOf(SelektovaniRed);
        if (idx > 0) SelektovaniRed = Redovi[idx - 1];
    }
    [RelayCommand] private void IdiDole()
    {
        if (SelektovaniRed == null) return;
        var idx = Redovi.IndexOf(SelektovaniRed);
        if (idx < Redovi.Count - 1) SelektovaniRed = Redovi[idx + 1];
    }

    [RelayCommand]
    private void Izlaz()
    {
        Snimi();
        ZatvoriFormu?.Invoke();
    }
}
