using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class IosiViewModel : ObservableObject
{
    private string _folderPath = "";

    [ObservableProperty] private ObservableCollection<IosiStavka> _stavke = [];
    [ObservableProperty] private string _naslov = "OBRAZAC IOSI";
    [ObservableProperty] private string _poruka = "";

    public IosiViewModel(string folderPath)
    {
        _folderPath = folderPath;
        UcitajPodatke(folderPath);
    }

    private void UcitajPodatke(string folderPath)
    {
        Stavke.Clear();
        if (string.IsNullOrWhiteSpace(folderPath)) { Poruka = "Nije izabrana firma."; return; }

        var dbfPath = Path.Combine(folderPath, "ldiosi.dbf");
        if (!File.Exists(dbfPath))
            dbfPath = Directory.GetFiles(folderPath, "ldiosi.dbf", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? dbfPath;
        if (!File.Exists(dbfPath)) { Poruka = "Fajl ldiosi.dbf nije pronađen."; return; }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);
            foreach (var z in zapisi)
            {
                Stavke.Add(new IosiStavka
                {
                    Mesec       = Dec(z, "MESEC"),
                    NazMes      = Str(z, "NAZMES"),
                    DatDok      = Dat(z, "DATDOK"),
                    UkZapos     = Dec(z, "UKZAPOS"),
                    BrojInval   = Dec(z, "BROJINVAL"),
                    UkInval     = Dec(z, "UKINVAL"),
                    BrojFinans  = Dec(z, "BROJFINANS"),
                    DinFinans   = Dec(z, "DINFINANS"),
                    BrojInvalU  = Dec(z, "BROJINVALU"),
                    DinUgov     = Dec(z, "DINUGOV"),
                    BrojUgov    = Str(z, "BROJUGOV"),
                    DatUgov     = Dat(z, "DATUGOV"),
                    BrojInvalP  = Dec(z, "BROJINVALP"),
                    DinPenal    = Dec(z, "DINPENAL"),
                    DatUplateP  = Dat(z, "DATUPLATEP"),
                });
            }
            Poruka = $"Učitano {zapisi.Count} stavki.";
        }
        catch (Exception ex) { Poruka = $"Greška: {ex.Message}"; }
    }

    private static string Str(Dictionary<string, object?> r, string k) => r.TryGetValue(k, out var v) && v is string s ? s : "";
    private static decimal Dec(Dictionary<string, object?> r, string k) => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;
    private static DateTime Dat(Dictionary<string, object?> r, string k) => r.TryGetValue(k, out var v) && v is DateTime d ? d : DateTime.MinValue;

    [RelayCommand]
    private void Osvezi() => UcitajPodatke(_folderPath);
}

public class IosiStavka
{
    public decimal  Mesec       { get; set; }
    public string   NazMes      { get; set; } = "";
    public DateTime DatDok      { get; set; }
    public decimal  UkZapos     { get; set; }
    public decimal  BrojInval   { get; set; }
    public decimal  UkInval     { get; set; }
    public decimal  BrojFinans  { get; set; }
    public decimal  DinFinans   { get; set; }
    public decimal  BrojInvalU  { get; set; }
    public decimal  DinUgov     { get; set; }
    public string   BrojUgov    { get; set; } = "";
    public DateTime DatUgov     { get; set; }
    public decimal  BrojInvalP  { get; set; }
    public decimal  DinPenal    { get; set; }
    public DateTime DatUplateP  { get; set; }
}
