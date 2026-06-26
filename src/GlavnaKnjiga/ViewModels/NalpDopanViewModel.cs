using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.IO;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALPDOPAN — DOPUNSKI PODACI ANALITIKE (ALT+F6 iz nalp2)</summary>
public partial class NalpDopanViewModel : ObservableObject
{
    public NalpRow Row   { get; }
    public string  Naziv { get; }

    public event Action? ZatvoriFormu;

    public NalpDopanViewModel(NalpRow row, string firmPath)
    {
        Row   = row;
        Naziv = NazivKonta(row.Konto.Trim(), firmPath);
    }

    private static string NazivKonta(string konto, string firmPath)
    {
        var path = Path.Combine(firmPath, "konto.dbf");
        if (!File.Exists(path)) return string.Empty;
        try
        {
            foreach (var rec in new SimpleDbfReader(path).Zapisi())
            {
                if (rec.DajString("KONTO").Trim().Equals(konto, StringComparison.OrdinalIgnoreCase))
                    return rec.DajString("NAZIV").Trim();
            }
        }
        catch { }
        return string.Empty;
    }

    // ── Izlaz (just close; row properties already updated by binding) ──
    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();
}
