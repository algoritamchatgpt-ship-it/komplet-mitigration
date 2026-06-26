using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoritam.WPF.ViewModels;

public sealed class LdIzjavaRadnoVremeStavka
{
    public int Broj { get; set; }
    public string ImePrez { get; set; } = string.Empty;
    public string Firma1 { get; set; } = string.Empty;
    public decimal Proc1 { get; set; }
    public string Firma2 { get; set; } = string.Empty;
    public decimal Proc2 { get; set; }
    public string Firma3 { get; set; } = string.Empty;
    public decimal Proc3 { get; set; }
    public string Firma4 { get; set; } = string.Empty;
    public decimal Proc4 { get; set; }
    public string Firma5 { get; set; } = string.Empty;
    public decimal Proc5 { get; set; }
    public string Maticnibr { get; set; } = string.Empty;
    public string Posta { get; set; } = string.Empty;
    public string Mesto { get; set; } = string.Empty;
    public string Adresa { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public DateTime? Datdok { get; set; }
    public string Preneto { get; set; } = string.Empty;
    public long Idbr { get; set; }
}

public partial class LdIzjavaRadnoVremeViewModel : ObservableObject
{
    private readonly string _folderPath;

    [ObservableProperty] private string _naslov = "IZJAVA O RADNOM VREMENU";
    [ObservableProperty] private ObservableCollection<LdIzjavaRadnoVremeStavka> _stavke = [];
    [ObservableProperty] private LdIzjavaRadnoVremeStavka? _selektovana;
    [ObservableProperty] private string _poruka = string.Empty;

    public event Action? ZatvaranjeZatrazeno;

    public LdIzjavaRadnoVremeViewModel(AppState appState)
    {
        _folderPath = appState.AktivnaFirma?.FolderPath ?? string.Empty;
        Ucitaj();
    }

    [RelayCommand]
    private void Dodaj()
    {
        var nova = new LdIzjavaRadnoVremeStavka
        {
            Datdok = DateTime.Today
        };

        Stavke.Add(nova);
        Selektovana = nova;
        Sacuvaj();
        Poruka = "Dodat je novi red.";
    }

    [RelayCommand]
    private void RadniciF4()
    {
        if (Selektovana is null)
        {
            Poruka = "Nije selektovan red.";
            return;
        }

        if (Selektovana.Broj <= 0)
        {
            Poruka = "Unesite broj radnika.";
            return;
        }

        var ldradPath = LdBolovanjeDbfSupport.PronadjiDbf(_folderPath, "ldrad.dbf");
        if (ldradPath is null)
        {
            Poruka = "ldrad.dbf nije pronađen.";
            return;
        }

        var radnik = DbfReader.CitajSveZapise(ldradPath)
            .FirstOrDefault(x => LdBolovanjeDbfSupport.Int(x, "BROJ") == Selektovana.Broj);

        if (radnik is null)
        {
            Poruka = $"Radnik broj {Selektovana.Broj} nije pronađen.";
            return;
        }

        Selektovana.ImePrez = LdBolovanjeDbfSupport.Str(radnik, "IME_PREZ");
        Selektovana.Maticnibr = LdBolovanjeDbfSupport.Str(radnik, "MATICNIBR");
        Selektovana.Posta = LdBolovanjeDbfSupport.Str(radnik, "POSTA");
        Selektovana.Mesto = LdBolovanjeDbfSupport.Str(radnik, "MESTO");
        Selektovana.Adresa = LdBolovanjeDbfSupport.Str(radnik, "ADRESA");
        Selektovana.Telefon = LdBolovanjeDbfSupport.Str(radnik, "TELEFON");

        Stavke = new ObservableCollection<LdIzjavaRadnoVremeStavka>(Stavke);
        Selektovana = Stavke.FirstOrDefault(x => x.Broj == LdBolovanjeDbfSupport.Int(radnik, "BROJ"))
                     ?? Stavke.FirstOrDefault();

        Sacuvaj();
        Poruka = "Podaci radnika su popunjeni.";
    }

    [RelayCommand]
    private void Izjava()
    {
        if (Selektovana is null)
        {
            Poruka = "Nije selektovan red.";
            return;
        }

        var view = new Views.Zarade.LdBolGenericReportView(
            "LDIZRVRE0 - IZJAVA",
            new[] { Selektovana },
            1);
        view.ShowDialog();
    }

    [RelayCommand]
    private void Brisanje()
    {
        if (Selektovana is null)
        {
            Poruka = "Nije selektovan red.";
            return;
        }

        Stavke.Remove(Selektovana);
        Selektovana = Stavke.FirstOrDefault();
        Sacuvaj();
        Poruka = "Obrisan je red.";
    }

    [RelayCommand]
    private void Prvi()
    {
        if (Stavke.Count == 0)
            return;

        Selektovana = Stavke[0];
    }

    [RelayCommand]
    private void Zadnji()
    {
        if (Stavke.Count == 0)
            return;

        Selektovana = Stavke[^1];
    }

    [RelayCommand]
    private void Gore()
    {
        if (Selektovana is null)
            return;

        var indeks = Stavke.IndexOf(Selektovana);
        if (indeks > 0)
            Selektovana = Stavke[indeks - 1];
    }

    [RelayCommand]
    private void Dole()
    {
        if (Selektovana is null)
            return;

        var indeks = Stavke.IndexOf(Selektovana);
        if (indeks >= 0 && indeks < Stavke.Count - 1)
            Selektovana = Stavke[indeks + 1];
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

        var dbfPath = LdBolovanjeDbfSupport.PronadjiDbf(_folderPath, "ldizrvre.dbf");
        if (dbfPath is null)
        {
            Poruka = "ldizrvre.dbf nije pronađen.";
            return;
        }

        try
        {
            foreach (var red in DbfReader.CitajSveZapise(dbfPath))
            {
                Stavke.Add(new LdIzjavaRadnoVremeStavka
                {
                    Broj = LdBolovanjeDbfSupport.Int(red, "BROJ"),
                    ImePrez = LdBolovanjeDbfSupport.Str(red, "IME_PREZ"),
                    Firma1 = LdBolovanjeDbfSupport.Str(red, "FIRMA1"),
                    Proc1 = LdBolovanjeDbfSupport.Dec(red, "PROC1"),
                    Firma2 = LdBolovanjeDbfSupport.Str(red, "FIRMA2"),
                    Proc2 = LdBolovanjeDbfSupport.Dec(red, "PROC2"),
                    Firma3 = LdBolovanjeDbfSupport.Str(red, "FIRMA3"),
                    Proc3 = LdBolovanjeDbfSupport.Dec(red, "PROC3"),
                    Firma4 = LdBolovanjeDbfSupport.Str(red, "FIRMA4"),
                    Proc4 = LdBolovanjeDbfSupport.Dec(red, "PROC4"),
                    Firma5 = LdBolovanjeDbfSupport.Str(red, "FIRMA5"),
                    Proc5 = LdBolovanjeDbfSupport.Dec(red, "PROC5"),
                    Maticnibr = LdBolovanjeDbfSupport.Str(red, "MATICNIBR"),
                    Posta = LdBolovanjeDbfSupport.Str(red, "POSTA"),
                    Mesto = LdBolovanjeDbfSupport.Str(red, "MESTO"),
                    Adresa = LdBolovanjeDbfSupport.Str(red, "ADRESA"),
                    Telefon = LdBolovanjeDbfSupport.Str(red, "TELEFON"),
                    Datdok = LdBolovanjeDbfSupport.Dat(red, "DATDOK"),
                    Preneto = LdBolovanjeDbfSupport.Str(red, "PRENETO"),
                    Idbr = LdBolovanjeDbfSupport.Long(red, "IDBR")
                });
            }

            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Ucitano {Stavke.Count} redova iz ldizrvre.dbf.";
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
                "ldizrvre.dbf",
                Stavke.ToList(),
                ResolveValue);
        }
        catch
        {
            // Poruku postavlja caller.
        }
    }

    private static object? ResolveValue(LdIzjavaRadnoVremeStavka row, string fieldName) => fieldName.ToUpperInvariant() switch
    {
        "BROJ" => row.Broj,
        "IME_PREZ" => LdBolovanjeDbfSupport.NormalizeText(row.ImePrez),
        "FIRMA1" => LdBolovanjeDbfSupport.NormalizeText(row.Firma1),
        "PROC1" => row.Proc1,
        "FIRMA2" => LdBolovanjeDbfSupport.NormalizeText(row.Firma2),
        "PROC2" => row.Proc2,
        "FIRMA3" => LdBolovanjeDbfSupport.NormalizeText(row.Firma3),
        "PROC3" => row.Proc3,
        "FIRMA4" => LdBolovanjeDbfSupport.NormalizeText(row.Firma4),
        "PROC4" => row.Proc4,
        "FIRMA5" => LdBolovanjeDbfSupport.NormalizeText(row.Firma5),
        "PROC5" => row.Proc5,
        "MATICNIBR" => LdBolovanjeDbfSupport.NormalizeText(row.Maticnibr),
        "POSTA" => LdBolovanjeDbfSupport.NormalizeText(row.Posta),
        "MESTO" => LdBolovanjeDbfSupport.NormalizeText(row.Mesto),
        "ADRESA" => LdBolovanjeDbfSupport.NormalizeText(row.Adresa),
        "TELEFON" => LdBolovanjeDbfSupport.NormalizeText(row.Telefon),
        "DATDOK" => row.Datdok,
        "PRENETO" => LdBolovanjeDbfSupport.NormalizeText(row.Preneto),
        "IDBR" => row.Idbr,
        _ => null
    };
}
