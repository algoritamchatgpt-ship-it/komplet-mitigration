using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALPLDAT — DATUMI: date range editor (dat0/dat1 for reports)</summary>
public partial class NalPlDatViewModel : ObservableObject
{
    private readonly string _dbfPath;
    public event Action? ZatvoriFormu;

    [ObservableProperty] private DateTime _dat0 = new DateTime(DateTime.Today.Year, 1, 1);
    [ObservableProperty] private DateTime _dat1 = new DateTime(DateTime.Today.Year, 12, 31);

    public NalPlDatViewModel(string firmPath)
    {
        _dbfPath = Path.Combine(firmPath, "nalpldat.dbf");
        Ucitaj();
    }

    private void Ucitaj()
    {
        if (!File.Exists(_dbfPath)) return;
        var r = new SimpleDbfReader(_dbfPath);
        foreach (var rec in r.Zapisi())
        {
            Dat0 = rec.DajDate("DAT0") ?? Dat0;
            Dat1 = rec.DajDate("DAT1") ?? Dat1;
            break;
        }
    }

    [RelayCommand]
    private void Izlaz()
    {
        Snimi();
        ZatvoriFormu?.Invoke();
    }

    private void Snimi()
    {
        if (!File.Exists(_dbfPath)) return;
        try
        {
            var schema = DbfTableWriter.LoadSchema(_dbfPath);
            var rows = new[] { new { Dat0, Dat1 } }.ToList();
            // Use SimpleDbfWriter directly via existing reader/write approach
            var reader = new SimpleDbfReader(_dbfPath);
            var recs   = reader.Zapisi().ToList();
            if (recs.Count == 0) return;
            // We need to write DAT0/DAT1 back — using typed row
            var typedRows = new List<(DateTime D0, DateTime D1)> { (Dat0, Dat1) };
            DbfTableWriter.WriteTable(_dbfPath, schema, typedRows,
                (r, f) => f switch { "DAT0" => (object?)r.D0, "DAT1" => r.D1, _ => null });
        }
        catch { }
    }
}
