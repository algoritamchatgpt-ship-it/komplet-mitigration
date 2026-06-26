using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoritam.WPF.ViewModels;

public sealed class LdPripravniciStavka
{
    public int Broj { get; set; }
    public string ImePrez { get; set; } = string.Empty;
    public decimal Casrad { get; set; }
    public decimal Casbol { get; set; }
    public decimal Neto { get; set; }
    public decimal Netobol { get; set; }
    public decimal Porez { get; set; }
    public decimal Dopsocr { get; set; }
    public decimal Ukupno { get; set; }
    public string Nivo { get; set; } = string.Empty;
    public string Preneto { get; set; } = string.Empty;
    public long Idbr { get; set; }
}

public partial class LdPripravniciViewModel : ObservableObject
{
    private readonly string _folderPath;

    [ObservableProperty] private string _naslov = "OBRAZAC REFUNDACIJE ZA PRIPRAVNIKE";
    [ObservableProperty] private ObservableCollection<LdPripravniciStavka> _stavke = [];
    [ObservableProperty] private LdPripravniciStavka? _selektovana;
    [ObservableProperty] private string _poruka = string.Empty;

    public event Action? ZatvaranjeZatrazeno;

    public LdPripravniciViewModel(AppState appState)
    {
        _folderPath = appState.AktivnaFirma?.FolderPath ?? string.Empty;
        Ucitaj();
    }

    [RelayCommand]
    private void Preuzimanje()
    {
        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        var ldPath = LdBolovanjeDbfSupport.PronadjiPrviDbf(_folderPath, "ld00.dbf", "ld.dbf");
        var ldradPath = LdBolovanjeDbfSupport.PronadjiDbf(_folderPath, "ldrad.dbf");
        if (ldPath is null || ldradPath is null)
        {
            Poruka = "Nedostaju ld00/ld ili ldrad tabela.";
            return;
        }

        try
        {
            var ldRows = DbfReader.CitajSveZapise(ldPath);
            var radnici = DbfReader.CitajSveZapise(ldradPath)
                .GroupBy(x => LdBolovanjeDbfSupport.Int(x, "BROJ"))
                .ToDictionary(g => g.Key, g => g.First());

            var brojDodatih = 0;
            foreach (var red in ldRows)
            {
                var broj = LdBolovanjeDbfSupport.Int(red, "BROJ");
                if (!radnici.TryGetValue(broj, out var radnik))
                    continue;

                var priprav = LdBolovanjeDbfSupport.Str(radnik, "PRIPRAV");
                if (string.IsNullOrWhiteSpace(priprav))
                    continue;

                var neto = LdBolovanjeDbfSupport.Dec(red, "NETO");
                var porez = LdBolovanjeDbfSupport.Dec(red, "POREZ");
                var dopsocr = LdBolovanjeDbfSupport.Dec(red, "DOPSOCR");
                var dopsocf = LdBolovanjeDbfSupport.Dec(red, "DOPSOCF");
                var casuk = LdBolovanjeDbfSupport.Dec(red, "CASUK");
                var casbol = LdBolovanjeDbfSupport.Dec(red, "CASBOL");

                Stavke.Add(new LdPripravniciStavka
                {
                    Broj = broj,
                    ImePrez = LdBolovanjeDbfSupport.Str(radnik, "IME_PREZ"),
                    Casrad = casuk - casbol,
                    Casbol = casbol,
                    Neto = neto,
                    Porez = porez,
                    Dopsocr = dopsocr,
                    Ukupno = neto + dopsocf + dopsocr,
                    Nivo = "PRIPRAVNICI SA VISIM ILI VISOKIM TROGODISNJIM OBRAZOVANJEM"
                });
                brojDodatih++;
            }

            Selektovana = Stavke.FirstOrDefault();
            Sacuvaj();
            Poruka = $"Preuzeto {brojDodatih} redova.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri preuzimanju: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Obrazac()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za obrazac.";
            return;
        }

        var view = new Views.Zarade.LdBolGenericReportView(
            "LDPRIP1 - OBRAZAC",
            Stavke.ToList(),
            Stavke.Count);
        view.ShowDialog();
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
        Sacuvaj();
        Poruka = "Obrisan je jedan red.";
    }

    [RelayCommand]
    private void BrisanjeSve()
    {
        Stavke.Clear();
        Sacuvaj();
        Poruka = "Tabela je obrisana.";
    }

    [RelayCommand]
    private void SacuvajRucno()
    {
        Sacuvaj();
        Poruka = "Izmene su sačuvane.";
    }

    [RelayCommand]
    private void Izlaz() => ZatvaranjeZatrazeno?.Invoke();

    private void Ucitaj()
    {
        Stavke.Clear();

        if (string.IsNullOrWhiteSpace(_folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        var dbfPath = LdBolovanjeDbfSupport.PronadjiDbf(_folderPath, "ldprip.dbf");
        if (dbfPath is null)
        {
            Poruka = "ldprip.dbf nije pronađen.";
            return;
        }

        try
        {
            foreach (var red in DbfReader.CitajSveZapise(dbfPath))
            {
                Stavke.Add(new LdPripravniciStavka
                {
                    Broj = LdBolovanjeDbfSupport.Int(red, "BROJ"),
                    ImePrez = LdBolovanjeDbfSupport.Str(red, "IME_PREZ"),
                    Casrad = LdBolovanjeDbfSupport.Dec(red, "CASRAD"),
                    Casbol = LdBolovanjeDbfSupport.Dec(red, "CASBOL"),
                    Neto = LdBolovanjeDbfSupport.Dec(red, "NETO"),
                    Netobol = LdBolovanjeDbfSupport.Dec(red, "NETOBOL"),
                    Porez = LdBolovanjeDbfSupport.Dec(red, "POREZ"),
                    Dopsocr = LdBolovanjeDbfSupport.Dec(red, "DOPSOCR"),
                    Ukupno = LdBolovanjeDbfSupport.Dec(red, "UKUPNO"),
                    Nivo = LdBolovanjeDbfSupport.Str(red, "NIVO"),
                    Preneto = LdBolovanjeDbfSupport.Str(red, "PRENETO"),
                    Idbr = LdBolovanjeDbfSupport.Long(red, "IDBR")
                });
            }

            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Ucitano {Stavke.Count} redova iz ldprip.dbf.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri ucitavanju: {ex.Message}";
        }
    }

    private void Sacuvaj()
    {
        if (string.IsNullOrWhiteSpace(_folderPath))
            return;

        try
        {
            LdBolovanjeDbfSupport.SacuvajTabelu(
                _folderPath,
                "ldprip.dbf",
                Stavke.ToList(),
                ResolveValue);
        }
        catch
        {
            // Poruku postavlja caller.
        }
    }

    private static object? ResolveValue(LdPripravniciStavka row, string fieldName) => fieldName.ToUpperInvariant() switch
    {
        "BROJ" => row.Broj,
        "IME_PREZ" => LdBolovanjeDbfSupport.NormalizeText(row.ImePrez),
        "CASRAD" => row.Casrad,
        "CASBOL" => row.Casbol,
        "NETO" => row.Neto,
        "NETOBOL" => row.Netobol,
        "POREZ" => row.Porez,
        "DOPSOCR" => row.Dopsocr,
        "UKUPNO" => row.Ukupno,
        "NIVO" => LdBolovanjeDbfSupport.NormalizeText(row.Nivo),
        "PRENETO" => LdBolovanjeDbfSupport.NormalizeText(row.Preneto),
        "IDBR" => row.Idbr,
        _ => null
    };
}
