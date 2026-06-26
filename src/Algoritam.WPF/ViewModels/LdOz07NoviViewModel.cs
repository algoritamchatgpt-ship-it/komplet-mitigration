using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public sealed class LdOz07NoviStavka
{
    public int Mesec { get; set; }
    public string NazMes { get; set; } = string.Empty;
    public int Godina { get; set; }
    public decimal Casuk { get; set; }
    public decimal Neto { get; set; }
    public decimal Bruto { get; set; }
    public int Broj { get; set; }
    public string ImePrez { get; set; } = string.Empty;
    public string Maticnibr { get; set; } = string.Empty;
    public string Lbobroj { get; set; } = string.Empty;
}

public partial class LdOz07NoviViewModel : ObservableObject
{
    private readonly string _folderPath;

    [ObservableProperty] private string _naslov = "Podatci za OBRAZAC OZ 7 NOVI";
    [ObservableProperty] private string _brojRadnika = string.Empty;
    [ObservableProperty] private string _maticniBroj = string.Empty;
    [ObservableProperty] private string _imePrezime = string.Empty;
    [ObservableProperty] private string _poruka = "Unesite broj radnika ili maticni broj.";
    [ObservableProperty] private decimal _ukupnoCasuk;
    [ObservableProperty] private decimal _ukupnoNeto;
    [ObservableProperty] private decimal _ukupnoBruto;
    [ObservableProperty] private DateTime _datum = DateTime.Today;
    [ObservableProperty] private ObservableCollection<LdOz07NoviStavka> _stavke = [];

    public event Action? ZatvaranjeZatrazeno;

    public LdOz07NoviViewModel(AppState appState)
    {
        _folderPath = appState.AktivnaFirma?.FolderPath ?? string.Empty;
    }

    [RelayCommand]
    private void PretraziPoBroju()
    {
        if (!int.TryParse(BrojRadnika.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var broj) || broj <= 0)
        {
            Poruka = "Unesite ispravan broj radnika.";
            return;
        }

        UcitajPodatke(
            x => x.Broj == broj,
            $"Ucitani podaci za broj radnika {broj}.");
    }

    [RelayCommand]
    private void PretraziPoMaticnom()
    {
        var maticni = MaticniBroj.Trim();
        if (maticni.Length != 13)
        {
            Poruka = "Maticni broj mora imati 13 cifara.";
            return;
        }

        UcitajPodatke(
            x => string.Equals(x.Maticnibr, maticni, StringComparison.OrdinalIgnoreCase),
            $"Ucitani podaci za maticni broj {maticni}.");
    }

    [RelayCommand]
    private void Preracun()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za preracun.";
            return;
        }

        UkupnoCasuk = Stavke.Sum(x => x.Casuk);
        UkupnoNeto = Stavke.Sum(x => x.Neto);
        UkupnoBruto = Stavke.Sum(x => x.Bruto);
        Poruka = "Preracun je zavrsen.";
    }

    [RelayCommand]
    private void Stampaj()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za obrazac OZ 7 NOVI.";
            return;
        }

        var view = new Views.Zarade.LdBolGenericReportView(
            "OBRAZAC OZ 7 NOVI",
            Stavke.ToList(),
            Stavke.Count);
        view.ShowDialog();
    }

    [RelayCommand]
    private void Izlaz() => ZatvaranjeZatrazeno?.Invoke();

    private void UcitajPodatke(Func<LdOz07NoviStavka, bool> filter, string porukaUspeh)
    {
        Stavke.Clear();
        ImePrezime = string.Empty;
        UkupnoCasuk = 0m;
        UkupnoNeto = 0m;
        UkupnoBruto = 0m;

        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        var ldarhivaPath = LdBolovanjeDbfSupport.PronadjiDbf(_folderPath, "ldarhiva.dbf");
        var ldradPath = LdBolovanjeDbfSupport.PronadjiDbf(_folderPath, "ldrad.dbf");
        if (ldarhivaPath is null || ldradPath is null)
        {
            Poruka = "Nedostaju ldarhiva.dbf ili ldrad.dbf.";
            return;
        }

        try
        {
            var arhiva = DbfReader.CitajSveZapise(ldarhivaPath);
            var radnici = DbfReader.CitajSveZapise(ldradPath)
                .GroupBy(r => LdBolovanjeDbfSupport.Int(r, "BROJ"))
                .ToDictionary(g => g.Key, g => g.First());

            var kandidati = new List<LdOz07NoviStavka>();
            foreach (var z in arhiva)
            {
                var broj = LdBolovanjeDbfSupport.Int(z, "BROJ");
                var mesec = LdBolovanjeDbfSupport.Int(z, "MESEC");
                if (mesec <= 0)
                    mesec = LdBolovanjeDbfSupport.MesecIzNaziva(LdBolovanjeDbfSupport.Str(z, "NAZMES"));

                var radnik = radnici.TryGetValue(broj, out var rr) ? rr : null;
                var maticni = LdBolovanjeDbfSupport.Str(z, "MATICNIBR");
                if (string.IsNullOrWhiteSpace(maticni))
                    maticni = radnik is not null ? LdBolovanjeDbfSupport.Str(radnik, "MATICNIBR") : string.Empty;

                kandidati.Add(new LdOz07NoviStavka
                {
                    Broj = broj,
                    Mesec = mesec,
                    NazMes = NazivMeseca(mesec),
                    Godina = LdBolovanjeDbfSupport.Int(z, "GODINA"),
                    Casuk = LdBolovanjeDbfSupport.Dec(z, "CASUK"),
                    Neto = LdBolovanjeDbfSupport.Dec(z, "NETO"),
                    Bruto = LdBolovanjeDbfSupport.Dec(z, "BRUTO"),
                    ImePrez = radnik is not null ? LdBolovanjeDbfSupport.Str(radnik, "IME_PREZ") : LdBolovanjeDbfSupport.Str(z, "IME_PREZ"),
                    Maticnibr = maticni,
                    Lbobroj = radnik is not null ? LdBolovanjeDbfSupport.Str(radnik, "LBOBROJ") : string.Empty
                });
            }

            var filtrirani = kandidati.Where(filter).ToList();
            if (filtrirani.Count == 0)
            {
                Poruka = "Nema podataka za zadati uslov.";
                return;
            }

            var grupisani = filtrirani
                .GroupBy(x => new { x.Godina, x.Mesec })
                .OrderBy(g => g.Key.Godina)
                .ThenBy(g => g.Key.Mesec)
                .Select(g => new LdOz07NoviStavka
                {
                    Godina = g.Key.Godina,
                    Mesec = g.Key.Mesec,
                    NazMes = NazivMeseca(g.Key.Mesec),
                    Casuk = g.Sum(x => x.Casuk),
                    Neto = g.Sum(x => x.Neto),
                    Bruto = g.Sum(x => x.Bruto),
                    Broj = g.First().Broj,
                    ImePrez = g.First().ImePrez,
                    Maticnibr = g.First().Maticnibr,
                    Lbobroj = g.First().Lbobroj
                })
                .ToList();

            foreach (var red in grupisani)
                Stavke.Add(red);

            ImePrezime = grupisani.First().ImePrez;
            MaticniBroj = grupisani.First().Maticnibr;
            BrojRadnika = grupisani.First().Broj.ToString(CultureInfo.InvariantCulture);
            UkupnoCasuk = Stavke.Sum(x => x.Casuk);
            UkupnoNeto = Stavke.Sum(x => x.Neto);
            UkupnoBruto = Stavke.Sum(x => x.Bruto);
            Poruka = porukaUspeh;
        }
        catch (Exception ex)
        {
            Poruka = $"Greska: {ex.Message}";
        }
    }

    private static string NazivMeseca(int mesec) => mesec switch
    {
        1 => "JANUAR",
        2 => "FEBRUAR",
        3 => "MART",
        4 => "APRIL",
        5 => "MAJ",
        6 => "JUN",
        7 => "JUL",
        8 => "AVGUST",
        9 => "SEPTEMBAR",
        10 => "OKTOBAR",
        11 => "NOVEMBAR",
        12 => "DECEMBAR",
        _ => string.Empty
    };
}
