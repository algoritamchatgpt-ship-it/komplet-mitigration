using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalkaskadViewModel : ObservableObject
{
    private readonly string _firmPath;

    [ObservableProperty] private string _brojNaloga = string.Empty;

    public event Action? ZatvoriFormu;

    public NalkaskadViewModel(string firmPath) => _firmPath = firmPath;

    [RelayCommand]
    private void Potvrda()
    {
        var mBrnal = BrojNaloga.Trim();
        if (string.IsNullOrEmpty(mBrnal) || mBrnal.Length != 6)
        {
            MessageBox.Show("Unesite ispravan 6-cifreni broj naloga.", "KASKADNO SLAGANJE",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show(
                $"Brisanje naloga {mBrnal} iz svih analitičkih evidencija?\n\n" +
                "Napomena: Ovom opcijom briše se odredjeni nalog u svim analitikama.\n" +
                "Opcija može poslužiti za brisanje obračunatih kursnih razlika radi ponovnog obračuna.",
                "KASKADNO SLAGANJE NALOGA",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            var aaanPath = Path.Combine(_firmPath, "aaan.dbf");
            int mReca = 0;

            if (File.Exists(aaanPath))
                mReca = new SimpleDbfReader(aaanPath).Zapisi().Count();

            if (mReca > 0)
            {
                for (int i = 1; i <= mReca; i++)
                {
                    var analFile = $"anal{i}.dbf";
                    var analPath = Path.Combine(_firmPath, analFile);
                    if (File.Exists(analPath))
                    {
                        var schema = DbfTableWriter.LoadSchema(analPath);
                        var rows = new SimpleDbfReader(analPath).Zapisi()
                            .Select(Nalp2ViewModel.NalpRowFromRecord)
                            .ToList();

                        var toRemove = rows.Where(r =>
                            r.Brnal.Trim() == mBrnal ||
                            (r.Dug == 0 && r.Pot == 0 && r.Devdug == 0 && r.Devpot == 0)).ToList();

                        if (toRemove.Count > 0)
                        {
                            foreach (var r in toRemove) rows.Remove(r);
                            DbfTableWriter.WriteTable(analPath, schema, rows, Nalp2ViewModel.NalpRowFieldMapper);
                        }
                    }
                }
            }

            MessageBox.Show($"Brisanje naloga {mBrnal} završeno.", "KASKADNO SLAGANJE",
                MessageBoxButton.OK, MessageBoxImage.Information);

            ZatvoriFormu?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška: {ex.Message}", "KASKADNO SLAGANJE",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();
}
