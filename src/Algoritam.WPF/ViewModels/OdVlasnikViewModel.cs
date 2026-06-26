using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class OdVlasnikViewModel : ObservableObject
{
    private string _folderPath = "";

    [ObservableProperty] private ObservableCollection<OdVlasnikStavka> _stavke = [];
    [ObservableProperty] private string _naslov = "OBRAZAC OD — VLASNIK";
    [ObservableProperty] private string _poruka = "";

    public OdVlasnikViewModel(string folderPath)
    {
        _folderPath = folderPath;
        UcitajPodatke(folderPath);
    }

    private void UcitajPodatke(string folderPath)
    {
        Stavke.Clear();
        if (string.IsNullOrWhiteSpace(folderPath)) { Poruka = "Nije izabrana firma."; return; }

        var dbfPath = Path.Combine(folderPath, "ldod.dbf");
        if (!File.Exists(dbfPath))
            dbfPath = Directory.GetFiles(folderPath, "ldod.dbf", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? dbfPath;
        if (!File.Exists(dbfPath)) { Poruka = "Fajl ldod.dbf nije pronađen."; return; }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);
            foreach (var z in zapisi)
            {
                Stavke.Add(new OdVlasnikStavka
                {
                    Kod   = Str(z, "KOD"),
                    Opis  = Str(z, "OPIS"),
                    Aop   = Str(z, "AOP"),
                    Sk1   = Dec(z, "SK1"),
                    Sk2   = Dec(z, "SK2"),
                    Sk3   = Dec(z, "SK3"),
                    Sk4   = Dec(z, "SK4"),
                    Sk5   = Dec(z, "SK5"),
                    Sk6   = Dec(z, "SK6"),
                    Sk7   = Dec(z, "SK7"),
                    Sk8   = Dec(z, "SK8"),
                    Sk0   = Dec(z, "SK0"),
                    RLini = Str(z, "RLINI"),
                });
            }
            Poruka = $"Učitano {zapisi.Count} stavki.";
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; }
    }

    private static string Str(Dictionary<string, object?> r, string k) => r.TryGetValue(k, out var v) && v is string s ? s : "";
    private static decimal Dec(Dictionary<string, object?> r, string k) => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;

    [RelayCommand]
    private void Osvezi() => UcitajPodatke(_folderPath);
}

public class OdVlasnikStavka
{
    public string  Kod   { get; set; } = "";
    public string  Opis  { get; set; } = "";
    public string  Aop   { get; set; } = "";
    public decimal Sk1   { get; set; }
    public decimal Sk2   { get; set; }
    public decimal Sk3   { get; set; }
    public decimal Sk4   { get; set; }
    public decimal Sk5   { get; set; }
    public decimal Sk6   { get; set; }
    public decimal Sk7   { get; set; }
    public decimal Sk8   { get; set; }
    public decimal Sk0   { get; set; }
    public string  RLini { get; set; } = "";
}
