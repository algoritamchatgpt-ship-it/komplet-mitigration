using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.IO;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALPMATUL — EVIDENCIJA ULAZA MATERIJALA (ALT+F2 iz nalp2)</summary>
public partial class NalpMatulViewModel : ObservableObject
{
    public NalpRow Row { get; }
    public string  Naziv { get; }

    public event Action? ZatvoriFormu;

    public NalpMatulViewModel(NalpRow row, string firmPath)
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

    // ── LostFocus handlers ────────────────────────────────────
    public void OnCenaLostFocus()
    {
        if (Row.Ulaz != 0)
        {
            if (Row.Cena != 0)
                Row.UkupnoD = Math.Round(Row.Ulaz * Row.Cena, 2);
            else
                Row.UkupnoD = 0;
        }
        else
        {
            Row.Cena    = 0;
            Row.UkupnoD = 0;
        }
        Row.Dug = Row.UkupnoD;
    }

    public void OnUlazLostFocus()
    {
        if (Row.Ulaz != 0)
        {
            if (Row.Cena == 0 && Row.UkupnoD != 0)
                Row.Cena = Math.Round(Row.UkupnoD / Row.Ulaz, 2);
        }
        else
        {
            Row.Cena    = 0;
            Row.UkupnoD = 0;
        }
    }

    // ── Izlaz (Unload: REPLACE DUG WITH UKUPNO_D) ────────────
    [RelayCommand]
    private void Izlaz()
    {
        Row.Dug = Row.UkupnoD;
        ZatvoriFormu?.Invoke();
    }
}
