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

public sealed class ZahtevPovracajStavka
{
    public int Broj { get; set; }
    public string ImePrez { get; set; } = string.Empty;
    public string Jmbg { get; set; } = string.Empty;
    public string SifraPrih { get; set; } = string.Empty;
    public string OznOlaks { get; set; } = string.Empty;
    public string OznBen { get; set; } = string.Empty;
    public string Umanjenje { get; set; } = string.Empty;
    public decimal BrutoOsnovica { get; set; }
    public decimal IznosOlaksice { get; set; }
    public decimal PorezUmanj { get; set; }
    public decimal DopUmanj { get; set; }
    public decimal UkupnoPovracaj => PorezUmanj + DopUmanj;
}

public partial class ZahtevPovracajViewModel : ObservableObject
{
    private readonly AppState _appState;

    [ObservableProperty] private ObservableCollection<ZahtevPovracajStavka> _stavke = [];
    [ObservableProperty] private string _firmaNaziv = string.Empty;
    [ObservableProperty] private string _firmaPib = string.Empty;
    [ObservableProperty] private string _firmaMesto = string.Empty;
    [ObservableProperty] private string _firmaAdresa = string.Empty;
    [ObservableProperty] private string _jmbgPodnosioca = string.Empty;
    [ObservableProperty] private string _period = string.Empty;
    [ObservableProperty] private string _osnov = "Čl. 21v ZPD i čl. 45 Zakona o doprinosima";
    [ObservableProperty] private string _poruka = string.Empty;

    public decimal UkupnoPorez => Stavke.Sum(s => s.PorezUmanj);
    public decimal UkupnoDop => Stavke.Sum(s => s.DopUmanj);
    public decimal UkupnoSvega => Stavke.Sum(s => s.UkupnoPovracaj);

    public event Action? ZatvaranjeZahtevano;

    public ZahtevPovracajViewModel(AppState appState, IReadOnlyList<RadniciOlaksiceStavka>? radniciOlaksice = null)
    {
        _appState = appState;
        UcitajFirmuInfo();
        UcitajStavke(radniciOlaksice);
    }

    public string Naslov => "ZAHTEV ZA POVRAĆAJ POREZA I DOPRINOSA";

    private void UcitajFirmuInfo()
    {
        var folder = _appState.AktivnaFirma?.FolderPath;
        FirmaNaziv = _appState.AktivnaFirma?.Naziv ?? string.Empty;

        if (string.IsNullOrWhiteSpace(folder)) return;

        var firmaPath = LdObracunDbfReader.PronadjiDbf(folder, "firma.dbf");
        if (firmaPath == null) return;

        try
        {
            var red = DbfReader.CitajSveZapise(firmaPath).FirstOrDefault();
            if (red == null) return;

            FirmaPib = Str(red, "FPOR");
            FirmaMesto = Str(red, "FMES");
            var ulica = $"{Str(red, "FUL")} {Str(red, "FULBR")}".Trim();
            FirmaAdresa = ulica;
            JmbgPodnosioca = PrvaNeprazna(Str(red, "FMBSAV"), Str(red, "FJMBG"));
        }
        catch { }

        var paramPath = LdObracunDbfReader.PronadjiDbf(folder, "ldparam.dbf");
        if (paramPath == null) return;
        try
        {
            var red = DbfReader.CitajSveZapise(paramPath).FirstOrDefault();
            if (red == null) return;
            var god = Str(red, "GODINA");
            var mes = Int(red, "MESEC");
            if (!string.IsNullOrWhiteSpace(god) && mes > 0)
                Period = $"{god}-{mes:00}";
        }
        catch { }
    }

    private void UcitajStavke(IReadOnlyList<RadniciOlaksiceStavka>? radnici)
    {
        if (radnici == null || radnici.Count == 0)
        {
            // Ucitaj iz ldrad.dbf direktno
            var folder = _appState.AktivnaFirma?.FolderPath;
            if (string.IsNullOrWhiteSpace(folder)) return;

            var putanja = LdObracunDbfReader.PronadjiDbf(folder, "ldrad.dbf");
            if (putanja == null) return;

            try
            {
                var zapisi = DbfReader.CitajSveZapise(putanja);
                radnici = zapisi
                    .Where(z => !string.IsNullOrWhiteSpace(Str(z, "OZNOLAKS")) ||
                                !string.IsNullOrWhiteSpace(Str(z, "OZNBEN")) ||
                                Dec(z, "PROCUMANJ") != 0m ||
                                Dec(z, "PORUMANJ") != 0m)
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
                        PioUmanjF = Dec(z, "PIOUMANJF")
                    })
                    .Where(s => s.Broj > 0)
                    .ToList();
            }
            catch { return; }
        }

        // Pokusaj ucitati iznose iz arhive/platnog spiska
        var iznosi = UcitajIznoseIzArhive();

        var lista = radnici
            .OrderBy(r => r.Broj)
            .Select(r =>
            {
                iznosi.TryGetValue(r.Broj, out var iznos);
                var procPor = r.PorUmanj > 0 ? r.PorUmanj : r.ProcUmanj;
                var procDop = r.DopUmanj > 0 ? r.DopUmanj : (r.PioUmanjF > 0 ? r.PioUmanjF : 0m);

                var bruto = iznos?.Bruto ?? 0m;
                var porezUmanj = bruto > 0 && procPor > 0
                    ? Math.Round(bruto * procPor / 100m, 2, MidpointRounding.AwayFromZero)
                    : iznos?.PorezOlaks ?? 0m;
                var dopUmanj = bruto > 0 && procDop > 0
                    ? Math.Round(bruto * procDop / 100m, 2, MidpointRounding.AwayFromZero)
                    : iznos?.DopOlaks ?? 0m;

                return new ZahtevPovracajStavka
                {
                    Broj = r.Broj,
                    ImePrez = r.ImePrez,
                    Jmbg = r.Jmbg,
                    SifraPrih = r.SifraPrih,
                    OznOlaks = r.OznOlaks,
                    OznBen = r.OznBen,
                    Umanjenje = r.Umanjenje,
                    BrutoOsnovica = bruto,
                    PorezUmanj = porezUmanj,
                    DopUmanj = dopUmanj
                };
            })
            .ToList();

        Stavke = new ObservableCollection<ZahtevPovracajStavka>(lista);
        OnPropertyChanged(nameof(UkupnoPorez));
        OnPropertyChanged(nameof(UkupnoDop));
        OnPropertyChanged(nameof(UkupnoSvega));

        Poruka = $"Ucitano {lista.Count} radnika sa olakšicama.";
    }

    private sealed record IznosOlaksice(decimal Bruto, decimal PorezOlaks, decimal DopOlaks);

    private Dictionary<int, IznosOlaksice> UcitajIznoseIzArhive()
    {
        var result = new Dictionary<int, IznosOlaksice>();
        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder)) return result;

        // Probaj ldarhiva.dbf pa ldppp.dbf
        var paths = new[] { "ldarhiva.dbf", "ldppp.dbf" };
        foreach (var fname in paths)
        {
            var path = LdObracunDbfReader.PronadjiDbf(folder, fname);
            if (path == null) continue;
            try
            {
                var zapisi = DbfReader.CitajSveZapise(path);
                foreach (var z in zapisi)
                {
                    var broj = Int(z, "BROJ");
                    if (broj <= 0 || result.ContainsKey(broj)) continue;
                    var bruto = Dec(z, "BRUTO");
                    var porOlaks = Dec(z, "POROSLOB1") + Dec(z, "POROSLOB2") +
                                   Dec(z, "POROSLOB3") + Dec(z, "POROSLOB4");
                    var dopOlaks = Dec(z, "DOPUMANJ");
                    result[broj] = new IznosOlaksice(bruto, porOlaks, dopOlaks);
                }
                if (result.Count > 0) return result;
            }
            catch { }
        }
        return result;
    }

    [RelayCommand]
    private void GenerisiDokument()
    {
        if (Stavke.Count == 0) { Poruka = "Nema podataka za zahtev."; return; }

        var dlg = new SaveFileDialog
        {
            Title = "Sačuvaj zahtev za povraćaj",
            Filter = "Tekstualni fajl (*.txt)|*.txt|Svi fajlovi|*.*",
            FileName = $"ZAHTEV_POVRACAJ_{Period.Replace("-", "")}_{DateTime.Now:yyyyMMdd}.txt",
            DefaultExt = "txt"
        };
        if (dlg.ShowDialog() != true) return;

        var sb = new StringBuilder();
        sb.AppendLine("ZAHTEV ZA POVRAĆAJ POREZA I DOPRINOSA");
        sb.AppendLine(new string('=', 60));
        sb.AppendLine();
        sb.AppendLine($"Podnosilac: {FirmaNaziv}");
        sb.AppendLine($"PIB:        {FirmaPib}");
        sb.AppendLine($"Adresa:     {FirmaAdresa}, {FirmaMesto}");
        if (!string.IsNullOrWhiteSpace(JmbgPodnosioca))
            sb.AppendLine($"JMBG:       {JmbgPodnosioca}");
        sb.AppendLine($"Period:     {Period}");
        sb.AppendLine($"Osnov:      {Osnov}");
        sb.AppendLine();
        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"{"Br.",-5} {"Ime i prezime",-28} {"JMBG",-14} {"OznOlaks",-10} {"Bruto",12} {"Por.umanj.",11} {"Dop.umanj.",11} {"Ukupno",12}");
        sb.AppendLine(new string('-', 60));

        foreach (var s in Stavke)
        {
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0,-5} {1,-28} {2,-14} {3,-10} {4,12:N2} {5,11:N2} {6,11:N2} {7,12:N2}",
                s.Broj, s.ImePrez.Length > 27 ? s.ImePrez[..27] : s.ImePrez,
                s.Jmbg, s.OznOlaks, s.BrutoOsnovica, s.PorezUmanj, s.DopUmanj, s.UkupnoPovracaj));
        }

        sb.AppendLine(new string('-', 60));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
            "{0,-48} {1,11:N2} {2,11:N2} {3,12:N2}",
            "UKUPNO:", UkupnoPorez, UkupnoDop, UkupnoSvega));
        sb.AppendLine();
        sb.AppendLine($"Datum: {DateTime.Today:dd.MM.yyyy}");
        sb.AppendLine();
        sb.AppendLine("Potpis i pečat: ___________________________");

        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.GetEncoding(1250));
        Poruka = $"Zahtev sačuvan: {Path.GetFileName(dlg.FileName)}";
    }

    [RelayCommand]
    private void Zatvori() => ZatvaranjeZahtevano?.Invoke();

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
}
