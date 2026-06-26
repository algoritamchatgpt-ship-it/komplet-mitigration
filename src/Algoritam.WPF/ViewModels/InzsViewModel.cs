using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class InzsViewModel : ObservableObject
{
    private string _folderPath = "";

    [ObservableProperty] private ObservableCollection<InzsStavka> _stavke = [];
    [ObservableProperty] private string _naslov = "OBRAZAC INZS";
    [ObservableProperty] private string _poruka = "";

    public InzsViewModel(string folderPath)
    {
        _folderPath = folderPath;
        UcitajPodatke(folderPath);
    }

    private void UcitajPodatke(string folderPath)
    {
        Stavke.Clear();
        if (string.IsNullOrWhiteSpace(folderPath)) { Poruka = "Nije izabrana firma."; return; }

        var dbfPath = Path.Combine(folderPath, "ldinzs.dbf");
        if (!File.Exists(dbfPath))
            dbfPath = Directory.GetFiles(folderPath, "ldinzs.dbf", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? dbfPath;
        if (!File.Exists(dbfPath)) { Poruka = "Fajl ldinzs.dbf nije pronađen."; return; }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);
            foreach (var z in zapisi)
            {
                Stavke.Add(new InzsStavka
                {
                    Redni     = Dec(z, "REDNI"),
                    ImePrez   = Str(z, "IME_PREZ"),
                    MaticniBr = Str(z, "MATICNIBR"),
                    Bruto     = Dec(z, "BRUTO"),
                    DopPf     = Dec(z, "DOPPF"),
                    DopZf     = Dec(z, "DOPZF"),
                    DopNf     = Dec(z, "DOPNF"),
                    DopSocF   = Dec(z, "DOPSOCF"),
                    DopPfU    = Dec(z, "DOPPFU"),
                    DopZfU    = Dec(z, "DOPZFU"),
                    DopNfU    = Dec(z, "DOPNFU"),
                    DopPfR    = Dec(z, "DOPPFR"),
                    DopZfR    = Dec(z, "DOPZFR"),
                    DopNfR    = Dec(z, "DOPNFR"),
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

public class InzsStavka
{
    public decimal Redni     { get; set; }
    public string  ImePrez   { get; set; } = "";
    public string  MaticniBr { get; set; } = "";
    public decimal Bruto     { get; set; }
    public decimal DopPf     { get; set; }
    public decimal DopZf     { get; set; }
    public decimal DopNf     { get; set; }
    public decimal DopSocF   { get; set; }
    public decimal DopPfU    { get; set; }
    public decimal DopZfU    { get; set; }
    public decimal DopNfU    { get; set; }
    public decimal DopPfR    { get; set; }
    public decimal DopZfR    { get; set; }
    public decimal DopNfR    { get; set; }
}
