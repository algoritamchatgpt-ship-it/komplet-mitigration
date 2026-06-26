using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.IO;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALPMATIZ — EVIDENCIJA IZLAZA MATERIJALA (ALT+F3 iz nalp2)</summary>
public partial class NalpMatizViewModel : ObservableObject
{
    private readonly string _firmPath;

    public NalpRow Row   { get; }
    public string  Naziv { get; }

    [ObservableProperty] private decimal _mStanje;
    [ObservableProperty] private decimal _mSaldo;

    public event Action? ZatvoriFormu;

    public NalpMatizViewModel(NalpRow row, string firmPath)
    {
        Row        = row;
        _firmPath  = firmPath;
        Naziv      = NazivKonta(row.Konto.Trim(), firmPath);
        IzracunajStanje();
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

    // ── Init: compute MStanje/MSaldo from NAL.dbf ─────────────
    private void IzracunajStanje()
    {
        var (stanje, saldo) = BeremStanjeIzNal();
        MStanje = stanje;
        MSaldo  = saldo;
        if (stanje != 0)
            Row.Cena = Math.Round(saldo / stanje, 3);
        else
            Row.Cena = 0;
    }

    private (decimal stanje, decimal saldo) BeremStanjeIzNal()
    {
        var nalPath = Path.Combine(_firmPath, "nal.dbf");
        if (!File.Exists(nalPath)) return (0, 0);
        try
        {
            decimal mkol = 0, mkoli = 0, mdug = 0, mpot = 0;
            var konto = Row.Konto.Trim();
            foreach (var rec in new SimpleDbfReader(nalPath).Zapisi())
            {
                if (!rec.DajString("KONTO").Trim().Equals(konto, StringComparison.OrdinalIgnoreCase))
                    continue;
                var dug  = rec.DajDecimal("DUG");
                var pot  = rec.DajDecimal("POT");
                if (dug != 0)
                {
                    mdug += dug;
                    mkol += rec.DajDecimal("ULAZ");
                }
                else
                {
                    mpot  += pot;
                    mkoli += rec.DajDecimal("IZLAZ");
                }
            }
            return (mkol - mkoli, mdug - mpot);
        }
        catch { return (0, 0); }
    }

    // ── Obračun (Command1) ────────────────────────────────────
    [RelayCommand]
    private void Obracun()
    {
        var (stanje, saldo) = BeremStanjeIzNal();
        MStanje = stanje;
        MSaldo  = saldo;
        decimal mcena = stanje != 0 ? Math.Round(saldo / stanje, 3) : 0;
        Row.Cena    = mcena;
        Row.UkupnoP = Math.Round(Row.Izlaz * Row.Cena, 2);
        Row.Pot     = Row.UkupnoP;
    }

    // ── Izlaz (Unload: REPLACE POT WITH UKUPNO_P) ────────────
    [RelayCommand]
    private void Izlaz()
    {
        Row.Pot = Row.UkupnoP;
        ZatvoriFormu?.Invoke();
    }
}
