using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class ZspViewModel : ObservableObject
{
    private string _folderPath = "";

    [ObservableProperty] private ObservableCollection<ZspStavka> _stavke = [];
    [ObservableProperty] private string _naslov = "OBRAZAC ZSP";
    [ObservableProperty] private string _poruka = "";

    public ZspViewModel(string folderPath)
    {
        _folderPath = folderPath;
        UcitajPodatke(folderPath);
    }

    private void UcitajPodatke(string folderPath)
    {
        Stavke.Clear();
        if (string.IsNullOrWhiteSpace(folderPath)) { Poruka = "Nije izabrana firma."; return; }

        var dbfPath = Path.Combine(folderPath, "ldzsp.dbf");
        if (!File.Exists(dbfPath))
            dbfPath = Directory.GetFiles(folderPath, "ldzsp.dbf", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? dbfPath;
        if (!File.Exists(dbfPath)) { Poruka = "Fajl ldzsp.dbf nije pronađen."; return; }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);
            foreach (var z in zapisi)
            {
                Stavke.Add(new ZspStavka
                {
                    Redni     = Dec(z, "REDNI"),
                    ImePrez   = Str(z, "IME_PREZ"),
                    MaticniBr = Str(z, "MATICNIBR"),
                    PorUmanj  = Dec(z, "PORUMANJ"),
                    DatZapos  = Dat(z, "DATZAPOS"),
                    Bruto     = Dec(z, "BRUTO"),
                    PorDoh    = Dec(z, "PORDOH"),
                    PorezU    = Dec(z, "POREZU"),
                    PorezUpl  = Dec(z, "POREZUPL"),
                    PorezSub  = Dec(z, "POREZSUB"),
                    DatOtkaz  = Dat(z, "DATOTKAZ"),
                    PorDug    = Dec(z, "PORDUG"),
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

public class ZspStavka
{
    public decimal  Redni     { get; set; }
    public string   ImePrez   { get; set; } = "";
    public string   MaticniBr { get; set; } = "";
    public decimal  PorUmanj  { get; set; }
    public DateTime DatZapos  { get; set; }
    public decimal  Bruto     { get; set; }
    public decimal  PorDoh    { get; set; }
    public decimal  PorezU    { get; set; }
    public decimal  PorezUpl  { get; set; }
    public decimal  PorezSub  { get; set; }
    public DateTime DatOtkaz  { get; set; }
    public decimal  PorDug    { get; set; }
}
