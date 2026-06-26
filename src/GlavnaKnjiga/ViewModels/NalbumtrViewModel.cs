using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalbumtrViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly int _nacin; // 1 = bilans po MTR, 2 = saldo po MTR

    public event Action? ZatvoriFormu;

    public string Naslov => _nacin == 1
        ? "BILANS USPEHA PO MESTIMA TROŠKOVA"
        : "SALDO PO MESTIMA TROŠKOVA";

    [ObservableProperty] private string _kontoP1 = "6";
    [ObservableProperty] private string _kontoP2 = string.Empty;
    [ObservableProperty] private string _kontoP3 = string.Empty;
    [ObservableProperty] private string _kontoR1 = "5";
    [ObservableProperty] private string _kontoR2 = string.Empty;
    [ObservableProperty] private string _kontoR3 = string.Empty;
    [ObservableProperty] private DateTime? _dat0;
    [ObservableProperty] private DateTime? _dat1;
    [ObservableProperty] private string _dodPrih = "N";

    public NalbumtrViewModel(string firmPath, int godina, int nacin)
    {
        _firmPath = firmPath;
        _nacin    = nacin;
        Dat0 = new DateTime(godina, 1, 1);
        Dat1 = new DateTime(godina, 12, 31);

        var datumiPath = Path.Combine(firmPath, "datumi.dbf");
        if (File.Exists(datumiPath))
        {
            try
            {
                var rec = new SimpleDbfReader(datumiPath).Zapisi().FirstOrDefault();
                if (rec != null)
                {
                    var d0 = rec.DajDate("DAT0");
                    var d1 = rec.DajDate("DAT1");
                    if (d0 != null) Dat0 = d0;
                    if (d1 != null) Dat1 = d1;
                }
            }
            catch { }
        }
    }

    [RelayCommand]
    private void Pregled()
    {
        if (Dat0 == null || Dat1 == null)
        {
            MessageBox.Show("Unesite početni i zadnji datum.", Naslov,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var nalPath = Path.Combine(_firmPath, "nal.dbf");
        if (!File.Exists(nalPath))
        {
            MessageBox.Show("nal.dbf ne postoji.", Naslov,
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        SalveDatume();

        try
        {
            var kontoPs = new[] { KontoP1.Trim(), KontoP2.Trim(), KontoP3.Trim() }
                .Where(k => k.Length > 0).ToArray();
            var kontoRs = new[] { KontoR1.Trim(), KontoR2.Trim(), KontoR3.Trim() }
                .Where(k => k.Length > 0).ToArray();
            var useDodPrih = DodPrih.Trim().Equals("D", StringComparison.OrdinalIgnoreCase);

            var rows = new SimpleDbfReader(nalPath).Zapisi()
                .Select(Nalp2ViewModel.NalpRowFromRecord)
                .Where(r =>
                {
                    var k = r.Konto.Trim();
                    if (!kontoPs.Any(p => k.StartsWith(p, StringComparison.OrdinalIgnoreCase)) &&
                        !kontoRs.Any(p => k.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                        return false;
                    return r.Datdok >= Dat0 && r.Datdok <= Dat1;
                })
                .ToList();

            if (useDodPrih)
            {
                foreach (var r in rows)
                {
                    r.Dug += r.Doddug;
                    r.Pot += r.Dodpot;
                }
            }

            // Group by MTR + KONTO
            var grouped = rows
                .GroupBy(r => ((int)r.Mtr, r.Konto.Trim()))
                .Select(g => new { Mtr = g.Key.Item1, Konto = g.Key.Item2, Dug = g.Sum(r => r.Dug), Pot = g.Sum(r => r.Pot) })
                .OrderBy(x => x.Mtr).ThenBy(x => x.Konto)
                .ToList();

            var ukDug = grouped.Sum(x => x.Dug);
            var ukPot = grouped.Sum(x => x.Pot);

            var rezime = _nacin == 2
                ? grouped.GroupBy(x => x.Mtr)
                         .Select(g => $"MTR {g.Key,3}: Dug={g.Sum(x => x.Dug):N2}  Pot={g.Sum(x => x.Pot):N2}")
                         .Take(10)
                : grouped.Take(10).Select(x => $"{x.Mtr,3} {x.Konto,-12} Dug={x.Dug:N2} Pot={x.Pot:N2}");

            MessageBox.Show(
                $"{Naslov}\n\n" +
                $"Broj redova: {grouped.Count}\n" +
                $"Ukupno DUG: {ukDug:N2}   POT: {ukPot:N2}\n\n" +
                string.Join("\n", rezime) + (grouped.Count > 10 ? "\n..." : "") +
                $"\n\n[Štampa — NALBU0MTR.FRX nije implementirana]",
                Naslov,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri pregledu:\n{ex.Message}", Naslov,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Izlaz() => ZatvoriFormu?.Invoke();

    private void SalveDatume()
    {
        var path = Path.Combine(_firmPath, "datumi.dbf");
        if (!File.Exists(path)) return;
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["DAT0"] = Dat0, ["DAT1"] = Dat1,
            };
            DbfTableWriter.WriteTable(path, schema, new List<Dictionary<string, object?>> { row },
                (r, f) => r.TryGetValue(f, out var v) ? v : null);
        }
        catch { }
    }
}
