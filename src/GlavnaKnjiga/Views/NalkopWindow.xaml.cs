using Algoritam.Core.Services.Dbf;
using GlavnaKnjiga.Models;
using GlavnaKnjiga.ViewModels;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.Views;

public partial class NalkopWindow : Window
{
    private readonly string _firmPath;
    private readonly string _sourceBrnal;

    public NalkopWindow(string firmPath, string sourceBrnal)
    {
        InitializeComponent();
        _firmPath    = firmPath;
        _sourceBrnal = sourceBrnal;
        LblStariBrnal.Content = sourceBrnal.Trim();
    }

    private void BtnKopiram_Click(object sender, RoutedEventArgs e)
    {
        var noviBrnal = TxtNoviBrnal.Text.Trim();
        var vrednost  = TxtVrednost.Text.Trim().ToUpper();
        var datdok    = TxtDatdok.SelectedDate;

        if (string.IsNullOrEmpty(noviBrnal))
        {
            MessageBox.Show("Unesite novi nalog.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var nalPath  = Path.Combine(_firmPath, "nal.dbf");
        var nalpPath = Path.Combine(_firmPath, "nalp.dbf");

        if (!File.Exists(nalPath))
        {
            MessageBox.Show("Datoteka nal.dbf ne postoji.", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (!File.Exists(nalpPath))
        {
            MessageBox.Show("Datoteka nalp.dbf ne postoji.", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            // 1. read source rows from nal.dbf filtered by source BRNAL
            var nalReader  = new SimpleDbfReader(nalPath);
            var sourceRows = nalReader.Zapisi()
                .Where(rec => rec.DajString("BRNAL").Trim() == _sourceBrnal.Trim())
                .Select(rec => Nalp2ViewModel.NalpRowFromRecord(rec))
                .ToList();

            if (sourceRows.Count == 0)
            {
                MessageBox.Show($"Nalog '{_sourceBrnal.Trim()}' nije pronađen u nal.dbf.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 2. clone, set new BRNAL + DATDOK, optionally negate DUG/POT
            var newRows = sourceRows.Select(r =>
            {
                var klon  = r.Clone();
                klon.Brnal  = noviBrnal.PadRight(6);
                klon.Datdok = datdok;
                if (vrednost == "N")
                {
                    klon.Dug = -klon.Dug;
                    klon.Pot = -klon.Pot;
                }
                return klon;
            }).ToList();

            // 3. read existing nalp.dbf, append new rows, write back
            var schema     = DbfTableWriter.LoadSchema(nalpPath);
            var nalpReader = new SimpleDbfReader(nalpPath);
            var existing   = nalpReader.Zapisi()
                .Select(rec => Nalp2ViewModel.NalpRowFromRecord(rec))
                .ToList();

            var svi = existing.Concat(newRows).ToList();
            DbfTableWriter.WriteTable(nalpPath, schema, svi, Nalp2ViewModel.NalpRowFieldMapper);

            MessageBox.Show(
                $"Nalog '{_sourceBrnal.Trim()}' kopiran kao '{noviBrnal}'.\nKopirano {newRows.Count} redova.",
                "KOPIRANJE NALOGA", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri kopiranju: {ex.Message}", "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnIzlaz_Click(object sender, RoutedEventArgs e) => Close();
}
