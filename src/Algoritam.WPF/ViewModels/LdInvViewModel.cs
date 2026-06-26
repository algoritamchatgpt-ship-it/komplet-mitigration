using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Algoritam.WPF.ViewModels;

public sealed class LdInvStavka
{
    public string Br { get; set; } = string.Empty;
    public string ImePrez { get; set; } = string.Empty;
    public string Maticnibr { get; set; } = string.Empty;
    public decimal Casovi { get; set; }
    public decimal Nak2003 { get; set; }
    public decimal Pros2002 { get; set; }
    public decimal Procuskl { get; set; }
    public decimal Uskladj { get; set; }
    public decimal Neuskladj { get; set; }
    public decimal Uskladj2 { get; set; }
    public decimal Ostvarcas { get; set; }
    public decimal Neto { get; set; }
    public decimal Porez { get; set; }
    public decimal Dopzdr { get; set; }
    public decimal Svega { get; set; }
    public string Preneto { get; set; } = string.Empty;
    public long Idbr { get; set; }
}

public partial class LdInvViewModel : ObservableObject
{
    private readonly string _folderPath;

    [ObservableProperty] private string _naslov = "OBRAZAC II NAKNADA ZA INVALIDE";
    [ObservableProperty] private ObservableCollection<LdInvStavka> _stavke = [];
    [ObservableProperty] private LdInvStavka? _selektovana;
    [ObservableProperty] private string _poruka = string.Empty;

    public event Action? ZatvaranjeZatrazeno;

    public LdInvViewModel(AppState appState)
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
            var cmes = param is null ? 0m : LdBolovanjeDbfSupport.Dec(param, "CMES");
            var mesec = param is null ? 0 : LdBolovanjeDbfSupport.Int(param, "MESEC");
            var isplata = param is null ? 0 : LdBolovanjeDbfSupport.Int(param, "ISPLATA");
            var godina = param is null ? string.Empty : LdBolovanjeDbfSupport.Str(param, "GODINA");
            var ldRowsPeriod = ldRows
                .Where(r => LdBolovanjeDbfSupport.OdgovaraPeriodu(r, mesec, isplata, godina))
                .ToList();

            foreach (var red in ldRowsPeriod)
            {
                var broj = LdBolovanjeDbfSupport.Int(red, "BROJ");
                var radnik = radnici.TryGetValue(broj, out var r) ? r : null;

                var neto = LdBolovanjeDbfSupport.Dec(red, "NETO");
                var porez = LdBolovanjeDbfSupport.Dec(red, "POREZ");
                var dopzdr = LdBolovanjeDbfSupport.Dec(red, "DOPZF");
                var svega = neto + porez + dopzdr;

                Stavke.Add(new LdInvStavka
                {
                    Br = (Stavke.Count + 1).ToString(CultureInfo.InvariantCulture),
                    ImePrez = radnik is null ? string.Empty : LdBolovanjeDbfSupport.Str(radnik, "IME_PREZ"),
                    Maticnibr = radnik is null ? string.Empty : LdBolovanjeDbfSupport.Str(radnik, "MATICNIBR"),
                    Neto = neto,
                    Porez = porez,
                    Dopzdr = dopzdr,
                    Svega = svega,
                    Casovi = cmes,
                    Ostvarcas = cmes
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
    private void ObrazacII()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za obrazac.";
            return;
        }

        var view = new Views.Zarade.LdBolGenericReportView(
            "LDINV0 - OBRAZAC II",
            Stavke.ToList(),
            Stavke.Count);
        view.ShowDialog();
    }

    [RelayCommand]
    private void Dodaj()
    {
        var nova = new LdInvStavka
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

        var dbfPath = LdBolovanjeDbfSupport.PronadjiDbf(_folderPath, "ldinv.dbf");
        if (dbfPath is null)
        {
            Poruka = "ldinv.dbf nije pronađen.";
            return;
        }

        try
        {
            foreach (var red in DbfReader.CitajSveZapise(dbfPath))
            {
                Stavke.Add(new LdInvStavka
                {
                    Br = LdBolovanjeDbfSupport.Str(red, "BR"),
                    ImePrez = LdBolovanjeDbfSupport.Str(red, "IME_PREZ"),
                    Maticnibr = LdBolovanjeDbfSupport.Str(red, "MATICNIBR"),
                    Casovi = LdBolovanjeDbfSupport.Dec(red, "CASOVI"),
                    Nak2003 = LdBolovanjeDbfSupport.Dec(red, "NAK2003"),
                    Pros2002 = LdBolovanjeDbfSupport.Dec(red, "PROS2002"),
                    Procuskl = LdBolovanjeDbfSupport.Dec(red, "PROCUSKL"),
                    Uskladj = LdBolovanjeDbfSupport.Dec(red, "USKLADJ"),
                    Neuskladj = LdBolovanjeDbfSupport.Dec(red, "NEUSKLADJ"),
                    Uskladj2 = LdBolovanjeDbfSupport.Dec(red, "USKLADJ2"),
                    Ostvarcas = LdBolovanjeDbfSupport.Dec(red, "OSTVARCAS"),
                    Neto = LdBolovanjeDbfSupport.Dec(red, "NETO"),
                    Porez = LdBolovanjeDbfSupport.Dec(red, "POREZ"),
                    Dopzdr = LdBolovanjeDbfSupport.Dec(red, "DOPZDR"),
                    Svega = LdBolovanjeDbfSupport.Dec(red, "SVEGA"),
                    Preneto = LdBolovanjeDbfSupport.Str(red, "PRENETO"),
                    Idbr = LdBolovanjeDbfSupport.Long(red, "IDBR")
                });
            }

            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Ucitano {Stavke.Count} redova iz ldinv.dbf.";
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
                "ldinv.dbf",
                Stavke.ToList(),
                ResolveValue);
        }
        catch
        {
            // Poruku postavlja caller.
        }
    }

    private static object? ResolveValue(LdInvStavka row, string fieldName) => fieldName.ToUpperInvariant() switch
    {
        "BR" => LdBolovanjeDbfSupport.NormalizeText(row.Br),
        "IME_PREZ" => LdBolovanjeDbfSupport.NormalizeText(row.ImePrez),
        "MATICNIBR" => LdBolovanjeDbfSupport.NormalizeText(row.Maticnibr),
        "CASOVI" => row.Casovi,
        "NAK2003" => row.Nak2003,
        "PROS2002" => row.Pros2002,
        "PROCUSKL" => row.Procuskl,
        "USKLADJ" => row.Uskladj,
        "NEUSKLADJ" => row.Neuskladj,
        "USKLADJ2" => row.Uskladj2,
        "OSTVARCAS" => row.Ostvarcas,
        "NETO" => row.Neto,
        "POREZ" => row.Porez,
        "DOPZDR" => row.Dopzdr,
        "SVEGA" => row.Svega,
        "PRENETO" => LdBolovanjeDbfSupport.NormalizeText(row.Preneto),
        "IDBR" => row.Idbr,
        _ => null
    };
}
