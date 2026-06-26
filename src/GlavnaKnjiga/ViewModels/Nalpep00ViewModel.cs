using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class Nalpep00ViewModel : ObservableObject
{
    private readonly string _firmPath;

    [ObservableProperty] private string _putanja = string.Empty;

    public string Naslov { get; set; } = "PUTANJA ZA PREUZIMANJE IZVODA";

    public event Action? ZatvoriFormu;

    public Nalpep00ViewModel(string firmPath) => _firmPath = firmPath;

    [RelayCommand]
    private void Preuzmi()
    {
        var mfile = Putanja.Trim();
        if (!File.Exists(mfile))
        {
            MessageBox.Show("Datoteka ne postoji.", "PREUZIMANJE FX",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var allLines = File.ReadAllLines(mfile);
            if (allLines.Length < 2) return;

            var headerLine = allLines[0];
            DateTime mDatDok;
            try
            {
                var day = headerLine.Substring(0, 2);
                var mon = headerLine.Substring(2, 2);
                var yr = headerLine.Substring(4, 4);
                mDatDok = new DateTime(int.Parse(yr), int.Parse(mon), int.Parse(day));
            }
            catch
            {
                MessageBox.Show("Neispravan format datuma u prvoj liniji.", "PREUZIMANJE FX",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var nalpepPath = Path.Combine(_firmPath, "nalpep.dbf");
            var an0Path = Path.Combine(_firmPath, "an0.dbf");

            if (!File.Exists(nalpepPath))
            {
                MessageBox.Show("nalpep.dbf ne postoji.", "PREUZIMANJE FX",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var an0Records = File.Exists(an0Path)
                ? new SimpleDbfReader(an0Path).Zapisi().ToList()
                : new List<DbfRecord>();

            var existingRows = new SimpleDbfReader(nalpepPath).Zapisi()
                .Select(NalpepRowFromRecord)
                .ToList();

            foreach (var line in allLines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var iznosStr = line.Length >= 13 ? line.Substring(0, 13).Trim() : line.Trim();
                var naziv = line.Length > 30 ? line.Substring(13, Math.Min(18, line.Length - 13)).Trim() : "";
                var svrha = line.Length > 48 ? line.Substring(31, Math.Min(34, line.Length - 31)).Trim() : "";
                var pozivNaP = line.Length > 82 ? line.Substring(65, Math.Min(17, line.Length - 65)).Trim() : "";
                var pozivNaZ = line.Length > 99 ? line.Substring(82, Math.Min(17, line.Length - 82)).Trim() : "";
                var racunP = line.Length > 116 ? line.Substring(99, Math.Min(20, line.Length - 99)).Trim() : "";
                var dp = line.Length > 119 ? line.Substring(119, 1).Trim() : "";

                decimal.TryParse(iznosStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var iznos);

                var row = new NalpepRow
                {
                    Datdok = mDatDok,
                    Valuta = mDatDok,
                    Opis = svrha,
                    Naziv = naziv,
                    Pozivp = pozivNaP,
                    Pozivz = pozivNaZ,
                    Brrac = pozivNaP,
                };

                if (iznos != 0)
                {
                    if (dp.Equals("D", StringComparison.OrdinalIgnoreCase))
                        row.Dug = iznos;
                    else
                        row.Pot = iznos;
                }

                var mRacunP = StripDashesAndZeros(racunP);

                var sifra = FindPartnerByAccount(an0Records, mRacunP);
                if (!string.IsNullOrEmpty(sifra))
                {
                    row.Sifra = sifra;
                    row.Konto = row.Pot != 0 ? "2040000000" : "4350000000";
                }
                else
                {
                    row.Konto = "9999999999";
                }

                existingRows.Add(row);
            }

            var schema = DbfTableWriter.LoadSchema(nalpepPath);
            DbfTableWriter.WriteTable(nalpepPath, schema, existingRows, NalpepFieldMapper);

            MessageBox.Show($"Preuzimanje završeno. Učitano stavki: {allLines.Length - 1}.",
                "PREUZIMANJE FX", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška: {ex.Message}", "PREUZIMANJE FX",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void PutanjaBrowse()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Izaberite FX izvod datoteku",
            Filter = "SDF datoteke|*.sdf;*.txt|Sve datoteke|*.*"
        };
        if (dlg.ShowDialog() == true)
            Putanja = dlg.FileName;
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    private static string StripDashesAndZeros(string input)
    {
        var result = input.Replace("-", "");
        while (result.StartsWith("0") && result.Length > 1)
            result = result.Substring(1);
        return result;
    }

    private static string FindPartnerByAccount(List<DbfRecord> an0Records, string account)
    {
        foreach (var rec in an0Records)
        {
            for (int i = 1; i <= 6; i++)
            {
                var field = i == 1 ? "ZIRORAC" : $"AZIRORAC{i}";
                var val = StripDashesAndZeros(rec.DajString(field));
                if (string.Equals(val, account, StringComparison.OrdinalIgnoreCase))
                    return rec.DajString("SIFRA");
            }
        }
        return "";
    }

    private static NalpepRow NalpepRowFromRecord(DbfRecord rec) => new()
    {
        Idbr = rec.DajDecimal("IDBR"),
        Konto = rec.DajString("KONTO"),
        Dug = rec.DajDecimal("DUG"),
        Pot = rec.DajDecimal("POT"),
        Opis = rec.DajString("OPIS"),
        Sifra = rec.DajString("SIFRA"),
        Naziv = rec.DajString("NAZIV"),
        Brrac = rec.DajString("BRRAC"),
        Datdok = rec.DajDate("DATDOK"),
        Valuta = rec.DajDate("VALUTA"),
        Brnal = rec.DajString("BRNAL"),
        Pozivz = rec.DajString("POZIVZ"),
        Pozivp = rec.DajString("POZIVP"),
        Mp = rec.DajString("MP"),
        Mtr = rec.DajDecimal("MTR"),
        Dok = rec.DajString("DOK"),
        Preneto = rec.DajString("PRENETO"),
    };

    private static object? NalpepFieldMapper(NalpepRow row, string field) => field switch
    {
        "IDBR" => row.Idbr,
        "KONTO" => row.Konto,
        "DUG" => row.Dug,
        "POT" => row.Pot,
        "OPIS" => row.Opis,
        "SIFRA" => row.Sifra,
        "NAZIV" => row.Naziv,
        "BRRAC" => row.Brrac,
        "DATDOK" => row.Datdok,
        "VALUTA" => row.Valuta,
        "BRNAL" => row.Brnal,
        "POZIVZ" => row.Pozivz,
        "POZIVP" => row.Pozivp,
        "MP" => row.Mp,
        "MTR" => row.Mtr,
        "DOK" => row.Dok,
        "PRENETO" => row.Preneto,
        _ => null,
    };
}
