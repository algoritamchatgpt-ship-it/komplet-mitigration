using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class OpnrViewModel : ObservableObject
{
    private string _folderPath = "";

    [ObservableProperty] private ObservableCollection<OpnrStavka> _stavke = [];
    [ObservableProperty] private string _naslov = "OBRAZAC OPNR";
    [ObservableProperty] private string _poruka = "";

    public OpnrViewModel(string folderPath)
    {
        _folderPath = folderPath;
        UcitajPodatke(folderPath);
    }

    private void UcitajPodatke(string folderPath)
    {
        Stavke.Clear();
        if (string.IsNullOrWhiteSpace(folderPath)) { Poruka = "Nije izabrana firma."; return; }

        var dbfPath = Path.Combine(folderPath, "ldopnr.dbf");
        if (!File.Exists(dbfPath))
            dbfPath = Directory.GetFiles(folderPath, "ldopnr.dbf", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? dbfPath;
        if (!File.Exists(dbfPath)) { Poruka = "Fajl ldopnr.dbf nije pronađen."; return; }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);
            foreach (var z in zapisi)
            {
                Stavke.Add(new OpnrStavka
                {
                    Br         = Str(z, "BR"),
                    ImePrez    = Str(z, "IME_PREZ"),
                    MaticniBr  = Str(z, "MATICNIBR"),
                    Adresa     = Str(z, "ADRESA"),
                    DatNezap   = Dat(z, "DATNEZAP"),
                    DatUgovor  = Dat(z, "DATUGOVOR"),
                    DatPri     = Dat(z, "DATPRI"),
                    DatZasniv  = Dat(z, "DATZASNIV"),
                    PorOlaks   = Str(z, "POROLAKS"),
                    SImePrez   = Str(z, "SIME_PREZ"),
                    SMaticni   = Str(z, "SMATICNI"),
                    DatGub     = Dat(z, "DATGUB"),
                    Razlog     = Str(z, "RAZLOG"),
                    DatUplPor  = Dat(z, "DATUPLPOR"),
                    Iznos      = Dec(z, "IZNOS"),
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

public class OpnrStavka
{
    public string   Br         { get; set; } = "";
    public string   ImePrez    { get; set; } = "";
    public string   MaticniBr  { get; set; } = "";
    public string   Adresa     { get; set; } = "";
    public DateTime DatNezap   { get; set; }
    public DateTime DatUgovor  { get; set; }
    public DateTime DatPri     { get; set; }
    public DateTime DatZasniv  { get; set; }
    public string   PorOlaks   { get; set; } = "";
    public string   SImePrez   { get; set; } = "";
    public string   SMaticni   { get; set; } = "";
    public DateTime DatGub     { get; set; }
    public string   Razlog     { get; set; } = "";
    public DateTime DatUplPor  { get; set; }
    public decimal  Iznos      { get; set; }
}
