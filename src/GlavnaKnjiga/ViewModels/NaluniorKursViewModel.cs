using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GlavnaKnjiga.Models;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

/// <summary>NALUNIORKURS — preračun deviznih računa po datumu PDV-a.</summary>
public partial class NaluniorKursViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly IEnumerable<UniorRow> _redovi;

    public event Action? ZatvoriFormu;
    public event Action? ObradaZavrsena;

    [ObservableProperty] private string _deviza = "EUR";
    [ObservableProperty] private string _status = string.Empty;

    public NaluniorKursViewModel(string firmPath, IEnumerable<UniorRow> redovi)
    {
        _firmPath = firmPath;
        _redovi = redovi;
    }

    [RelayCommand]
    private void UnesiKurs()
    {
        var deviza = Deviza.Trim().ToUpperInvariant();
        if (deviza.Length is < 1 or > 3)
        {
            MessageBox.Show(
                "Oznaka devize mora imati najviše 3 znaka.",
                "UNOS KURSA",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            var kursevi = UcitajKurseve(deviza);
            var rezultat = PrimeniKurs(_redovi, deviza, kursevi);
            Status = $"Deviznih računa: {rezultat.Deviznih}. " +
                     $"Pronađen kurs: {rezultat.SaKursom}. " +
                     $"Bez kursa: {rezultat.BezKursa}.";

            ObradaZavrsena?.Invoke();
            MessageBox.Show(
                Status,
                "UNOS KURSA",
                MessageBoxButton.OK,
                rezultat.BezKursa == 0
                    ? MessageBoxImage.Information
                    : MessageBoxImage.Warning);
            ZatvoriFormu?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Greška pri unosu kursa:\n{ex.Message}",
                "UNOS KURSA",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private Dictionary<DateTime, decimal> UcitajKurseve(string deviza)
    {
        var rezultat = new Dictionary<DateTime, decimal>();
        var path = Path.Combine(_firmPath, "dev.dbf");
        if (!File.Exists(path))
            return rezultat;

        foreach (var rec in new SimpleDbfReader(path).Zapisi())
        {
            if (!rec.DajString("DEV").Trim().Equals(
                    deviza, StringComparison.OrdinalIgnoreCase))
                continue;

            var datum = rec.DajDate("DATDOK");
            if (datum.HasValue)
                rezultat[datum.Value.Date] = rec.DajDecimal("KURS");
        }

        return rezultat;
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    internal static RezultatObrade PrimeniKurs(
        IEnumerable<UniorRow> redovi,
        string deviza,
        IReadOnlyDictionary<DateTime, decimal> kursevi)
    {
        var oznaka = deviza.Trim().ToUpperInvariant();
        var deviznih = 0;
        var saKursom = 0;
        var bezKursa = 0;

        foreach (var row in redovi)
        {
            if (row.Devdug != 0)
            {
                deviznih++;
                var kurs = row.Datpdv.HasValue &&
                           kursevi.TryGetValue(row.Datpdv.Value.Date, out var vrednost)
                    ? vrednost
                    : 0m;

                row.Dev = oznaka;
                row.Devkurs = kurs;
                row.Osn18 = Math.Round(row.Devdug * kurs, 2);
                row.Ukprod = row.Osn18;

                if (kurs == 0)
                    bezKursa++;
                else
                    saKursom++;
            }
            else
            {
                row.Ukprod = Math.Round(row.Osn18 + row.Pdv18, 2);
            }
        }

        return new RezultatObrade(deviznih, saKursom, bezKursa);
    }

    internal sealed record RezultatObrade(
        int Deviznih,
        int SaKursom,
        int BezKursa);
}
