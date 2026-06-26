using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALUNIOR0 — "PRIJEM RAČUNA IZ STRIPA": copies records from unior0.dbf → unior.dbf</summary>
public partial class NaluniorPrijemViewModel : ObservableObject
{
    private readonly string _firmPath;

    public event Action? ZatvoriFormu;

    [ObservableProperty] private DateTime _dat0 = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime _dat1 = DateTime.Today;

    public NaluniorPrijemViewModel(string firmPath) => _firmPath = firmPath;

    [RelayCommand]
    private void Prijem()
    {
        var unior0Path = Path.Combine(_firmPath, "unior0.dbf");
        var uniorPath  = Path.Combine(_firmPath, "unior.dbf");

        if (!File.Exists(unior0Path))
        {
            MessageBox.Show("unior0.dbf ne postoji.", "PRIJEM",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var mDat0 = Dat0.Date;
        var mDat1 = Dat1.Date;

        // Read all rows from unior0 in date range
        var candidates = new SimpleDbfReader(unior0Path).Zapisi()
            .Where(r => { var d = r.DajDate("DATSLANJA"); return d.HasValue && d.Value.Date >= mDat0 && d.Value.Date <= mDat1; })
            .Select(NaluniorViewModel.MapUniorRowFromRecord)
            .ToList();

        if (candidates.Count == 0)
        {
            MessageBox.Show("Nema stavki u zadatom periodu.", "PRIJEM",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!File.Exists(uniorPath))
        {
            MessageBox.Show("unior.dbf ne postoji.", "PRIJEM",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Load existing unior rows to exclude duplicates by IDBR
        var existingIdbr = new HashSet<decimal>();
        foreach (var rec in new SimpleDbfReader(uniorPath).Zapisi())
            existingIdbr.Add(rec.DajDecimal("IDBR"));

        var toAppend = candidates.Where(r => !existingIdbr.Contains(r.Idbr)).ToList();

        if (toAppend.Count == 0)
        {
            MessageBox.Show("Sve stavke su već preuzete (nalaze se u unior.dbf).", "PRIJEM",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var schema = DbfTableWriter.LoadSchema(uniorPath);
            var existing = new SimpleDbfReader(uniorPath).Zapisi()
                .Select(NaluniorViewModel.MapUniorRowFromRecord)
                .ToList();

            existing.AddRange(toAppend);

            DbfTableWriter.WriteTable(uniorPath, schema, existing, NaluniorViewModel.UniorFieldMapper);
            MessageBox.Show($"Prijem završen. Preuzeto stavki: {toAppend.Count}.",
                "PRIJEM", MessageBoxButton.OK, MessageBoxImage.Information);

            ZatvoriFormu?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri prijemu:\n{ex.Message}", "PRIJEM",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand] private void Izlaz() => ZatvoriFormu?.Invoke();
}
