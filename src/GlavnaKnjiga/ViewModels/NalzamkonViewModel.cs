using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalzamkonViewModel : ObservableObject
{
    private readonly string _firmPath;

    public event Action? ZatvoriFormu;

    [ObservableProperty] private string _stariKonto = string.Empty;
    [ObservableProperty] private string _noviKonto = string.Empty;

    public NalzamkonViewModel(string firmPath)
    {
        _firmPath = firmPath;
    }

    [RelayCommand]
    private void Zamena()
    {
        StariKonto = StariKonto.Trim();
        NoviKonto = NoviKonto.Trim();

        if (string.IsNullOrEmpty(StariKonto) || string.IsNullOrEmpty(NoviKonto))
        {
            MessageBox.Show("Unesite stari i novi konto.", "ZAMENA KONTA",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (StariKonto.Equals(NoviKonto, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Stari i novi konto su isti.", "ZAMENA KONTA",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var path = Path.Combine(_firmPath, "nal.dbf");
        if (!File.Exists(path))
        {
            MessageBox.Show("nal.dbf ne postoji.", "ZAMENA KONTA",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show(
                $"Konto '{StariKonto}' biće zamenjen kontom '{NoviKonto}' u nal.dbf. Nastaviti?",
                "ZAMENA KONTA", MessageBoxButton.YesNo, MessageBoxImage.Warning) !=
            MessageBoxResult.Yes)
            return;

        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            var rows = new SimpleDbfReader(path).Zapisi()
                .Select(Nalp2ViewModel.NalpRowFromRecord)
                .ToList();
            var promenjeno = ZameniKonto(rows, StariKonto, NoviKonto);

            if (promenjeno > 0)
                DbfTableWriter.WriteTable(path, schema, rows, Nalp2ViewModel.NalpRowFieldMapper);

            MessageBox.Show($"Zamena je završena. Izmenjeno redova: {promenjeno}.",
                "ZAMENA KONTA", MessageBoxButton.OK, MessageBoxImage.Information);
            ZatvoriFormu?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri zameni konta:\n{ex.Message}",
                "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    internal static int ZameniKonto(IList<NalpRow> rows, string stariKonto, string noviKonto)
    {
        var stari = stariKonto.Trim();
        var novi = noviKonto.Trim();
        var promenjeno = 0;

        foreach (var row in rows)
        {
            if (!row.Konto.Trim().Equals(stari, StringComparison.OrdinalIgnoreCase)) continue;
            row.Konto = novi;
            promenjeno++;
        }

        return promenjeno;
    }
}
