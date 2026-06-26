using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class Opj1ViewModel : ObservableObject
{
    private string _folderPath = "";

    [ObservableProperty] private ObservableCollection<Opj1Stavka> _stavke = [];
    [ObservableProperty] private string _naslov = "OBRAZAC OPJ-1 — OPJ-8";
    [ObservableProperty] private string _poruka = "";

    public Opj1ViewModel(string folderPath)
    {
        _folderPath = folderPath;
        UcitajPodatke(folderPath);
    }

    private void UcitajPodatke(string folderPath)
    {
        Stavke.Clear();
        if (string.IsNullOrWhiteSpace(folderPath)) { Poruka = "Nije izabrana firma."; return; }

        var dbfPath = Path.Combine(folderPath, "ldopj1n.dbf");
        if (!File.Exists(dbfPath))
            dbfPath = Directory.GetFiles(folderPath, "ldopj1n.dbf", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? dbfPath;
        if (!File.Exists(dbfPath)) { Poruka = "Fajl ldopj1n.dbf nije pronađen."; return; }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);
            foreach (var z in zapisi)
            {
                Stavke.Add(new Opj1Stavka
                {
                    Broj       = Dec(z, "BROJ"),
                    SifraPrih  = Str(z, "SIFRAPRIH"),
                    RedBr      = Dec(z, "REDBR"),
                    SifOpis    = Str(z, "SIFOPIS"),
                    DatDok     = Dat(z, "DATDOK"),
                    Radnika    = Dec(z, "RADNIKA"),
                    Kol        = Dec(z, "KOL"),
                    Neoporez   = Dec(z, "NEOPOREZ"),
                    Isplaceno  = Dec(z, "ISPLACENO"),
                    ZaIsplatu  = Dec(z, "ZAISPLATU"),
                    Svega      = Dec(z, "SVEGA"),
                    BrutoOpor  = Dec(z, "BRUTOOPOR"),
                    Porez      = Dec(z, "POREZ"),
                    Opis1      = Str(z, "OPIS1"),
                    Opis2      = Str(z, "OPIS2"),
                    DatIspl    = Dat(z, "DATISPL"),
                    Mesec      = Dec(z, "MESEC"),
                    NazMes     = Str(z, "NAZMES"),
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

public class Opj1Stavka
{
    public decimal  Broj      { get; set; }
    public string   SifraPrih { get; set; } = "";
    public decimal  RedBr     { get; set; }
    public string   SifOpis   { get; set; } = "";
    public DateTime DatDok    { get; set; }
    public decimal  Radnika   { get; set; }
    public decimal  Kol       { get; set; }
    public decimal  Neoporez  { get; set; }
    public decimal  Isplaceno { get; set; }
    public decimal  ZaIsplatu { get; set; }
    public decimal  Svega     { get; set; }
    public decimal  BrutoOpor { get; set; }
    public decimal  Porez     { get; set; }
    public string   Opis1     { get; set; } = "";
    public string   Opis2     { get; set; } = "";
    public DateTime DatIspl   { get; set; }
    public decimal  Mesec     { get; set; }
    public string   NazMes    { get; set; } = "";
}
