using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Algoritam.WPF.ViewModels;

public sealed class LdPotvrdaZaradaStavka
{
    public string Br { get; set; } = string.Empty;
    public string ImePrez { get; set; } = string.Empty;
    public string Maticnibr { get; set; } = string.Empty;
    public string Mesec { get; set; } = string.Empty;
    public decimal Bruto { get; set; }
    public decimal Bruto2 { get; set; }
    public DateTime? Datispl { get; set; }
    public string Preneto { get; set; } = string.Empty;
    public long Idbr { get; set; }
}

public partial class LdPotvrdaZaradaViewModel : ObservableObject
{
    private readonly string _folderPath;

    [ObservableProperty] private string _naslov = "IZRADA POTVRDE ZA BOLOVANJE";
    [ObservableProperty] private ObservableCollection<LdPotvrdaZaradaStavka> _stavke = [];
    [ObservableProperty] private LdPotvrdaZaradaStavka? _selektovana;
    [ObservableProperty] private string _poruka = string.Empty;

    public event Action? ZatvaranjeZatrazeno;

    public LdPotvrdaZaradaViewModel(AppState appState)
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
        var ldparamPath = LdBolovanjeDbfSupport.PronadjiDbf(_folderPath, "ldparam.dbf");
        if (ldPath is null || ldradPath is null || ldparamPath is null)
        {
            Poruka = "Nedostaju ld00/ld, ldrad ili ldparam tabela.";
            return;
        }

        try
        {
            var ldRows = DbfReader.CitajSveZapise(ldPath);
            var radnici = DbfReader.CitajSveZapise(ldradPath)
                .GroupBy(x => LdBolovanjeDbfSupport.Int(x, "BROJ"))
                .ToDictionary(g => g.Key, g => g.First());
            var param = DbfReader.CitajSveZapise(ldparamPath).FirstOrDefault();

            var mesec = param is null ? string.Empty : LdBolovanjeDbfSupport.Str(param, "NAZMES");
            var datIspl = param is null ? (DateTime?)null : LdBolovanjeDbfSupport.Dat(param, "DAT1");
            var periodMesec = param is null ? 0 : LdBolovanjeDbfSupport.Int(param, "MESEC");
            var periodIsplata = param is null ? 0 : LdBolovanjeDbfSupport.Int(param, "ISPLATA");
            var periodGodina = param is null ? string.Empty : LdBolovanjeDbfSupport.Str(param, "GODINA");
            var ldRowsPeriod = ldRows
                .Where(r => LdBolovanjeDbfSupport.OdgovaraPeriodu(r, periodMesec, periodIsplata, periodGodina))
                .ToList();

            foreach (var red in ldRowsPeriod)
            {
                var broj = LdBolovanjeDbfSupport.Int(red, "BROJ");
                var radnik = radnici.TryGetValue(broj, out var r) ? r : null;

                var bruto = LdBolovanjeDbfSupport.Dec(red, "BRUTO");
                var procBol = LdBolovanjeDbfSupport.Dec(red, "PROCBOL");
                var bruto2 = procBol != 100m
                    ? LdBolovanjeDbfSupport.Round(bruto / 0.65m, 2)
                    : bruto;

                Stavke.Add(new LdPotvrdaZaradaStavka
                {
                    Br = (Stavke.Count + 1).ToString(CultureInfo.InvariantCulture),
                    ImePrez = radnik is null ? string.Empty : LdBolovanjeDbfSupport.Str(radnik, "IME_PREZ"),
                    Maticnibr = radnik is null ? string.Empty : LdBolovanjeDbfSupport.Str(radnik, "MATICNIBR"),
                    Mesec = string.IsNullOrWhiteSpace(mesec) ? LdBolovanjeDbfSupport.Str(red, "NAZMES") : mesec,
                    Bruto = bruto,
                    Bruto2 = bruto2,
                    Datispl = datIspl
                });
            }

            Selektovana = Stavke.FirstOrDefault();
            Sacuvaj();
            Poruka = $"Preuzeto {ldRowsPeriod.Count} redova za period.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri preuzimanju: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Potvrda()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za potvrdu.";
            return;
        }

        var view = new Views.Zarade.LdBolGenericReportView(
            "LDPOTVR0 - POTVRDA O ZARADI",
            Stavke.ToList(),
            Stavke.Count);
        view.ShowDialog();
    }

    [RelayCommand]
    private void Dodaj()
    {
        var nova = new LdPotvrdaZaradaStavka
        {
            Br = (Stavke.Count + 1).ToString(CultureInfo.InvariantCulture)
        };

        Stavke.Add(nova);
        Selektovana = nova;
        Sacuvaj();
        Poruka = "Dodat je novi red.";
    }

    [RelayCommand]
    private void Brisanje()
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

        var dbfPath = LdBolovanjeDbfSupport.PronadjiDbf(_folderPath, "ldpotvr.dbf");
        if (dbfPath is null)
        {
            Poruka = "ldpotvr.dbf nije pronađen.";
            return;
        }

        try
        {
            foreach (var red in DbfReader.CitajSveZapise(dbfPath))
            {
                Stavke.Add(new LdPotvrdaZaradaStavka
                {
                    Br = LdBolovanjeDbfSupport.Str(red, "BR"),
                    ImePrez = LdBolovanjeDbfSupport.Str(red, "IME_PREZ"),
                    Maticnibr = LdBolovanjeDbfSupport.Str(red, "MATICNIBR"),
                    Mesec = LdBolovanjeDbfSupport.Str(red, "MESEC"),
                    Bruto = LdBolovanjeDbfSupport.Dec(red, "BRUTO"),
                    Bruto2 = LdBolovanjeDbfSupport.Dec(red, "BRUTO2"),
                    Datispl = LdBolovanjeDbfSupport.Dat(red, "DATISPL"),
                    Preneto = LdBolovanjeDbfSupport.Str(red, "PRENETO"),
                    Idbr = LdBolovanjeDbfSupport.Long(red, "IDBR")
                });
            }

            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Ucitano {Stavke.Count} redova iz ldpotvr.dbf.";
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
                "ldpotvr.dbf",
                Stavke.ToList(),
                ResolveValue);
        }
        catch
        {
            // Poruka postavlja caller.
        }
    }

    private static object? ResolveValue(LdPotvrdaZaradaStavka row, string fieldName) => fieldName.ToUpperInvariant() switch
    {
        "BR" => LdBolovanjeDbfSupport.NormalizeText(row.Br),
        "IME_PREZ" => LdBolovanjeDbfSupport.NormalizeText(row.ImePrez),
        "MATICNIBR" => LdBolovanjeDbfSupport.NormalizeText(row.Maticnibr),
        "MESEC" => LdBolovanjeDbfSupport.NormalizeText(row.Mesec),
        "BRUTO" => row.Bruto,
        "BRUTO2" => row.Bruto2,
        "DATISPL" => row.Datispl,
        "PRENETO" => LdBolovanjeDbfSupport.NormalizeText(row.Preneto),
        "IDBR" => row.Idbr,
        _ => null
    };
}
