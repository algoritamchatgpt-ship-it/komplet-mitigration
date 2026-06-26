using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// Fox forma LDPORPOT - UTVRDJIVANJE PROSECNE ZARADE KOD POSLODAVCA.
/// </summary>
public partial class LdPorpotViewModel : ObservableObject
{
    private readonly string _folderPath;

    [ObservableProperty] private ObservableCollection<LdPorpotStavka> _stavke = [];
    [ObservableProperty] private LdPorpotStavka? _selektovana;
    [ObservableProperty] private string _naslov = "UTVRDJIVANJE PROSECNE ZARADE KOD POSLODAVCA";
    [ObservableProperty] private string _poruka = string.Empty;

    public event Action? ZatvaranjeZatrazeno;

    public LdPorpotViewModel(AppState appState)
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

        var view = new Views.Zarade.LdPorpotReportView(Stavke);
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
            var radnikPoBroju = radnici
                .GroupBy(r => Int(r, "BROJ"))
                .ToDictionary(g => g.Key, g => g.First());

            int dodato = 0;
            foreach (var z in ldZapisi)
            {
                int broj = Int(z, "BROJ");
                string imePrez = Str(radnikPoBroju.TryGetValue(broj, out var rr) ? rr : z, "IME_PREZ");
                string maticni = Str(radnikPoBroju.TryGetValue(broj, out var rr2) ? rr2 : z, "MATICNIBR");

                for (int i = 0; i < 4; i++)
                {
                    Stavke.Add(new LdPorpotStavka
                    {
                        Broj = broj,
                        ImePrez = imePrez,
                        Maticnibr = maticni,
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
    private void Preracun()
    {
        // Fox kod preracuna radi samo nad prva 4 reda.
        if (Stavke.Count < 4)
        {
            Poruka = "Potrebna su najmanje 4 reda za preracun.";
            return;
        }

        var r1 = Stavke[0];
        var r2 = Stavke[1];
        var r3 = Stavke[2];
        var r4 = Stavke[3];

        if (r1.Raddin != 0m)
            r2.Radkoef = Math.Round(r2.Raddin / r1.Raddin * 100m, 2);
        if (r1.Firdin != 0m)
            r2.Firkoef = Math.Round(r2.Firdin / r1.Firdin * 100m, 2);

        if (r2.Raddin != 0m)
            r3.Radkoef = Math.Round(r3.Raddin / r2.Raddin * 100m, 2);
        if (r2.Firdin != 0m)
            r3.Firkoef = Math.Round(r3.Firdin / r2.Firdin * 100m, 2);

        if (r3.Raddin != 0m)
            r4.Radkoef = Math.Round(r4.Raddin / r3.Raddin * 100m, 2);
        if (r3.Firdin != 0m)
            r4.Firkoef = Math.Round(r4.Firdin / r3.Firdin * 100m, 2);

        Poruka = "Preracun uradjen za prva 4 reda.";
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

        var putanja = PronadjiDbf(_folderPath, "ldporpot.dbf");
        if (putanja is null)
        {
            Poruka = "Fajl ldporpot.dbf nije pronađen.";
            return;
        }

        try
        {
            var zapisi = DbfReader.CitajSveZapise(putanja);
            foreach (var z in zapisi)
            {
                Stavke.Add(new LdPorpotStavka
                {
                    Broj = Int(z, "BROJ"),
                    ImePrez = Str(z, "IME_PREZ"),
                    Mes = Int(z, "MES"),
                    Mesec = Str(z, "MESEC"),
                    Raddin = Dec(z, "RADDIN"),
                    Radkoef = Dec(z, "RADKOEF"),
                    Firdin = Dec(z, "FIRDIN"),
                    Firkoef = Dec(z, "FIRKOEF"),
                    Maticnibr = Str(z, "MATICNIBR"),
                    Lbobroj = Str(z, "LBOBROJ"),
                    Zkbroj = Str(z, "ZKBROJ"),
                    Nfirma = Str(z, "PRENETO"),
                    Nredni = Long(z, "IDBR"),
                });
            }

            Poruka = $"Ucitano {Stavke.Count} stavki iz ldporpot.dbf.";
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

    private static long Long(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? (long)d : 0L;

    [RelayCommand]
    private void Osvezi() => UcitajPostojecuTabelu();
}

public partial class LdPorpotStavka : ObservableObject
{
    [ObservableProperty] private int _broj;
    [ObservableProperty] private string _imePrez = string.Empty;
    [ObservableProperty] private int _mes;
    [ObservableProperty] private string _mesec = string.Empty;
    [ObservableProperty] private decimal _raddin;
    [ObservableProperty] private decimal _radkoef;
    [ObservableProperty] private decimal _firdin;
    [ObservableProperty] private decimal _firkoef;
    [ObservableProperty] private string _maticnibr = string.Empty;
    [ObservableProperty] private string _lbobroj = string.Empty;
    [ObservableProperty] private string _zkbroj = string.Empty;
    [ObservableProperty] private string _nfirma = string.Empty;
    [ObservableProperty] private long _nredni;
}
