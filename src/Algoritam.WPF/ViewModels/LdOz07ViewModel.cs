using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public sealed class LdOz07Stavka
{
    public int Broj { get; set; }
    public string ImePrez { get; set; } = string.Empty;
    public string Maticnibr { get; set; } = string.Empty;
    public string Lbobroj { get; set; } = string.Empty;
    public string Zkbroj { get; set; } = string.Empty;
    public int Mes1 { get; set; }
    public string Mesec1 { get; set; } = string.Empty;
    public decimal Casuc1 { get; set; }
    public decimal Staz1 { get; set; }
    public decimal Procmin1 { get; set; }
    public decimal Dinuc1 { get; set; }
    public decimal Dinmin1 { get; set; }
    public decimal Dinuk1 { get; set; }
    public int Mes2 { get; set; }
    public string Mesec2 { get; set; } = string.Empty;
    public decimal Casuc2 { get; set; }
    public decimal Staz2 { get; set; }
    public decimal Procmin2 { get; set; }
    public decimal Dinuc2 { get; set; }
    public decimal Dinmin2 { get; set; }
    public decimal Dinuk2 { get; set; }
    public int Mes3 { get; set; }
    public string Mesec3 { get; set; } = string.Empty;
    public decimal Casuc3 { get; set; }
    public decimal Staz3 { get; set; }
    public decimal Procmin3 { get; set; }
    public decimal Dinuc3 { get; set; }
    public decimal Dinmin3 { get; set; }
    public decimal Dinuk3 { get; set; }
    public decimal Casuk { get; set; }
    public decimal Dinuk { get; set; }
    public decimal Proscas { get; set; }
    public decimal Prosdin { get; set; }
    public decimal Staz { get; set; }
    public decimal Dincas { get; set; }
    public string Preneto { get; set; } = string.Empty;
    public long Idbr { get; set; }
}

public partial class LdOz07ViewModel : ObservableObject
{
    private readonly string _folderPath;

    [ObservableProperty] private string _naslov = "OBRAZAC OZ 7 POTVRDA O OSTVARENOJ ZARADI ZA BOLOVANJE";
    [ObservableProperty] private ObservableCollection<LdOz07Stavka> _stavke = [];
    [ObservableProperty] private LdOz07Stavka? _selektovana;
    [ObservableProperty] private string _poruka = string.Empty;

    public event Action? ZatvaranjeZatrazeno;

    public LdOz07ViewModel(AppState appState)
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

            var brojStart = Stavke.Count;
            foreach (var red in ldRows)
            {
                var brojRadnika = LdBolovanjeDbfSupport.Int(red, "BROJ");
                var radnik = radnici.TryGetValue(brojRadnika, out var rr) ? rr : null;

                Stavke.Add(new LdOz07Stavka
                {
                    Broj = ++brojStart,
                    ImePrez = radnik is not null ? LdBolovanjeDbfSupport.Str(radnik, "IME_PREZ") : string.Empty,
                    Maticnibr = radnik is not null ? LdBolovanjeDbfSupport.Str(radnik, "MATICNIBR") : string.Empty,
                    Lbobroj = radnik is not null ? LdBolovanjeDbfSupport.Str(radnik, "LBOBROJ") : string.Empty,
                    Zkbroj = radnik is not null ? LdBolovanjeDbfSupport.Str(radnik, "ZKBROJ") : string.Empty,
                    Staz = radnik is not null ? LdBolovanjeDbfSupport.Dec(radnik, "STAZ") : 0m
                });
            }

            Selektovana = Stavke.FirstOrDefault();
            Sacuvaj();
            Poruka = $"Preuzeto {ldRows.Count} redova.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri preuzimanju: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ObrazacNaknada()
    {
        if (Selektovana is null)
        {
            Poruka = "Nije selektovan red.";
            return;
        }

        var view = new Views.Zarade.LdBolGenericReportView(
            "LDOZ070 - OBRAZAC NAKNADA",
            new[] { Selektovana },
            1);
        view.ShowDialog();
    }

    [RelayCommand]
    private void KarticaF7()
    {
        if (Selektovana is null)
        {
            Poruka = "Nije selektovan red.";
            return;
        }

        var view = new Views.Zarade.LdBolRowEditorView("LDOZ07K - KARTICA F7", Selektovana);
        view.ShowDialog();
        Sacuvaj();
        Poruka = "Izmene su sačuvane.";
    }

    [RelayCommand]
    private void Brisanje()
    {
        Stavke.Clear();
        Sacuvaj();
        Poruka = "Tabela je obrisana.";
        ZatvaranjeZatrazeno?.Invoke();
    }

    [RelayCommand]
    private void DodajRed()
    {
        var novi = new LdOz07Stavka { Broj = Stavke.Count + 1 };
        Stavke.Add(novi);
        Selektovana = novi;
        Sacuvaj();
        Poruka = "Dodat je novi red.";
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

        var dbfPath = LdBolovanjeDbfSupport.PronadjiDbf(_folderPath, "ldoz07.dbf");
        if (dbfPath is null)
        {
            Poruka = "ldoz07.dbf nije pronađen.";
            return;
        }

        try
        {
            foreach (var z in DbfReader.CitajSveZapise(dbfPath))
            {
                Stavke.Add(new LdOz07Stavka
                {
                    Broj = LdBolovanjeDbfSupport.Int(z, "BROJ"),
                    ImePrez = LdBolovanjeDbfSupport.Str(z, "IME_PREZ"),
                    Maticnibr = LdBolovanjeDbfSupport.Str(z, "MATICNIBR"),
                    Lbobroj = LdBolovanjeDbfSupport.Str(z, "LBOBROJ"),
                    Zkbroj = LdBolovanjeDbfSupport.Str(z, "ZKBROJ"),
                    Mes1 = LdBolovanjeDbfSupport.Int(z, "MES1"),
                    Mesec1 = LdBolovanjeDbfSupport.Str(z, "MESEC1"),
                    Casuc1 = LdBolovanjeDbfSupport.Dec(z, "CASUC1"),
                    Staz1 = LdBolovanjeDbfSupport.Dec(z, "STAZ1"),
                    Procmin1 = LdBolovanjeDbfSupport.Dec(z, "PROCMIN1"),
                    Dinuc1 = LdBolovanjeDbfSupport.Dec(z, "DINUC1"),
                    Dinmin1 = LdBolovanjeDbfSupport.Dec(z, "DINMIN1"),
                    Dinuk1 = LdBolovanjeDbfSupport.Dec(z, "DINUK1"),
                    Mes2 = LdBolovanjeDbfSupport.Int(z, "MES2"),
                    Mesec2 = LdBolovanjeDbfSupport.Str(z, "MESEC2"),
                    Casuc2 = LdBolovanjeDbfSupport.Dec(z, "CASUC2"),
                    Staz2 = LdBolovanjeDbfSupport.Dec(z, "STAZ2"),
                    Procmin2 = LdBolovanjeDbfSupport.Dec(z, "PROCMIN2"),
                    Dinuc2 = LdBolovanjeDbfSupport.Dec(z, "DINUC2"),
                    Dinmin2 = LdBolovanjeDbfSupport.Dec(z, "DINMIN2"),
                    Dinuk2 = LdBolovanjeDbfSupport.Dec(z, "DINUK2"),
                    Mes3 = LdBolovanjeDbfSupport.Int(z, "MES3"),
                    Mesec3 = LdBolovanjeDbfSupport.Str(z, "MESEC3"),
                    Casuc3 = LdBolovanjeDbfSupport.Dec(z, "CASUC3"),
                    Staz3 = LdBolovanjeDbfSupport.Dec(z, "STAZ3"),
                    Procmin3 = LdBolovanjeDbfSupport.Dec(z, "PROCMIN3"),
                    Dinuc3 = LdBolovanjeDbfSupport.Dec(z, "DINUC3"),
                    Dinmin3 = LdBolovanjeDbfSupport.Dec(z, "DINMIN3"),
                    Dinuk3 = LdBolovanjeDbfSupport.Dec(z, "DINUK3"),
                    Casuk = LdBolovanjeDbfSupport.Dec(z, "CASUK"),
                    Dinuk = LdBolovanjeDbfSupport.Dec(z, "DINUK"),
                    Proscas = LdBolovanjeDbfSupport.Dec(z, "PROSCAS"),
                    Prosdin = LdBolovanjeDbfSupport.Dec(z, "PROSDIN"),
                    Staz = LdBolovanjeDbfSupport.Dec(z, "STAZ"),
                    Dincas = LdBolovanjeDbfSupport.Dec(z, "DINCAS"),
                    Preneto = LdBolovanjeDbfSupport.Str(z, "PRENETO"),
                    Idbr = LdBolovanjeDbfSupport.Long(z, "IDBR")
                });
            }

            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Ucitano {Stavke.Count} redova iz ldoz07.dbf.";
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
                "ldoz07.dbf",
                Stavke.ToList(),
                ResolveValue);
        }
        catch
        {
            // Poruka postavlja caller kada je to korisno.
        }
    }

    private static object? ResolveValue(LdOz07Stavka row, string fieldName) => fieldName.ToUpperInvariant() switch
    {
        "BROJ" => row.Broj,
        "IME_PREZ" => LdBolovanjeDbfSupport.NormalizeText(row.ImePrez),
        "MATICNIBR" => LdBolovanjeDbfSupport.NormalizeText(row.Maticnibr),
        "LBOBROJ" => LdBolovanjeDbfSupport.NormalizeText(row.Lbobroj),
        "ZKBROJ" => LdBolovanjeDbfSupport.NormalizeText(row.Zkbroj),
        "MES1" => row.Mes1,
        "MESEC1" => LdBolovanjeDbfSupport.NormalizeText(row.Mesec1),
        "CASUC1" => row.Casuc1,
        "STAZ1" => row.Staz1,
        "PROCMIN1" => row.Procmin1,
        "DINUC1" => row.Dinuc1,
        "DINMIN1" => row.Dinmin1,
        "DINUK1" => row.Dinuk1,
        "MES2" => row.Mes2,
        "MESEC2" => LdBolovanjeDbfSupport.NormalizeText(row.Mesec2),
        "CASUC2" => row.Casuc2,
        "STAZ2" => row.Staz2,
        "PROCMIN2" => row.Procmin2,
        "DINUC2" => row.Dinuc2,
        "DINMIN2" => row.Dinmin2,
        "DINUK2" => row.Dinuk2,
        "MES3" => row.Mes3,
        "MESEC3" => LdBolovanjeDbfSupport.NormalizeText(row.Mesec3),
        "CASUC3" => row.Casuc3,
        "STAZ3" => row.Staz3,
        "PROCMIN3" => row.Procmin3,
        "DINUC3" => row.Dinuc3,
        "DINMIN3" => row.Dinmin3,
        "DINUK3" => row.Dinuk3,
        "CASUK" => row.Casuk,
        "DINUK" => row.Dinuk,
        "PROSCAS" => row.Proscas,
        "PROSDIN" => row.Prosdin,
        "STAZ" => row.Staz,
        "DINCAS" => row.Dincas,
        "PRENETO" => LdBolovanjeDbfSupport.NormalizeText(row.Preneto),
        "IDBR" => row.Idbr,
        _ => null
    };
}
