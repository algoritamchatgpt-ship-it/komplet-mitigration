using Algoritam.Application;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Algoritam.WPF.ViewModels;

public sealed class LdOz10Stavka
{
    public string Br { get; set; } = string.Empty;
    public string ImePrez { get; set; } = string.Empty;
    public string Pol { get; set; } = string.Empty;
    public string Prvaispl { get; set; } = string.Empty;
    public DateTime? Dod { get; set; }
    public DateTime? Ddo { get; set; }
    public decimal Bolest { get; set; }
    public decimal Povrad { get; set; }
    public decimal Profbol { get; set; }
    public decimal Nega65 { get; set; }
    public decimal Nega { get; set; }
    public decimal Izolac { get; set; }
    public decimal Davalac { get; set; }
    public decimal Trudnoca { get; set; }
    public decimal Bruto { get; set; }
    public decimal Dopr { get; set; }
    public decimal Dopf { get; set; }
    public decimal Porez { get; set; }
    public decimal Zaisplat { get; set; }
    public decimal Svega { get; set; }
    public string Preneto { get; set; } = string.Empty;
    public long Idbr { get; set; }
}

public sealed class LdOz10SpecStavka
{
    public string Kategorija { get; set; } = string.Empty;
    public decimal Dani { get; set; }
    public decimal Bruto { get; set; }
    public decimal Porez { get; set; }
    public decimal Dopr { get; set; }
    public decimal Dopf { get; set; }
    public decimal Zaisplat { get; set; }
    public decimal Svega { get; set; }
}

public partial class LdOz10ViewModel : ObservableObject
{
    private readonly string _folderPath;

    [ObservableProperty] private string _naslov = "OBRAZAC OZ 10 NAKNADA ZA BOLOVANJE";
    [ObservableProperty] private ObservableCollection<LdOz10Stavka> _stavke = [];
    [ObservableProperty] private LdOz10Stavka? _selektovana;
    [ObservableProperty] private string _poruka = string.Empty;

    public event Action? ZatvaranjeZatrazeno;

    public LdOz10ViewModel(AppState appState)
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

            var redni = Stavke.Count;
            foreach (var z in ldRows)
            {
                var broj = LdBolovanjeDbfSupport.Int(z, "BROJ");
                var radnik = radnici.TryGetValue(broj, out var rr) ? rr : null;

                var porez = LdBolovanjeDbfSupport.Dec(z, "POREZ");
                var neto = LdBolovanjeDbfSupport.Dec(z, "NETO");
                var dopsocf = LdBolovanjeDbfSupport.Dec(z, "DOPSOCF");
                var dopsocr = LdBolovanjeDbfSupport.Dec(z, "DOPSOCR");

                Stavke.Add(new LdOz10Stavka
                {
                    Br = (++redni).ToString(CultureInfo.InvariantCulture),
                    ImePrez = radnik is not null ? LdBolovanjeDbfSupport.Str(radnik, "IME_PREZ") : string.Empty,
                    Bruto = neto + porez + dopsocr,
                    Porez = porez,
                    Dopr = dopsocr,
                    Dopf = dopsocf,
                    Svega = neto + porez + dopsocf + dopsocr,
                    Zaisplat = neto
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
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za obrazac.";
            return;
        }

        var view = new Views.Zarade.LdBolGenericReportView(
            "LDOZ100 - OBRAZAC NAKNADA",
            Stavke.ToList(),
            Stavke.Count);
        view.ShowDialog();
    }

    [RelayCommand]
    private void Specifikacija()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za specifikaciju.";
            return;
        }

        decimal mBolest = 0m, mPovrad = 0m, mProfbol = 0m, mNega = 0m, mIzolac = 0m, mDavalac = 0m, mTrudnoca = 0m;
        decimal mBolestBruto = 0m, mPovradBruto = 0m, mProfbolBruto = 0m, mNegaBruto = 0m, mIzolacBruto = 0m, mDavalacBruto = 0m, mTrudnocaBruto = 0m;
        decimal mBruto = 0m, mPorez = 0m, mDopr = 0m, mDopf = 0m, mZaisplat = 0m, mSvega = 0m;

        foreach (var red in Stavke)
        {
            mBolest += red.Bolest;
            if (mBolest != 0) mBolestBruto += red.Bruto;

            mPovrad += red.Povrad;
            if (mPovrad != 0) mPovradBruto += red.Bruto;

            mProfbol += red.Profbol;
            if (mProfbol != 0) mProfbolBruto += red.Bruto;

            mNega += red.Nega;
            if (mNega != 0) mNegaBruto += red.Bruto;

            mIzolac += red.Izolac;
            if (mIzolac != 0) mIzolacBruto += red.Bruto;

            mDavalac += red.Davalac;
            if (mDavalac != 0) mDavalacBruto += red.Bruto;

            mTrudnoca += red.Trudnoca;
            if (mTrudnoca != 0) mTrudnocaBruto += red.Bruto;

            mBruto += red.Bruto;
            mPorez += red.Porez;
            mDopr += red.Dopr;
            mDopf += red.Dopf;
            mZaisplat += red.Zaisplat;
            mSvega += red.Svega;
        }

        var spec = new List<LdOz10SpecStavka>
        {
            new() { Kategorija = "BOLEST", Dani = mBolest, Bruto = mBolestBruto },
            new() { Kategorija = "POVRADA NA RADU", Dani = mPovrad, Bruto = mPovradBruto },
            new() { Kategorija = "PROFESIONALNO", Dani = mProfbol, Bruto = mProfbolBruto },
            new() { Kategorija = "NEGA", Dani = mNega, Bruto = mNegaBruto },
            new() { Kategorija = "IZOLACIJA", Dani = mIzolac, Bruto = mIzolacBruto },
            new() { Kategorija = "DAVALAC KRVI", Dani = mDavalac, Bruto = mDavalacBruto },
            new() { Kategorija = "TRUDNOCA", Dani = mTrudnoca, Bruto = mTrudnocaBruto },
            new()
            {
                Kategorija = "UKUPNO",
                Bruto = mBruto,
                Porez = mPorez,
                Dopr = mDopr,
                Dopf = mDopf,
                Zaisplat = mZaisplat,
                Svega = mSvega
            }
        };

        var view = new Views.Zarade.LdBolGenericReportView(
            "LDOZ10SPIS - SPECIFIKACIJA",
            spec,
            spec.Count);
        view.ShowDialog();
    }

    [RelayCommand]
    private void Dodaj()
    {
        var nova = new LdOz10Stavka
        {
            Br = (Stavke.Count + 1).ToString(CultureInfo.InvariantCulture)
        };

        Stavke.Add(nova);
        Selektovana = nova;
        Sacuvaj();
        Poruka = "Dodat je novi red.";
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
    private void Brisanje()
    {
        Stavke.Clear();
        Sacuvaj();
        Poruka = "Tabela je obrisana.";
        ZatvaranjeZatrazeno?.Invoke();
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

        var dbfPath = LdBolovanjeDbfSupport.PronadjiDbf(_folderPath, "ldoz10.dbf");
        if (dbfPath is null)
        {
            Poruka = "ldoz10.dbf nije pronađen.";
            return;
        }

        try
        {
            foreach (var z in DbfReader.CitajSveZapise(dbfPath))
            {
                Stavke.Add(new LdOz10Stavka
                {
                    Br = LdBolovanjeDbfSupport.Str(z, "BR"),
                    ImePrez = LdBolovanjeDbfSupport.Str(z, "IME_PREZ"),
                    Pol = LdBolovanjeDbfSupport.Str(z, "POL"),
                    Prvaispl = LdBolovanjeDbfSupport.Str(z, "PRVAISPL"),
                    Dod = LdBolovanjeDbfSupport.Dat(z, "DOD"),
                    Ddo = LdBolovanjeDbfSupport.Dat(z, "DDO"),
                    Bolest = LdBolovanjeDbfSupport.Dec(z, "BOLEST"),
                    Povrad = LdBolovanjeDbfSupport.Dec(z, "POVRAD"),
                    Profbol = LdBolovanjeDbfSupport.Dec(z, "PROFBOL"),
                    Nega65 = LdBolovanjeDbfSupport.Dec(z, "NEGA65"),
                    Nega = LdBolovanjeDbfSupport.Dec(z, "NEGA"),
                    Izolac = LdBolovanjeDbfSupport.Dec(z, "IZOLAC"),
                    Davalac = LdBolovanjeDbfSupport.Dec(z, "DAVALAC"),
                    Trudnoca = LdBolovanjeDbfSupport.Dec(z, "TRUDNOCA"),
                    Bruto = LdBolovanjeDbfSupport.Dec(z, "BRUTO"),
                    Dopr = LdBolovanjeDbfSupport.Dec(z, "DOPR"),
                    Dopf = LdBolovanjeDbfSupport.Dec(z, "DOPF"),
                    Porez = LdBolovanjeDbfSupport.Dec(z, "POREZ"),
                    Zaisplat = LdBolovanjeDbfSupport.Dec(z, "ZAISPLAT"),
                    Svega = LdBolovanjeDbfSupport.Dec(z, "SVEGA"),
                    Preneto = LdBolovanjeDbfSupport.Str(z, "PRENETO"),
                    Idbr = LdBolovanjeDbfSupport.Long(z, "IDBR")
                });
            }

            Selektovana = Stavke.FirstOrDefault();
            Poruka = $"Ucitano {Stavke.Count} redova iz ldoz10.dbf.";
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
                "ldoz10.dbf",
                Stavke.ToList(),
                ResolveValue);
        }
        catch
        {
            // Poruka postavlja caller.
        }
    }

    private static object? ResolveValue(LdOz10Stavka row, string fieldName) => fieldName.ToUpperInvariant() switch
    {
        "BR" => LdBolovanjeDbfSupport.NormalizeText(row.Br),
        "IME_PREZ" => LdBolovanjeDbfSupport.NormalizeText(row.ImePrez),
        "POL" => LdBolovanjeDbfSupport.NormalizeText(row.Pol),
        "PRVAISPL" => LdBolovanjeDbfSupport.NormalizeText(row.Prvaispl),
        "DOD" => row.Dod,
        "DDO" => row.Ddo,
        "BOLEST" => row.Bolest,
        "POVRAD" => row.Povrad,
        "PROFBOL" => row.Profbol,
        "NEGA65" => row.Nega65,
        "NEGA" => row.Nega,
        "IZOLAC" => row.Izolac,
        "DAVALAC" => row.Davalac,
        "TRUDNOCA" => row.Trudnoca,
        "BRUTO" => row.Bruto,
        "DOPR" => row.Dopr,
        "DOPF" => row.Dopf,
        "POREZ" => row.Porez,
        "ZAISPLAT" => row.Zaisplat,
        "SVEGA" => row.Svega,
        "PRENETO" => LdBolovanjeDbfSupport.NormalizeText(row.Preneto),
        "IDBR" => row.Idbr,
        _ => null
    };
}
