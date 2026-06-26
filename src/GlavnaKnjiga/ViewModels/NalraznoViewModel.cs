using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Views;
using Microsoft.VisualBasic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalraznoViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly int _godina;

    public event Action? ZatvoriFormu;

    public NalraznoViewModel(string firmPath, int godina)
    {
        _firmPath = firmPath;
        _godina = godina;
    }

    [RelayCommand]
    private void ZamenaKonta()
    {
        var vm = new NalzamkonViewModel(_firmPath);
        new NalzamkonWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void UtvrdjivanjeRezultata()
    {
        var vm = new Nalrezul6ViewModel(_firmPath, _godina);
        new Nalrezul6Window(vm).ShowDialog();
    }

    [RelayCommand]
    private void ZatvaranjeKlasa()
    {
        var vm = NalzatViewModel.ZaKlase(_firmPath, _godina);
        new NalzatWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void ZatvaranjeAktiveIPasive()
    {
        var vm = NalzatViewModel.ZaAktivuIPasivu(_firmPath, _godina);
        new NalzatWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void KopiranjeNaloga()
    {
        var brnal = IzaberiNalog();
        if (brnal == null) return;
        new NalkopWindow(_firmPath, brnal).ShowDialog();
    }

    [RelayCommand]
    private void SlaganjeNaloga()
    {
        var vm = new NalslagViewModel(_firmPath, _godina);
        new NalslagWindow(vm, _firmPath).ShowDialog();
    }

    [RelayCommand]
    private void ProveraMaterijala()
    {
        var vm = new NalmatViewModel(_firmPath, _godina);
        new NalmatWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PlanLikvidnosti()
    {
        var vm = new NalprilViewModel(_firmPath);
        new NalprilWindow(vm, _firmPath).ShowDialog();
    }

    [RelayCommand]
    private void Parametri()
    {
        var path = Dbf("datumi.dbf");
        if (path == null)
        {
            MessageBox.Show("datumi.dbf ne postoji u folderu firme.",
                "Parametri knjiženja", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            var rows = UcitajRedove(path, schema);
            if (rows.Count == 0)
            {
                MessageBox.Show("datumi.dbf nema nijedan zapis.", "Parametri knjiženja",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var trenutno = Vrednost(rows[0], "PAR2");
            var unos = Interaction.InputBox(
                "PRIKAZ NALOGA SA ANALITIKOM (1=DA):",
                "PARAMETRI KNJIŽENJA", trenutno).Trim();
            if (string.IsNullOrEmpty(unos)) return;

            rows[0]["PAR2"] = unos[..1];
            SnimiRedove(path, schema, rows);
            MessageBox.Show("Parametar PAR2 je sačuvan.", "Parametri knjiženja",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri snimanju parametra:\n{ex.Message}",
                "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void UnosDokumentaZaKonto()
    {
        var konto = Interaction.InputBox("Unesite konto:", "UNOS DOKUMENTA ZA KONTO", "").Trim();
        if (string.IsNullOrEmpty(konto)) return;

        var dok = Interaction.InputBox("Unesite dokument:", "UNOS DOKUMENTA ZA KONTO", "").Trim();
        if (string.IsNullOrEmpty(dok)) return;

        if (MessageBox.Show($"Polje DOK biće postavljeno na '{dok}' za konto '{konto}'. Nastaviti?",
                "UNOS DOKUMENTA ZA KONTO", MessageBoxButton.YesNo, MessageBoxImage.Question) !=
            MessageBoxResult.Yes)
            return;

        var path = Dbf("nal.dbf");
        if (path == null)
        {
            MessageBox.Show("nal.dbf ne postoji.", "UNOS DOKUMENTA ZA KONTO",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            var rows = UcitajRedove(path, schema);
            var promenjeno = 0;
            foreach (var row in rows.Where(r =>
                         Vrednost(r, "KONTO").Equals(konto, StringComparison.OrdinalIgnoreCase)))
            {
                row["DOK"] = dok;
                promenjeno++;
            }

            SnimiRedove(path, schema, rows);
            MessageBox.Show($"Izmenjeno redova: {promenjeno}.", "UNOS DOKUMENTA ZA KONTO",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri unosu dokumenta:\n{ex.Message}",
                "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void DopuniKonto()
    {
        var vm = new NalDopuniKontoViewModel(_firmPath);
        new NalDopuniKontoWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void KaskadnoSlaganje()
    {
        var brnal = Interaction.InputBox("Unesite broj naloga:", "KASKADNO SLAGANJE", "").Trim();
        if (string.IsNullOrEmpty(brnal)) return;

        if (MessageBox.Show(
                $"Nalog '{brnal}' biće obrisan iz svih ANAL<n>.DBF tabela. Nastaviti?",
                "KASKADNO SLAGANJE", MessageBoxButton.YesNo, MessageBoxImage.Warning) !=
            MessageBoxResult.Yes)
            return;

        try
        {
            var analFajlovi = Directory.EnumerateFiles(_firmPath, "*.dbf")
                .Where(p => Regex.IsMatch(Path.GetFileName(p), @"^anal\d+\.dbf$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var ukupnoObrisano = 0;
            foreach (var path in analFajlovi)
            {
                var schema = DbfTableWriter.LoadSchema(path);
                var rows = UcitajRedove(path, schema);
                var pre = rows.Count;
                rows.RemoveAll(r =>
                    Vrednost(r, "BRNAL").Equals(brnal, StringComparison.OrdinalIgnoreCase) ||
                    (Broj(r, "DUG") == 0 && Broj(r, "POT") == 0 &&
                     Broj(r, "DEVDUG") == 0 && Broj(r, "DEVPOT") == 0));
                ukupnoObrisano += pre - rows.Count;
                SnimiRedove(path, schema, rows);
            }

            MessageBox.Show(
                $"Obrađeno tabela: {analFajlovi.Count}.\nObrisano redova: {ukupnoObrisano}.",
                "KASKADNO SLAGANJE", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri kaskadnom slaganju:\n{ex.Message}",
                "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void KorekcijaPdvDatuma()
    {
        var nalPath = Dbf("nal.dbf");
        if (nalPath == null)
        {
            MessageBox.Show("nal.dbf ne postoji.", "Korekcija PDV datuma",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show(
                "Biće dopunjena prazna polja DATPRI i DATPDV u svim redovima nal.dbf. Nastaviti?",
                "Korekcija PDV datuma", MessageBoxButton.YesNo, MessageBoxImage.Question) !=
            MessageBoxResult.Yes)
            return;

        try
        {
            var schema = DbfTableWriter.LoadSchema(nalPath);
            var rows = new SimpleDbfReader(nalPath).Zapisi()
                .Select(Nalp2ViewModel.NalpRowFromRecord)
                .ToList();

            var promenjeno = 0;
            foreach (var row in rows)
            {
                if (row.Datpri == null)
                {
                    row.Datpri = row.Datdok;
                    promenjeno++;
                }

                if (row.Datpdv == null)
                {
                    row.Datpdv = row.Datpri;
                    promenjeno++;
                }
            }

            DbfTableWriter.WriteTable(nalPath, schema, rows, Nalp2ViewModel.NalpRowFieldMapper);
            MessageBox.Show($"Korekcija je završena. Izmenjeno polja: {promenjeno}.",
                "Korekcija PDV datuma", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri korekciji PDV datuma:\n{ex.Message}",
                "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void KorekcijaDatumaKnjizenja()
    {
        var nalPath = Dbf("nal.dbf");
        if (nalPath == null)
        {
            MessageBox.Show("nal.dbf ne postoji.", "Korekcija datuma knjiženja",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show(
                "Biće dopunjeno prazno polje DATUM u svim redovima nal.dbf. Nastaviti?",
                "Korekcija datuma knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question) !=
            MessageBoxResult.Yes)
            return;

        try
        {
            var datumiNaloga = UcitajDatumeNaloga();
            var schema = DbfTableWriter.LoadSchema(nalPath);
            var rows = new SimpleDbfReader(nalPath).Zapisi()
                .Select(Nalp2ViewModel.NalpRowFromRecord)
                .ToList();

            var promenjeno = 0;
            foreach (var row in rows)
            {
                if (row.Datum != null) continue;
                if (!datumiNaloga.TryGetValue(row.Brnal.Trim(), out var datum)) continue;
                row.Datum = datum;
                promenjeno++;
            }

            var poslednjiDatum = new DateTime(_godina, 1, 1);
            foreach (var row in rows)
            {
                if (row.Datum != null)
                {
                    poslednjiDatum = row.Datum.Value;
                    continue;
                }

                row.Datum = poslednjiDatum;
                promenjeno++;
            }

            DbfTableWriter.WriteTable(nalPath, schema, rows, Nalp2ViewModel.NalpRowFieldMapper);
            MessageBox.Show($"Korekcija je završena. Izmenjeno redova: {promenjeno}.",
                "Korekcija datuma knjiženja", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri korekciji datuma knjiženja:\n{ex.Message}",
                "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    private Dictionary<string, DateTime> UcitajDatumeNaloga()
    {
        var rezultat = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        var path = Dbf("nalbroj.dbf");
        if (path == null) return rezultat;

        foreach (var rec in new SimpleDbfReader(path).Zapisi())
        {
            var brnal = rec.DajString("BRNAL").Trim();
            var datum = rec.DajDate("DATKNJI");
            if (!string.IsNullOrEmpty(brnal) && datum != null)
                rezultat[brnal] = datum.Value;
        }

        return rezultat;
    }

    private string? IzaberiNalog()
    {
        var nalozi = new List<string>();
        var path = Dbf("nalbroj.dbf");
        if (path != null)
        {
            try
            {
                foreach (var rec in new SimpleDbfReader(path).Zapisi())
                {
                    var brnal = rec.DajString("BRNAL").Trim();
                    if (string.IsNullOrEmpty(brnal)) continue;
                    nalozi.Add($"{brnal}  {rec.DajString("VRNAL").Trim()}  {rec.DajDate("DATNAL"):dd.MM.yyyy}");
                }
            }
            catch { }
        }

        var dlg = new NalogIzborWindow(nalozi);
        return dlg.ShowDialog() == true ? dlg.IzabraniKod : null;
    }

    private static List<Dictionary<string, object?>> UcitajRedove(
        string path, DbfTableWriter.DbfSchema schema)
    {
        var rezultat = new List<Dictionary<string, object?>>();
        foreach (var rec in new SimpleDbfReader(path, schema.Encoding).Zapisi())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in schema.Fields)
            {
                row[field.Name] = field.Type switch
                {
                    'D' => rec.DajDate(field.Name),
                    'N' or 'F' => rec.DajDecimal(field.Name),
                    'L' => rec.DajBool(field.Name),
                    _ => rec.DajString(field.Name),
                };
            }
            rezultat.Add(row);
        }
        return rezultat;
    }

    private static void SnimiRedove(string path, DbfTableWriter.DbfSchema schema,
        List<Dictionary<string, object?>> rows) =>
        DbfTableWriter.WriteTable(path, schema, rows,
            (row, field) => row.TryGetValue(field, out var value) ? value : null);

    private static string Vrednost(Dictionary<string, object?> row, string field) =>
        row.TryGetValue(field, out var value) ? Convert.ToString(value)?.Trim() ?? string.Empty : string.Empty;

    private static decimal Broj(Dictionary<string, object?> row, string field)
    {
        if (!row.TryGetValue(field, out var value) || value == null) return 0;
        try { return Convert.ToDecimal(value); }
        catch { return 0; }
    }

    private string? Dbf(string name)
    {
        var path = Path.Combine(_firmPath, name);
        return File.Exists(path) ? path : null;
    }
}
