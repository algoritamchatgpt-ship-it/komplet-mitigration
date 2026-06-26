using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class ZsdViewModel : ObservableObject
{
    private string _folderPath = "";

    [ObservableProperty] private ObservableCollection<ZsdStavka> _stavke = [];
    [ObservableProperty] private string _naslov = "OBRAZAC ZSD";
    [ObservableProperty] private string _poruka = "";

    public ZsdViewModel(string folderPath)
    {
        _folderPath = folderPath;
        UcitajPodatke(folderPath);
    }

    private void UcitajPodatke(string folderPath)
    {
        Stavke.Clear();
        if (string.IsNullOrWhiteSpace(folderPath)) { Poruka = "Nije izabrana firma."; return; }

        var dbfPath = Path.Combine(folderPath, "ldzsd.dbf");
        if (!File.Exists(dbfPath))
            dbfPath = Directory.GetFiles(folderPath, "ldzsd.dbf", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? dbfPath;
        if (!File.Exists(dbfPath)) { Poruka = "Fajl ldzsd.dbf nije pronađen."; return; }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);
            foreach (var z in zapisi)
            {
                Stavke.Add(new ZsdStavka
                {
                    Redni     = Dec(z, "REDNI"),
                    ImePrez   = Str(z, "IME_PREZ"),
                    MaticniBr = Str(z, "MATICNIBR"),
                    DatZapos  = Dat(z, "DATZAPOS"),
                    Bruto     = Dec(z, "BRUTO"),
                    DopPf     = Dec(z, "DOPPF"),
                    DopPfR    = Dec(z, "DOPPFR"),
                    DopPUk    = Dec(z, "DOPPUK"),
                    DatOtkaz  = Dat(z, "DATOTKAZ"),
                    DopDug    = Dec(z, "DOPDUG"),
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

public class ZsdStavka
{
    public decimal  Redni     { get; set; }
    public string   ImePrez   { get; set; } = "";
    public string   MaticniBr { get; set; } = "";
    public DateTime DatZapos  { get; set; }
    public decimal  Bruto     { get; set; }
    public decimal  DopPf     { get; set; }
    public decimal  DopPfR    { get; set; }
    public decimal  DopPUk    { get; set; }
    public DateTime DatOtkaz  { get; set; }
    public decimal  DopDug    { get; set; }
}
