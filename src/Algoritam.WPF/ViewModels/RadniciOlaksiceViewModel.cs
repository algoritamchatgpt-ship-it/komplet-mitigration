using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

public sealed class RadniciOlaksiceStavka
{
    public int Broj { get; set; }
    public string ImePrez { get; set; } = string.Empty;
    public string Jmbg { get; set; } = string.Empty;
    public string SifraPrih { get; set; } = string.Empty;
    public string OznOlaks { get; set; } = string.Empty;
    public string OznBen { get; set; } = string.Empty;
    public string Umanjenje { get; set; } = string.Empty;
    public decimal ProcUmanj { get; set; }
    public decimal PorUmanj { get; set; }
    public decimal DopUmanj { get; set; }
    public decimal PioUmanjR { get; set; }
    public decimal PioUmanjF { get; set; }
    public string Partija { get; set; } = string.Empty;
}

public partial class RadniciOlaksiceViewModel : ObservableObject
{
    private readonly AppState _appState;

    [ObservableProperty] private ObservableCollection<RadniciOlaksiceStavka> _stavke = [];
    [ObservableProperty] private RadniciOlaksiceStavka? _selektovana;
    [ObservableProperty] private string _poruka = string.Empty;
    [ObservableProperty] private string _firmaNaziv = string.Empty;
    [ObservableProperty] private bool _prikaziSveRadnike;

    public event Action? ZatvaranjeZahtevano;

    public RadniciOlaksiceViewModel(AppState appState)
    {
        _appState = appState;
        _ = UcitajAsync();
    }

    public string Naslov => "RADNICI ZA OLAKŠICE";

    partial void OnPrikaziSveRadnikeChanged(bool value) => _ = UcitajAsync();

    [RelayCommand]
    private async Task UcitajAsync()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        FirmaNaziv = _appState.AktivnaFirma?.Naziv ?? string.Empty;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Stavke = [];
            Poruka = "Folder aktivne firme nije pronađen.";
            return;
        }

        try
        {
            var putanja = LdObracunDbfReader.PronadjiDbf(folder, "ldrad.dbf");
            if (putanja == null)
            {
                Stavke = [];
                Poruka = "ldrad.dbf nije pronađen.";
                return;
            }

            var zapisi = await Task.Run(() => DbfReader.CitajSveZapise(putanja));

            var lista = zapisi
                .Select(z => new RadniciOlaksiceStavka
                {
                    Broj = Int(z, "BROJ"),
                    ImePrez = Str(z, "IME_PREZ"),
                    Jmbg = PrvaNeprazna(Str(z, "MATICNIBR"), Str(z, "JMBG")),
                    SifraPrih = Str(z, "SIFRAPRIH"),
                    OznOlaks = Str(z, "OZNOLAKS"),
                    OznBen = Str(z, "OZNBEN"),
                    Umanjenje = Str(z, "UMANJENJE"),
                    ProcUmanj = Dec(z, "PROCUMANJ"),
                    PorUmanj = Dec(z, "PORUMANJ"),
                    DopUmanj = Dec(z, "DOPUMANJ"),
                    PioUmanjR = Dec(z, "PIOUMANJR"),
                    PioUmanjF = Dec(z, "PIOUMANJF"),
                    Partija = Str(z, "PARTIJA")
                })
                .Where(s => s.Broj > 0)
                .Where(s => PrikaziSveRadnike || ImaOlaksicu(s))
                .OrderBy(s => s.Broj)
                .ToList();

            Stavke = new ObservableCollection<RadniciOlaksiceStavka>(lista);
            Selektovana = Stavke.FirstOrDefault();

            var brSaOlaks = lista.Count(ImaOlaksicu);
            Poruka = PrikaziSveRadnike
                ? $"Prikazano {lista.Count} radnika (od toga {brSaOlaks} sa olakšicama)."
                : $"Pronađeno {lista.Count} radnika sa olakšicama.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri učitavanju: {ex.Message}";
        }
    }

    [RelayCommand]
    private void IzvozTxt()
    {
        if (Stavke.Count == 0) { Poruka = "Nema podataka za izvoz."; return; }

        var dlg = new SaveFileDialog
        {
            Title = "Izvoz radnika sa olakšicama",
            Filter = "Tekstualni fajl (*.txt)|*.txt|CSV (*.csv)|*.csv|Svi fajlovi|*.*",
            FileName = $"OLAKSICE_{DateTime.Now:yyyyMMdd}.txt",
            DefaultExt = "txt"
        };
        if (dlg.ShowDialog() != true) return;

        var lines = new List<string>
        {
            "Broj;ImePrez;JMBG;SifraPrih;OznOlaks;OznBen;Umanjenje;ProcUmanj;PorUmanj;DopUmanj;PioUmanjR;PioUmanjF"
        };
        foreach (var s in Stavke)
        {
            lines.Add(string.Join(";",
                s.Broj, Csv(s.ImePrez), Csv(s.Jmbg), Csv(s.SifraPrih),
                Csv(s.OznOlaks), Csv(s.OznBen), Csv(s.Umanjenje),
                s.ProcUmanj.ToString("F2", CultureInfo.InvariantCulture),
                s.PorUmanj.ToString("F2", CultureInfo.InvariantCulture),
                s.DopUmanj.ToString("F2", CultureInfo.InvariantCulture),
                s.PioUmanjR.ToString("F2", CultureInfo.InvariantCulture),
                s.PioUmanjF.ToString("F2", CultureInfo.InvariantCulture)));
        }

        File.WriteAllLines(dlg.FileName, lines, Encoding.GetEncoding(1250));
        Poruka = $"Izvoz završen: {Stavke.Count} radnika → {Path.GetFileName(dlg.FileName)}";
    }

    [RelayCommand]
    private void OtvoriZahtevPovracaj()
    {
        var vm = new ZahtevPovracajViewModel(_appState, Stavke.Where(ImaOlaksicu).ToList());
        var view = new Views.Zarade.ZahtevPovracajView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.ShowDialog();
    }

    [RelayCommand]
    private void Zatvori() => ZatvaranjeZahtevano?.Invoke();

    private static bool ImaOlaksicu(RadniciOlaksiceStavka s) =>
        !string.IsNullOrWhiteSpace(s.OznOlaks) ||
        !string.IsNullOrWhiteSpace(s.OznBen) ||
        !string.IsNullOrWhiteSpace(s.Umanjenje) ||
        s.ProcUmanj != 0m || s.PorUmanj != 0m || s.DopUmanj != 0m ||
        s.PioUmanjR != 0m || s.PioUmanjF != 0m;

    private static string Str(Dictionary<string, object?> z, string k)
        => z.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;

    private static int Int(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0;
        if (v is decimal d) return (int)d;
        return int.TryParse(v.ToString(), out var i) ? i : 0;
    }

    private static decimal Dec(Dictionary<string, object?> z, string k)
    {
        if (!z.TryGetValue(k, out var v) || v == null) return 0m;
        if (v is decimal d) return d;
        return decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : 0m;
    }

    private static string PrvaNeprazna(string a, string b) =>
        !string.IsNullOrWhiteSpace(a) ? a : b;

    private static string Csv(string s) =>
        s.Contains(';') ? $"\"{s}\"" : s;
}
