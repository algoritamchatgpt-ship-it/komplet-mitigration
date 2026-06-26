using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class BlIzvestajViewModel : ObservableObject
{
    private readonly string _folderPath;
    private List<Dictionary<string, object?>> _sviZapisi = [];

    public string Naslov => "IZVEŠTAJ BLAGAJNE — PROMET PO KONTIMA";

    [ObservableProperty] private ObservableCollection<BlIzvestajStavka> _stavke = [];
    [ObservableProperty] private string _statusPoruka = "";
    [ObservableProperty] private bool _ucitava = true;
    [ObservableProperty] private DateTime _datumOd = new(DateTime.Today.Year, 1, 1);
    [ObservableProperty] private DateTime _datumDo = DateTime.Today;
    [ObservableProperty] private decimal _ukupnoDuguje;
    [ObservableProperty] private decimal _ukupnoPotrazuje;
    [ObservableProperty] private decimal _saldo;

    public event Action? ZatvaranjeZahtevano;

    public BlIzvestajViewModel(string folderPath)
    {
        _folderPath = folderPath;
        _ = UcitajAsync();
    }

    private async Task UcitajAsync()
    {
        Ucitava = true;
        StatusPoruka = "Učitavanje...";
        try
        {
            var dbfPath = NadjiDbf(_folderPath, "kas.dbf");
            if (dbfPath is null)
            {
                StatusPoruka = "kas.dbf nije pronađen.";
                return;
            }
            _sviZapisi = await Task.Run(() => DbfReader.CitajSveZapise(dbfPath));
            PrimenjiFilter();
            StatusPoruka = $"Ukupno zapisa: {_sviZapisi.Count}";
        }
        catch (Exception ex)
        {
            StatusPoruka = $"Greška: {ex.Message}";
        }
        finally
        {
            Ucitava = false;
        }
    }

    partial void OnDatumOdChanged(DateTime value) => PrimenjiFilter();
    partial void OnDatumDoChanged(DateTime value) => PrimenjiFilter();

    private void PrimenjiFilter()
    {
        var od  = DatumOd.Date;
        var do_ = DatumDo.Date;

        var filtrirani = _sviZapisi.Where(n =>
        {
            if (!n.TryGetValue("DATDOK", out var dv)) return true;
            var d = dv is DateTime dt ? dt :
                    DateTime.TryParse(dv?.ToString(), out var dt2) ? dt2 : (DateTime?)null;
            return d == null || (d.Value.Date >= od && d.Value.Date <= do_);
        });

        var grupisano = filtrirani
            .GroupBy(n => Str(n, "KONTO"))
            .OrderBy(g => g.Key)
            .Select(g => new BlIzvestajStavka
            {
                Konto           = g.Key,
                UkupnoDuguje    = g.Sum(n => Dec(n, "DUG")),
                UkupnoPotrazuje = g.Sum(n => Dec(n, "POT")),
                BrojStavki      = g.Count(),
            })
            .ToList();

        Stavke          = new ObservableCollection<BlIzvestajStavka>(grupisano);
        UkupnoDuguje    = grupisano.Sum(s => s.UkupnoDuguje);
        UkupnoPotrazuje = grupisano.Sum(s => s.UkupnoPotrazuje);
        Saldo           = UkupnoDuguje - UkupnoPotrazuje;
    }

    [RelayCommand]
    private void Osvezi() => _ = UcitajAsync();

    [RelayCommand]
    private void OcistiFilter()
    {
        DatumOd = new DateTime(DateTime.Today.Year, 1, 1);
        DatumDo = DateTime.Today;
    }

    [RelayCommand]
    private void Zatvori() => ZatvaranjeZahtevano?.Invoke();

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;

    private static decimal Dec(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0m;
        return v switch { decimal d => d, double db => (decimal)db, int i => i, long l => l, _ => 0m };
    }

    private static string? NadjiDbf(string folderPath, string fileName)
    {
        foreach (var dir in new[] { folderPath,
            Path.Combine(folderPath, "data00"),
            Path.Combine(folderPath, "01"),
            Path.Combine(folderPath, "..") })
        {
            if (!Directory.Exists(dir)) continue;
            var f = Path.Combine(dir, fileName);
            if (File.Exists(f)) return f;
            try
            {
                var ci = Directory.GetFiles(dir, "*.dbf", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(x => Path.GetFileName(x).Equals(fileName, StringComparison.OrdinalIgnoreCase));
                if (ci is not null) return ci;
            }
            catch { }
        }
        return null;
    }
}

public class BlIzvestajStavka
{
    public string  Konto           { get; set; } = string.Empty;
    public decimal UkupnoDuguje    { get; set; }
    public decimal UkupnoPotrazuje { get; set; }
    public decimal Saldo           => UkupnoDuguje - UkupnoPotrazuje;
    public int     BrojStavki      { get; set; }
}
