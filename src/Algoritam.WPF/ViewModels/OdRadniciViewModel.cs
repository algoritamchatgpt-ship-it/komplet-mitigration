using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// ViewModel za obrazac OD Radnici — čita ldodn.dbf iz foldera izabrane firme.
/// Originalni FoxPro poziv: DO FORM LDODN
/// </summary>
public partial class OdRadniciViewModel : ObservableObject
{
    private string _folderPath = "";

    [ObservableProperty] private ObservableCollection<OdRadniciStavka> _stavke = [];
    [ObservableProperty] private string _naslov = "OD RADNICI";
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private bool _ucitava = true;

    public OdRadniciViewModel(string folderPath)
    {
        _folderPath = folderPath;
        UcitajPodatke(folderPath);
    }

    private void UcitajPodatke(string folderPath)
    {
        Ucitava = true;
        Stavke.Clear();

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            Poruka = "Nije izabrana firma.";
            Ucitava = false;
            return;
        }

        var dbfPath = Path.Combine(folderPath, "ldodn.dbf");

        // Pokušaj i malim slovima (Windows je case-insensitive, ali za svaki slučaj)
        if (!File.Exists(dbfPath))
            dbfPath = Directory.GetFiles(folderPath, "ldodn.dbf", SearchOption.TopDirectoryOnly)
                               .FirstOrDefault() ?? dbfPath;

        if (!File.Exists(dbfPath))
        {
            Poruka = $"Fajl ldodn.dbf nije pronađen u: {folderPath}";
            Ucitava = false;
            return;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);

            if (zapisi.Count == 0)
            {
                Poruka = "Nema unetih podataka za ovaj obrazac.";
            }
            else
            {
                foreach (var z in zapisi)
                {
                    Stavke.Add(new OdRadniciStavka
                    {
                        Kod      = Str(z, "KOD"),
                        Opis     = Str(z, "OPIS"),
                        Aop      = Str(z, "AOP"),
                        Sk1      = Dec(z, "SK1"),
                        Sk2      = Dec(z, "SK2"),
                        Sk3      = Dec(z, "SK3"),
                        Sk4      = Dec(z, "SK4"),
                        ZiroRac  = Str(z, "ZIRORAC"),
                    });
                }
                Poruka = $"Učitano {zapisi.Count} stavki.";
            }
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čitanju: {ex.Message}";
        }

        Ucitava = false;
    }

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s : string.Empty;

    private static decimal Dec(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;

    [RelayCommand]
    private void Osvezi() => UcitajPodatke(_folderPath);
}

public class OdRadniciStavka
{
    public string  Kod     { get; set; } = string.Empty;
    public string  Opis    { get; set; } = string.Empty;
    public string  Aop     { get; set; } = string.Empty;
    public decimal Sk1     { get; set; }
    public decimal Sk2     { get; set; }
    public decimal Sk3     { get; set; }
    public decimal Sk4     { get; set; }
    public string  ZiroRac { get; set; } = string.Empty;
}
