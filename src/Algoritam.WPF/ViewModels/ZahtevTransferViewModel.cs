using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class ZahtevTransferViewModel : ObservableObject
{
    private string _folderPath = "";

    [ObservableProperty] private ObservableCollection<ZahtevStavka> _stavke = [];
    [ObservableProperty] private string _naslov = "ZAHTEV ZA TRANSFER";
    [ObservableProperty] private string _poruka = "";

    public ZahtevTransferViewModel(string folderPath)
    {
        _folderPath = folderPath;
        UcitajPodatke(folderPath);
    }

    private void UcitajPodatke(string folderPath)
    {
        Stavke.Clear();
        if (string.IsNullOrWhiteSpace(folderPath)) { Poruka = "Nije izabrana firma."; return; }

        var dbfPath = Path.Combine(folderPath, "ldzah.dbf");
        if (!File.Exists(dbfPath))
            dbfPath = Directory.GetFiles(folderPath, "ldzah.dbf", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? dbfPath;
        if (!File.Exists(dbfPath)) { Poruka = "Fajl ldzah.dbf nije pronađen."; return; }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);
            foreach (var z in zapisi)
            {
                Stavke.Add(new ZahtevStavka
                {
                    Kod     = Str(z, "KOD"),
                    Opis    = Str(z, "OPIS"),
                    Pre     = Dec(z, "PRE"),
                    Sada    = Dec(z, "SADA"),
                    Razlika = Dec(z, "RAZLIKA"),
                    RLini   = Str(z, "RLINI"),
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

public class ZahtevStavka
{
    public string  Kod     { get; set; } = "";
    public string  Opis    { get; set; } = "";
    public decimal Pre     { get; set; }
    public decimal Sada    { get; set; }
    public decimal Razlika { get; set; }
    public string  RLini   { get; set; } = "";
}
