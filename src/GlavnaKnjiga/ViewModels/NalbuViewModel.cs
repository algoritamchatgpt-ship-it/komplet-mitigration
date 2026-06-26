using Algoritam.Core.Services.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows;

namespace GlavnaKnjiga.ViewModels;

public partial class NalbuViewModel : ObservableObject
{
    private readonly string _firmPath;
    private readonly int _godina;

    public event Action? ZatvoriFormu;

    // Konto prihoda (3 rows)
    [ObservableProperty] private string _kontoP1 = "6";
    [ObservableProperty] private string _kontoP2 = string.Empty;
    [ObservableProperty] private string _kontoP3 = string.Empty;
    // Konto rashoda (3 rows)
    [ObservableProperty] private string _kontoR1 = "5";
    [ObservableProperty] private string _kontoR2 = string.Empty;
    [ObservableProperty] private string _kontoR3 = string.Empty;
    // Date range
    [ObservableProperty] private DateTime? _dat0;
    [ObservableProperty] private DateTime? _dat1;
    // Filters
    [ObservableProperty] private string _dok    = string.Empty;
    [ObservableProperty] private string _mp     = string.Empty;
    [ObservableProperty] private decimal _mtr;
    [ObservableProperty] private string _kurs   = string.Empty;
    [ObservableProperty] private string _brnal1 = "999991";
    [ObservableProperty] private string _brnal2 = "999992";
    // Mode flags
    [ObservableProperty] private string _saldoObj = "N";
    [ObservableProperty] private string _dodPrih  = "N";
    [ObservableProperty] private string _saldoMtr = "N";

    public NalbuViewModel(string firmPath, int godina)
    {
        _firmPath = firmPath;
        _godina   = godina;
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
            MessageBox.Show("Unesite početni i zadnji datum.", "BILANS USPEHA",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var nalPath = Path.Combine(_firmPath, "nal.dbf");
        if (!File.Exists(nalPath))
        {
            MessageBox.Show("nal.dbf ne postoji.", "BILANS USPEHA",
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

            var brnal1   = Brnal1.Trim();
            var brnal2   = Brnal2.Trim();
            var mp       = Mp.Trim();
            var mtr      = Mtr;
            var useKurs  = Kurs.Trim().Equals("D", StringComparison.OrdinalIgnoreCase);
            var useDodPrih  = DodPrih.Trim().Equals("D", StringComparison.OrdinalIgnoreCase);
            var useSaldoObj = SaldoObj.Trim().Equals("D", StringComparison.OrdinalIgnoreCase);
            var useSaldoMtr = SaldoMtr.Trim().Equals("D", StringComparison.OrdinalIgnoreCase);

            var rows = new SimpleDbfReader(nalPath).Zapisi()
                .Select(Nalp2ViewModel.NalpRowFromRecord)
                .Where(r =>
                {
                    var k = r.Konto.Trim();
                    if (!kontoPs.Any(p => k.StartsWith(p, StringComparison.OrdinalIgnoreCase)) &&
                        !kontoRs.Any(p => k.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                        return false;
                    if (r.Datdok < Dat0 || r.Datdok > Dat1) return false;
                    if (!string.IsNullOrEmpty(mp) && r.Mp.ToString().Trim() != mp) return false;
                    if (mtr != 0 && r.Mtr != mtr) return false;
                    if (!string.IsNullOrEmpty(brnal1) && r.Brnal.Trim() == brnal1) return false;
                    if (!string.IsNullOrEmpty(brnal2) && r.Brnal.Trim() == brnal2) return false;
                    return true;
                })
                .ToList();

            if (useKurs)
            {
                foreach (var r in rows)
                {
                    r.Dug = Math.Round(r.Kursdug, 2);
                    r.Pot = Math.Round(r.Kurspot, 2);
                }
            }

            if (useDodPrih)
            {
                foreach (var r in rows)
                {
                    r.Dug += r.Doddug;
                    r.Pot += r.Dodpot;
                }
            }

            IEnumerable<(string Kl, decimal Dug, decimal Pot)> grouped;
            if (useSaldoMtr)
                grouped = rows.GroupBy(r => r.Konto.Trim() + "|MTR=" + r.Mtr)
                              .Select(g => (g.Key, g.Sum(r => r.Dug), g.Sum(r => r.Pot)));
            else if (useSaldoObj)
                grouped = rows.GroupBy(r => r.Konto.Trim() + "|DOK=" + r.Dok.Trim())
                              .Select(g => (g.Key, g.Sum(r => r.Dug), g.Sum(r => r.Pot)));
            else
                grouped = rows.GroupBy(r => r.Konto.Trim())
                              .Select(g => (g.Key, g.Sum(r => r.Dug), g.Sum(r => r.Pot)));

            var list = grouped.OrderBy(x => x.Kl).ToList();
            var ukDug = list.Sum(x => x.Dug);
            var ukPot = list.Sum(x => x.Pot);

            MessageBox.Show(
                $"BILANS USPEHA\n\n" +
                $"Broj redova: {list.Count}\n" +
                $"Ukupno DUG: {ukDug:N2}\n" +
                $"Ukupno POT: {ukPot:N2}\n" +
                $"Saldo: {ukDug - ukPot:N2}\n\n" +
                $"[Štampa — NALBU0.FRX nije implementirana]",
                "BILANS USPEHA — PREGLED",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri pregledu:\n{ex.Message}", "BILANS USPEHA",
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
                ["DAT0"] = Dat0,
                ["DAT1"] = Dat1,
            };
            DbfTableWriter.WriteTable(path, schema, new List<Dictionary<string, object?>> { row },
                (r, f) => r.TryGetValue(f, out var v) ? v : null);
        }
        catch { }
    }
}
