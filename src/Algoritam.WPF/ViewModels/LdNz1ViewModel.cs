using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Fox forma LDNZ1 - OBRAZAC NZ-1 NAKNADA ZA PORODILJE.
/// </summary>
public partial class LdNz1ViewModel : ObservableObject
{
    private readonly string _folderPath;

    [ObservableProperty] private ObservableCollection<LdNz1Stavka> _stavke = [];
    [ObservableProperty] private string _naslov = "OBRAZAC NZ-1 NAKNADA ZA PORODILJE";
    [ObservableProperty] private string _poruka = string.Empty;

    public event Action? ZatvaranjeZatrazeno;

    public LdNz1ViewModel(AppState appState)
    {
        _folderPath = appState.AktivnaFirma?.FolderPath ?? string.Empty;
        UcitajPostojecuTabelu();
    }

    [RelayCommand]
    private void Preuzimanje()
    {
        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        var ldPath = PronadjiDbf(_folderPath, "ld00.dbf", "ld.dbf");
        var ldradPath = PronadjiDbf(_folderPath, "ldrad.dbf");
        if (ldPath is null || ldradPath is null)
        {
            Poruka = "Nedostaju LD00/LD ili LDRAD tabela.";
            return;
        }

        try
        {
            var ldZapisi = DbfReader.CitajSveZapise(ldPath);
            var radnici = DbfReader.CitajSveZapise(ldradPath);
            var imePoBroju = radnici
                .GroupBy(r => Int(r, "BROJ"))
                .ToDictionary(g => g.Key, g => Str(g.First(), "IME_PREZ"));

            int startBr = Stavke.Count;
            int dodato = 0;

            for (int i = 0; i < ldZapisi.Count; i++)
            {
                var z = ldZapisi[i];
                int broj = Int(z, "BROJ");
                decimal bruto = Dec(z, "BRUTO");
                decimal poroslob = Dec(z, "POROSLOB");
                decimal porez = Dec(z, "POREZ");
                decimal neto = Dec(z, "NETO");
                decimal dopsocf = Dec(z, "DOPSOCF");
                decimal dopsocr = Dec(z, "DOPSOCR");

                Stavke.Add(new LdNz1Stavka
                {
                    Br = (startBr + dodato + 1).ToString(CultureInfo.InvariantCulture),
                    ImePrez = imePoBroju.TryGetValue(broj, out var ime) && !string.IsNullOrWhiteSpace(ime)
                        ? ime
                        : Str(z, "IME_PREZ"),
                    Bruto = bruto,
                    Pdete = bruto,
                    Oposto = Dec(z, "OPOSTO"),
                    Tposto = Dec(z, "TPOSTO"),
                    Sposto = Dec(z, "SPOSTO"),
                    Osnovp = bruto - poroslob,
                    Porez = porez,
                    Zaisplat = neto,
                    Dop = dopsocf + dopsocr,
                    Obaveze = neto + porez + dopsocf + dopsocr,
                });

                dodato++;
            }

            Poruka = $"Preuzeto {dodato} stavki iz {Path.GetFileName(ldPath)}.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri preuzimanju: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ObrazacNz11()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za obrazac NZ1-1.";
            return;
        }

        var view = new Views.Zarade.LdNz1ReportView(Stavke);
        view.ShowDialog();
    }

    [RelayCommand]
    private void Brisanje()
    {
        Stavke.Clear();
        Poruka = "Tabela je obrisana.";
        ZatvaranjeZatrazeno?.Invoke();
    }

    private void UcitajPostojecuTabelu()
    {
        Stavke.Clear();

        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        var putanja = PronadjiDbf(_folderPath, "ldnz1.dbf");
        if (putanja is null)
        {
            Poruka = "Fajl ldnz1.dbf nije pronađen.";
            return;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(putanja);
            foreach (var z in zapisi)
            {
                Stavke.Add(new LdNz1Stavka
                {
                    Br = Str(z, "BR"),
                    ImePrez = Str(z, "IME_PREZ"),
                    Bruto = Dec(z, "BRUTO"),
                    Pdete = Dec(z, "PDETE"),
                    Oposto = Dec(z, "OPOSTO"),
                    Tposto = Dec(z, "TPOSTO"),
                    Sposto = Dec(z, "SPOSTO"),
                    Osnovp = Dec(z, "OSNOVP"),
                    Porez = Dec(z, "POREZ"),
                    Zaisplat = Dec(z, "ZAISPLAT"),
                    Dop = Dec(z, "DOP"),
                    Obaveze = Dec(z, "OBAVEZE"),
                });
            }

            Poruka = $"Ucitano {Stavke.Count} stavki iz ldnz1.dbf.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri ucitavanju: {ex.Message}";
        }
    }

    private static string? PronadjiDbf(string folderPath, params string[] candidates)
    {
        foreach (var fileName in candidates)
        {
            var full = Path.Combine(folderPath, fileName);
            if (File.Exists(full))
                return full;

            var found = Directory.GetFiles(folderPath, "*.dbf", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase));
            if (found is not null)
                return found;
        }

        return null;
    }

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;

    private static decimal Dec(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;

    private static int Int(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? (int)d : 0;

    [RelayCommand]
    private void Osvezi() => UcitajPostojecuTabelu();
}

public class LdNz1Stavka
{
    public string Br { get; set; } = string.Empty;
    public string ImePrez { get; set; } = string.Empty;
    public decimal Bruto { get; set; }
    public decimal Pdete { get; set; }
    public decimal Oposto { get; set; }
    public decimal Tposto { get; set; }
    public decimal Sposto { get; set; }
    public decimal Osnovp { get; set; }
    public decimal Porez { get; set; }
    public decimal Zaisplat { get; set; }
    public decimal Dop { get; set; }
    public decimal Obaveze { get; set; }
}
