using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Fox forma LDPORPOT12 - UTVRDJIVANJE PROSECNE ZARADE KOD POSLODAVCA ZA 12 MESECI.
/// </summary>
public partial class LdPorp12ViewModel : ObservableObject
{
    private readonly string _folderPath;

    [ObservableProperty] private ObservableCollection<LdPorp12Stavka> _stavke = [];
    [ObservableProperty] private LdPorp12Stavka? _selektovana;
    [ObservableProperty] private string _naslov = "UTVRDJIVANJE PROSECNE ZARADE KOD POSLODAVCA ZA 12 MESECI";
    [ObservableProperty] private string _poruka = string.Empty;

    public event Action? ZatvaranjeZatrazeno;

    public LdPorp12ViewModel(AppState appState)
    {
        _folderPath = appState.AktivnaFirma?.FolderPath ?? string.Empty;
        UcitajPostojecuTabelu();
    }

    [RelayCommand]
    private void Potvrda()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za potvrdu.";
            return;
        }

        decimal brutoSum = 0m;
        int brutoCount = 0;
        decimal netoSum = 0m;
        int netoCount = 0;

        foreach (var s in Stavke)
        {
            if (s.Bruto != 0m)
            {
                brutoSum += s.Bruto;
                brutoCount++;
            }

            if (s.Neto != 0m)
            {
                netoSum += s.Neto;
                netoCount++;
            }
        }

        decimal avgBruto = brutoCount == 0 ? 0m : Math.Round(brutoSum / brutoCount, 2);
        decimal avgNeto = netoCount == 0 ? 0m : Math.Round(netoSum / netoCount, 2);

        var view = new Views.Zarade.LdPorp12ReportView(Stavke, avgBruto, avgNeto);
        view.ShowDialog();
    }

    [RelayCommand]
    private void Dodavanje()
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

            int dodato = 0;
            foreach (var z in ldZapisi)
            {
                int broj = Int(z, "BROJ");
                string imePrez = imePoBroju.TryGetValue(broj, out var ime) && !string.IsNullOrWhiteSpace(ime)
                    ? ime
                    : Str(z, "IME_PREZ");

                for (int i = 0; i < 12; i++)
                {
                    Stavke.Add(new LdPorp12Stavka
                    {
                        Broj = broj,
                        ImePrez = imePrez,
                    });
                    dodato++;
                }
            }

            Poruka = $"Dodato {dodato} redova.";
            Selektovana = Stavke.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri dodavanju: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Brisanje()
    {
        Stavke.Clear();
        Poruka = "Tabela je obrisana.";
        ZatvaranjeZatrazeno?.Invoke();
    }

    [RelayCommand]
    private void BrisanjeReda()
    {
        if (Selektovana is null)
        {
            Poruka = "Nije selektovan red.";
            return;
        }

        Stavke.Remove(Selektovana);
        Selektovana = Stavke.FirstOrDefault();
        Poruka = "Obrisan je jedan red.";
    }

    private void UcitajPostojecuTabelu()
    {
        Stavke.Clear();

        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        var putanja = PronadjiDbf(_folderPath, "ldporp12.dbf");
        if (putanja is null)
        {
            Poruka = "Fajl ldporp12.dbf nije pronađen.";
            return;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(putanja);
            foreach (var z in zapisi)
            {
                Stavke.Add(new LdPorp12Stavka
                {
                    Broj = Int(z, "BROJ"),
                    ImePrez = Str(z, "IME_PREZ"),
                    Mes = Int(z, "MES"),
                    Mesec = Str(z, "MESEC"),
                    Bruto = Dec(z, "BRUTO"),
                    Neto = Dec(z, "NETO"),
                });
            }

            Poruka = $"Ucitano {Stavke.Count} stavki iz ldporp12.dbf.";
            Selektovana = Stavke.FirstOrDefault();
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

public partial class LdPorp12Stavka : ObservableObject
{
    [ObservableProperty] private int _broj;
    [ObservableProperty] private string _imePrez = string.Empty;
    [ObservableProperty] private int _mes;
    [ObservableProperty] private string _mesec = string.Empty;
    [ObservableProperty] private decimal _bruto;
    [ObservableProperty] private decimal _neto;
}
