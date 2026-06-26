using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// PUTAR0 — šifarnik relacija/putarina (lookup).
/// </summary>
public partial class PutarRelacijeViewModel : ObservableObject
{
    private readonly string _folderPath;

    [ObservableProperty] private ObservableCollection<PutarRelacija> _relacije = [];
    [ObservableProperty] private PutarRelacija? _selektovana;
    [ObservableProperty] private string _pretragaTekst = string.Empty;
    [ObservableProperty] private string _poruka = string.Empty;

    public bool Izabrana { get; private set; }
    public event Action? ZatvaranjeZahtevano;

    public PutarRelacijeViewModel(string folderPath)
    {
        _folderPath = folderPath;
        Ucitaj();
    }

    [RelayCommand]
    private void Potvrdi()
    {
        if (Selektovana == null) { Poruka = "Selektujte relaciju."; return; }
        Izabrana = true;
        ZatvaranjeZahtevano?.Invoke();
    }

    [RelayCommand]
    private void Trazi()
    {
        if (string.IsNullOrWhiteSpace(PretragaTekst)) return;
        var upit = PretragaTekst.Trim();
        var found = Relacije.FirstOrDefault(r =>
            r.Relacija.Contains(upit, StringComparison.OrdinalIgnoreCase) ||
            r.Sifrel.Contains(upit, StringComparison.OrdinalIgnoreCase));
        if (found != null) { Selektovana = found; Poruka = $"Pronađeno: {found.Relacija}"; }
        else Poruka = "Nije pronađena relacija.";
    }

    [RelayCommand]
    private void DodajNovu()
    {
        var max = Relacije.Count == 0 ? 0 : Relacije.Max(r =>
        {
            if (int.TryParse(r.Sifrel.Trim(), out var n)) return n;
            return 0;
        });
        var nova = new PutarRelacija { Sifrel = (max + 1).ToString("000000"), Relacija = string.Empty, Iznos = 0m };
        Relacije.Add(nova);
        Selektovana = nova;
        Poruka = "Nova relacija — unesite naziv i iznos pa kliknite SAČUVAJ.";
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        var path = Path.Combine(_folderPath, "putar0.dbf");
        if (!File.Exists(path)) { Poruka = "putar0.dbf nije pronađen."; return; }
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            var rows = Relacije.Select(r => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["SIFREL"]   = r.Sifrel,
                ["RELACIJA"] = r.Relacija,
                ["IZNOS"]    = r.Iznos,
                ["CPRENETO"] = " ",
            }).ToList<Dictionary<string, object?>>();
            DbfTableWriter.WriteTable(path, schema, rows,
                static (row, field) => row.TryGetValue(field, out var v) ? v : null);
            Poruka = $"Sačuvano {Relacije.Count} relacija.";
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; }
    }

    [RelayCommand]
    private void Izlaz() => ZatvaranjeZahtevano?.Invoke();

    private void Ucitaj()
    {
        Relacije.Clear();
        var path = Path.Combine(_folderPath, "putar0.dbf");
        if (!File.Exists(path)) { Poruka = "putar0.dbf nije pronađen."; return; }
        try
        {
            foreach (var z in DbfReader.CitajSveZapise(path))
            {
                Relacije.Add(new PutarRelacija
                {
                    Sifrel   = Str(z, "SIFREL"),
                    Relacija = Str(z, "RELACIJA"),
                    Iznos    = Dec(z, "IZNOS"),
                });
            }
            Selektovana = Relacije.FirstOrDefault();
            Poruka = $"Učitano {Relacije.Count} relacija.";
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; }
    }

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;
    private static decimal Dec(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;
}

public partial class PutarRelacija : ObservableObject
{
    [ObservableProperty] private string _sifrel = string.Empty;
    [ObservableProperty] private string _relacija = string.Empty;
    [ObservableProperty] private decimal _iznos;
}
