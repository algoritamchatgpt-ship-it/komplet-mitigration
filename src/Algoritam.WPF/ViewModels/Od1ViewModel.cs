using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class Od1ViewModel : ObservableObject
{
    private string _folderPath = "";

    [ObservableProperty] private ObservableCollection<Od1Stavka> _stavke = [];
    [ObservableProperty] private string _naslov = "OBRAZAC OD-1";
    [ObservableProperty] private string _poruka = "";

    public Od1ViewModel(string folderPath)
    {
        _folderPath = folderPath;
        UcitajPodatke(folderPath);
    }

    private void UcitajPodatke(string folderPath)
    {
        Stavke.Clear();
        if (string.IsNullOrWhiteSpace(folderPath)) { Poruka = "Nije izabrana firma."; return; }

        var dbfPath = Path.Combine(folderPath, "ldod1n.dbf");
        if (!File.Exists(dbfPath))
            dbfPath = Directory.GetFiles(folderPath, "ldod1n.dbf", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? dbfPath;
        if (!File.Exists(dbfPath)) { Poruka = "Fajl ldod1n.dbf nije pronađen."; return; }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);
            foreach (var z in zapisi)
            {
                Stavke.Add(new Od1Stavka
                {
                    Kod       = Str(z, "KOD"),
                    Opis      = Str(z, "OPIS"),
                    Porodilje = Dec(z, "PORODILJE"),
                    Bolovanje = Dec(z, "BOLOVANJE"),
                    Invalidi  = Dec(z, "INVALIDI"),
                    RLini     = Str(z, "RLINI"),
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

public class Od1Stavka
{
    public string  Kod       { get; set; } = "";
    public string  Opis      { get; set; } = "";
    public decimal Porodilje { get; set; }
    public decimal Bolovanje { get; set; }
    public decimal Invalidi  { get; set; }
    public string  RLini     { get; set; } = "";
}
