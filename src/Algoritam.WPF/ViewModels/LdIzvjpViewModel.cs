using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// ViewModel za LDIZVJP — Izjava o isplaćenim zaradama.
/// Originalni FoxPro: DO FORM LDIZVJP, čita ldizvjp.dbf
/// </summary>
public partial class LdIzvjpViewModel : ObservableObject
{
    private readonly string _folderPath;

    [ObservableProperty] private ObservableCollection<LdIzvjpStavka> _stavke = [];
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private bool _ucitava = true;

    public string Naslov => "IZJAVA O ISPLAĆENIM ZARADAMA";

    public LdIzvjpViewModel(string folderPath)
    {
        _folderPath = folderPath;
        UcitajPodatke();
    }

    private void UcitajPodatke()
    {
        Ucitava = true;
        Stavke.Clear();

        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            Ucitava = false;
            return;
        }

        var dbfPath = Path.Combine(_folderPath, "ldizvjp.dbf");
        if (!File.Exists(dbfPath))
        {
            Poruka = $"Fajl ldizvjp.dbf nije pronađen u: {_folderPath}";
            Ucitava = false;
            return;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);
            foreach (var z in zapisi)
            {
                Stavke.Add(new LdIzvjpStavka
                {
                    Broj      = Int(z, "BROJ"),
                    Ime       = Str(z, "IME"),
                    Prezime   = Str(z, "PREZIME"),
                    MaticniBr = Str(z, "MATICNIBR"),
                    Rmesto    = Str(z, "RMESTO"),
                    Skosprem  = Str(z, "SKOSPREMA"),
                    Zarada    = Dec(z, "ZARADA"),
                    Uvecana   = Dec(z, "UVECANA"),
                    Varijabil = Dec(z, "VARIJABIL"),
                    Netold    = Dec(z, "NETOLD"),
                    Bruto     = Dec(z, "BRUTO"),
                    Mesec     = Int(z, "MESEC"),
                    Nazmes    = Str(z, "NAZMES"),
                    Isplata   = Int(z, "ISPLATA"),
                    Preneto   = Str(z, "PRENETO"),
                    IdBr      = Long(z, "IDBR"),
                });
            }
            Poruka = zapisi.Count == 0 ? "Nema podataka." : $"Učitano {zapisi.Count} stavki.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čitanju: {ex.Message}";
        }

        Ucitava = false;
    }

    [RelayCommand]
    private void Osvezi() => UcitajPodatke();

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s : string.Empty;
    private static int Int(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0;
        return v switch { decimal d => (int)d, int i => i, long l => (int)l, _ => 0 };
    }

    private static long Long(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0L;
        return v switch { decimal d => (long)d, long l => l, int i => i, _ => 0L };
    }
    private static decimal Dec(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;
}

public class LdIzvjpStavka
{
    public int     Broj      { get; set; }
    public string  Ime       { get; set; } = string.Empty;
    public string  Prezime   { get; set; } = string.Empty;
    public string  MaticniBr { get; set; } = string.Empty;
    public string  Rmesto    { get; set; } = string.Empty;
    public string  Skosprem  { get; set; } = string.Empty;
    public decimal Zarada    { get; set; }
    public decimal Uvecana   { get; set; }
    public decimal Varijabil { get; set; }
    public decimal Netold    { get; set; }
    public decimal Bruto     { get; set; }
    public int     Mesec     { get; set; }
    public string  Nazmes    { get; set; } = string.Empty;
    public int     Isplata   { get; set; }
    public string  Preneto   { get; set; } = string.Empty;
    public long    IdBr      { get; set; }
}
