using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml.Linq;

namespace Algoritam.WPF.ViewModels;

public partial class PppPreuzimanjeViewModel : ObservableObject
{
    private readonly string _folderPath;
    private string? _xm2LdPath;
    private string? _xm2LdSvePath;
    private string? _xm2PzarPath;
    private DbfTableWriter.DbfSchema? _xm2LdSchema;

    [ObservableProperty] private ObservableCollection<PppLdStavka> _stavke = [];
    [ObservableProperty] private PppLdStavka? _selektovanaStavka;
    [ObservableProperty] private string _firmaNaziv = "FIRMA";
    [ObservableProperty] private string _firmaMesto = string.Empty;
    [ObservableProperty] private string _poruka = string.Empty;
    [ObservableProperty] private string _stranaTekst = "1 / 2";

    public Action? ZatvoriAction { get; set; }
    public Action? OtvoriParametrePrenosaAction { get; set; }

    public PppPreuzimanjeViewModel(string folderPath)
    {
        _folderPath = folderPath ?? string.Empty;
        Ucitaj();
    }

    public void SacuvajNaDisk(bool prijaviPoruku = true)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_xm2LdPath))
            {
                if (prijaviPoruku)
                    Poruka = "Nije pronađen xm2ld.dbf.";
                return;
            }

            _xm2LdSchema ??= DbfTableWriter.LoadSchema(_xm2LdPath);
            var rows = Stavke.Select(s => s.ToRowDictionary()).ToList();

            DbfTableWriter.WriteTable(
                _xm2LdPath,
                _xm2LdSchema,
                rows,
                static (r, fieldName) => r.TryGetValue(fieldName, out var value) ? value : null);

            if (prijaviPoruku)
                Poruka = "Podaci su sačuvani u xm2ld.dbf.";
        }
        catch (Exception ex)
        {
            if (prijaviPoruku)
                Poruka = $"Greska pri cuvanju xm2ld.dbf: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Osvezi() => Ucitaj();

    [RelayCommand]
    private void DodajR() => DodajSaTipom("R");

    [RelayCommand]
    private void DodajF() => DodajSaTipom("F");

    [RelayCommand]
    private void DodajV() => DodajSaTipom("V");

    [RelayCommand]
    private void DodajSve()
    {
        var deklaracija = OdrediDeklaraciju();
        var redoviDeklaracije = string.IsNullOrWhiteSpace(deklaracija)
            ? Stavke.Count
            : Stavke.Count(s => string.Equals(Safe(s.Deklarac), Safe(deklaracija), StringComparison.OrdinalIgnoreCase));

        var zaDodavanje = redoviDeklaracije switch
        {
            0 => 8,
            _ when redoviDeklaracije % 8 == 0 => 0,
            _ => 8 - (redoviDeklaracije % 8)
        };

        if (zaDodavanje <= 0)
        {
            Poruka = "Aktivna strana je vec popunjena (8 redova).";
            return;
        }

        var tip = Safe(SelektovanaStavka?.Radnik);
        if (!string.Equals(tip, "R", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(tip, "F", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(tip, "V", StringComparison.OrdinalIgnoreCase))
        {
            tip = "R";
        }

        for (var i = 0; i < zaDodavanje; i++)
            DodajSaTipom(tip);

        SacuvajNaDisk(false);
        Poruka = $"Dodato {zaDodavanje} redova (tip: {tip}) da se popuni strana.";
    }

    private void DodajSaTipom(string radnikTip)
    {
        var deklaracija = SelektovanaStavka?.Deklarac ?? UcitajDeklaracijuIzPzar();
        if (string.IsNullOrWhiteSpace(deklaracija))
            deklaracija = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        var nextBroj = Stavke.Count + 1;

        var nova = new PppLdStavka
        {
            Deklarac = deklaracija,
            RedniBroj = nextBroj.ToString(CultureInfo.InvariantCulture),
            FondSati = SelektovanaStavka?.FondSati ?? UcitajFondSatiIzPzar(),
            BrojDana = SelektovanaStavka?.BrojDana ?? UcitajBrojDanaIzPzar(),
            BrojZapos = SelektovanaStavka?.BrojZapos ?? UcitajBrojZaposIzPzar(),
            Konacna = SelektovanaStavka?.Konacna ?? UcitajKonacnaIzPzar(),
            VrstaPrijave = SelektovanaStavka?.VrstaPrijave ?? UcitajVrstuPrijaveIzPzar(),
            Godina = SelektovanaStavka?.Godina ?? UcitajGodinuIzPzar(),
            IsplataZaMesec = SelektovanaStavka?.IsplataZaMesec ?? UcitajMesecIzPzar(),
            Radnik = radnikTip,
            VrstaId = radnikTip == "R" ? "1" : "0"
        };

        Stavke.Add(nova);
        SelektovanaStavka = nova;
        Poruka = $"Dodat je novi red (tip: {radnikTip}).";
    }

    [RelayCommand]
    private void Obrisi()
    {
        if (SelektovanaStavka is null)
        {
            Poruka = "Izaberite red za brisanje.";
            return;
        }

        Stavke.Remove(SelektovanaStavka);
        SelektovanaStavka = Stavke.Count > 0 ? Stavke[Math.Max(Stavke.Count - 1, 0)] : null;
        Poruka = "Red je obrisan.";
    }

    [RelayCommand]
    private void ObrisiSve()
    {
        if (MessageBox.Show(
            "Obrisati sve redove u tabeli xm2ld?",
            "PPP-PD",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        Stavke.Clear();
        SelektovanaStavka = null;
        Poruka = "Svi redovi su obrisani.";
    }

    [RelayCommand]
    private void BrisiMesec()
    {
        var dostupne = Stavke
            .Select(s => Safe(s.Deklarac))
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (dostupne.Count == 0)
        {
            Poruka = "Nema deklaracija za brisanje.";
            return;
        }

        var predlog = SelektovanaStavka is not null && !string.IsNullOrWhiteSpace(SelektovanaStavka.Deklarac)
            ? SelektovanaStavka.Deklarac.Trim()
            : dostupne.Last();

        var odabrana = PromptText(
            "Brisanje meseca",
            $"Unesite broj deklaracije za brisanje:\n(dostupno: {string.Join(", ", dostupne)})",
            predlog);

        if (string.IsNullOrWhiteSpace(odabrana))
            return;

        if (MessageBox.Show(
            $"Obrisati sve redove deklaracije  \"{odabrana}\"?",
            "Brisanje meseca",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var za_brisanje = Stavke
            .Where(s => string.Equals(Safe(s.Deklarac), Safe(odabrana), StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var s in za_brisanje)
            Stavke.Remove(s);

        SelektovanaStavka = Stavke.Count > 0 ? Stavke[0] : null;
        SacuvajNaDisk(false);
        Poruka = $"Obrisano {za_brisanje.Count} redova (deklaracija: {odabrana}).";
    }

    [RelayCommand]
    private void Prvi()
    {
        if (Stavke.Count > 0)
            SelektovanaStavka = Stavke[0];
    }

    [RelayCommand]
    private void Zadnji()
    {
        if (Stavke.Count > 0)
            SelektovanaStavka = Stavke[^1];
    }

    [RelayCommand]
    private void Gore()
    {
        if (Stavke.Count == 0 || SelektovanaStavka is null)
            return;

        var idx = Stavke.IndexOf(SelektovanaStavka);
        if (idx > 0)
            SelektovanaStavka = Stavke[idx - 1];
    }

    [RelayCommand]
    private void Dole()
    {
        if (Stavke.Count == 0 || SelektovanaStavka is null)
            return;

        var idx = Stavke.IndexOf(SelektovanaStavka);
        if (idx >= 0 && idx < Stavke.Count - 1)
            SelektovanaStavka = Stavke[idx + 1];
    }

    [RelayCommand]
    private void ParametriPrenosa()
    {
        OtvoriParametrePrenosaAction?.Invoke();
        Ucitaj();
    }

    [RelayCommand]
    private void PopuniObrazac()
    {
        var ldarhivaPath = PronadjiDbf(_folderPath, "ldarhiva.dbf");
        if (string.IsNullOrWhiteSpace(ldarhivaPath) || !File.Exists(ldarhivaPath))
        {
            Poruka = "Nije pronađen ldarhiva.dbf. Pokrenite obračun zarada najpre.";
            return;
        }

        var ldpppPath = PronadjiIliKreirajDbf("ldppp.dbf");
        if (string.IsNullOrWhiteSpace(ldpppPath))
        {
            Poruka = "Nije pronađen niti kreiran ldppp.dbf.";
            return;
        }

        var mesecText = PromptText(
            "Popuni ldppp.dbf",
            "Unesite broj meseca za preuzimanje iz arhive (1-12):",
            DateTime.Now.Month.ToString(CultureInfo.InvariantCulture));

        if (string.IsNullOrWhiteSpace(mesecText))
            return;

        if (!int.TryParse(mesecText.Trim(), out var mesec) || mesec < 1 || mesec > 12)
        {
            Poruka = "Neispravan broj meseca.";
            return;
        }

        var arhivaRows = DbfReader.CitajSveZapise(ldarhivaPath);
        var zaOvajMesec = arhivaRows.Where(r => (int)Dec(r, "MESEC") == mesec).ToList();

        if (zaOvajMesec.Count == 0)
        {
            // ldarhiva.dbf nema podatke za ovaj mesec — čitamo direktno iz aktivnog LD*.dbf
            zaOvajMesec = CitajLdDbfZaMesec(_folderPath, mesec);
            if (zaOvajMesec.Count == 0)
            {
                Poruka = $"Nema zapisa za mesec {mesec} ni u ldarhiva.dbf ni u LD*.dbf. Proverite da li je obracun pokrenut.";
                return;
            }
        }

        var existingRows = File.Exists(ldpppPath)
            ? DbfReader.CitajSveZapise(ldpppPath)
            : new List<Dictionary<string, object?>>();

        var hasMesec = existingRows.Any(r => (int)Dec(r, "MESEC") == mesec);
        if (hasMesec && MessageBox.Show(
                $"U ldppp.dbf vec postoje podaci za mesec {mesec}. Zameni ih?",
                "Popuni ldppp",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        if (hasMesec)
            existingRows = existingRows.Where(r => (int)Dec(r, "MESEC") != mesec).ToList();

        var ldradByBroj = UcitajLdradByBroj();

        foreach (var row in zaOvajMesec)
        {
            var bruto = Dec(row, "BRUTO");
            var poroslob1 = Dec(row, "POROSLOB1");
            var poroslob2 = Dec(row, "POROSLOB2");
            var poroslob3 = Dec(row, "POROSLOB3");
            var poroslob4 = Dec(row, "POROSLOB4");
            var porezu = Dec(row, "POREZU");
            var porezArhiva = Dec(row, "POREZ");
            var dopsocr = Dec(row, "DOPSOCR");
            var dopsocf = Dec(row, "DOPSOCF");
            var doppr = Dec(row, "DOPPR");
            var dopzr = Dec(row, "DOPZR");
            var dopnr = Dec(row, "DOPNR");
            var sifraprih = Str(row, "SIFRAPRIH");
            var broj = NumAsText(row, "BROJ");

            var ldrad = (broj.Length > 0 && ldradByBroj.TryGetValue(broj, out var lr))
                ? lr
                : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            var imePrez = PrvaNeprazna(Str(row, "IME_PREZ"), Str(ldrad, "IME_PREZ"));
            var maticnibr = PrvaNeprazna(Str(row, "MATICNIBR"), Str(ldrad, "MATICNIBR"));
            if (string.IsNullOrWhiteSpace(sifraprih))
                sifraprih = Str(ldrad, "SIFRAPRIH");

            var brutold = Math.Round(bruto - poroslob1 - poroslob2 - poroslob3 - poroslob4, 0);
            var porezUkupno = Math.Round(porezArhiva + porezu, 0);
            var neto = Math.Round(bruto - porezArhiva - doppr - dopzr - dopnr, 0);

            existingRows.Add(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["BROJ"] = Dec(row, "BROJ"),
                ["MESEC"] = Dec(row, "MESEC"),
                ["IME_PREZ"] = imePrez,
                ["MATICNIBR"] = maticnibr,
                ["MATICNIB"] = maticnibr,
                ["SIFRAPRIH"] = sifraprih,
                ["BRUTOLD"] = brutold,
                ["UKBRUTOLD"] = Math.Round(bruto, 0),
                ["POREZ"] = porezUkupno,
                ["DOPSOC"] = Math.Round(dopsocr, 0),
                ["PENSOC"] = Math.Round(doppr, 0),
                ["ZDRSOC"] = Math.Round(dopzr, 0),
                ["NEZSOC"] = Math.Round(dopnr, 0),
                ["DOPSOCF"] = Math.Round(dopsocf, 0),
                ["NETO"] = neto,
                ["OBRAZAC"] = "plata",
                ["PROCPOR"] = 10m,
                ["RADNIK"] = "R"
            });
        }

        var schema = DbfTableWriter.LoadSchema(ldpppPath);
        DbfTableWriter.WriteTable(
            ldpppPath,
            schema,
            existingRows,
            static (r, fieldName) => r.TryGetValue(fieldName, out var value) ? value : null);

        Poruka = $"Popunjeno {zaOvajMesec.Count} zapisa u ldppp.dbf za mesec {mesec:00}. Sada kliknite '2. PREUZMI ZARADE'.";
    }

    [RelayCommand]
    private void PreuzmiZarade()
    {
        var ldpppPath = PronadjiDbf(_folderPath, "ldppp.dbf");
        if (string.IsNullOrWhiteSpace(ldpppPath) || !File.Exists(ldpppPath))
        {
            Poruka = "Nije pronađen ldppp.dbf za preuzimanje zarada.";
            return;
        }

        var pzar = UcitajPrviRed(_xm2PzarPath);
        var deklaracija = PrvaNeprazna(Str(pzar, "DEKLARAC"), UcitajDeklaracijuIzPzar());
        if (string.IsNullOrWhiteSpace(deklaracija))
            deklaracija = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        var ldradByBroj = UcitajLdradByBroj();
        var ldradByMaticni = UcitajLdradByMaticni();
        var ldpppRows = DbfReader.CitajSveZapise(ldpppPath);

        if (ldpppRows.Count == 0)
        {
            Poruka = "ldppp.dbf je prazan, nema podataka za preuzimanje.";
            return;
        }

        var stari = Stavke.Where(s => string.Equals(Safe(s.Deklarac), Safe(deklaracija), StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var s in stari)
            Stavke.Remove(s);

        var dodato = 0;
        foreach (var row in ldpppRows)
        {
            var brojText = NumAsText(row, "BROJ");
            var maticni = PrvaNeprazna(Str(row, "MATICNIBR"), Str(row, "MATICNIB"));

            var ldrad = (brojText.Length > 0 && ldradByBroj.TryGetValue(brojText, out var poBroju))
                ? poBroju
                : (ldradByMaticni.TryGetValue(maticni, out var poMaticnom) ? poMaticnom : new Dictionary<string, object?>());

            var prezime = PrvaNeprazna(Str(row, "PREZIME"), Str(ldrad, "PREZIME"));
            var ime = PrvaNeprazna(Str(row, "IME"), Str(ldrad, "IME"));
            if (string.IsNullOrWhiteSpace(prezime) && string.IsNullOrWhiteSpace(ime))
                SplitImePrezime(Str(row, "IME_PREZ"), out prezime, out ime);

            var bruto = Dec(row, "BRUTOLD");
            var oporezivo = Dec(row, "OPOREZIVO");
            var porez = Dec(row, "POREZ");
            var pio = PrvaVecaOdNule(Dec(row, "PENSOC"), Dec(row, "DOPSOC"));
            var zdr = Dec(row, "ZDRSOC");
            var nez = Dec(row, "NEZSOC");
            var pioben = 0m;

            var stavka = new PppLdStavka
            {
                Deklarac = deklaracija,
                RedniBroj = PrvaNeprazna(brojText, (dodato + 1).ToString(CultureInfo.InvariantCulture)),
                Prezime = prezime,
                Ime = ime,
                VrstaId = PrvaNeprazna(Str(ldrad, "VRSTAID"), "1"),
                Prebival = Str(ldrad, "PREBIVAL"),
                SifraPrih = Str(row, "SIFRAPRIH"),
                MaticniBr = maticni,
                BrojDana = PrvaNeprazna(Str(pzar, "DANA"), string.Empty),
                BrojSati = PrvaNeprazna(Str(pzar, "FONDSATI"), string.Empty),
                FondSati = PrvaNeprazna(Str(pzar, "FONDSATI"), string.Empty),
                Bruto = FormatDec(bruto),
                OsnovicaP = FormatDec(oporezivo),
                Porez = FormatDec(porez),
                OsnovicaDop = FormatDec(oporezivo > 0m ? oporezivo : bruto),
                Pio = FormatDec(pio),
                Zdr = FormatDec(zdr),
                Nez = FormatDec(nez),
                PioBen = FormatDec(pioben),
                Mfp1 = FormatDec(Dec(row, "PROCPOR")),
                Mfp2 = string.Empty,
                Mfp3Proc = Str(ldrad, "MFP3PROC"),
                Mfp4 = string.Empty,
                Mfp5 = string.Empty,
                Mfp6 = string.Empty,
                Mfp7 = string.Empty,
                Mfp8Nepun = string.Empty,
                Mfp9NajOsn = string.Empty,
                Mfp10Dvez = Str(ldrad, "MFP10DVEZ"),
                Mfp11Neop = string.Empty,
                Mfp12 = string.Empty,
                VrstaPrijave = Str(pzar, "VRSTAPRIJ"),
                Godina = Str(pzar, "GODINA"),
                IsplataZaMesec = Str(pzar, "ISPZAMES"),
                Konacna = Str(pzar, "KONACNA"),
                DatumObaveze = Dat(row: pzar, "DATUMOBAV"),
                DatumPlacanja = Dat(row: pzar, "DATUMPLAC"),
                DatumObavezeC = FormatDatC(Dat(row: pzar, "DATUMOBAV")),
                DatumPlacanjaC = FormatDatC(Dat(row: pzar, "DATUMPLAC")),
                VrstaIzmene = Str(pzar, "VRSTAIZM"),
                Identifikacija = Str(pzar, "IDENTIFIK"),
                BrojResenja = Str(pzar, "BROJRES"),
                Osnov = Str(pzar, "OSNOV"),
                TipIsplate = Str(pzar, "TIPISP"),
                VrstaIdIsplatioca = Str(pzar, "VRSTAIDISP"),
                Najniza = Str(pzar, "PROPISANAO"),
                Pib = Str(pzar, "PIB"),
                Dana = Str(pzar, "DANA"),
                BrojZapos = Str(pzar, "BROJZAPOS"),
                JmbgPodnosioca = Str(pzar, "JMBGPODNOS"),
                Maticni = Str(pzar, "MATICNI"),
                Naziv = Str(pzar, "NAZIV"),
                Sediste = Str(pzar, "SEDISTE"),
                Telefon = Str(pzar, "TELEFON"),
                UlicaIBr = Str(pzar, "ULICAIBR"),
                Email = Str(pzar, "EMAIL"),
                Period = Str(pzar, "PERIOD"),
                RedniIsplate = NumAsText(pzar, "REDISPL"),
                Opstina = Str(pzar, "OPSTINA"),
                Radnik = PrvaNeprazna(Str(row, "RADNIK"), "R"),
                Mesto = PrvaNeprazna(Str(row, "NAZIV"), Str(ldrad, "MESTO")),
                Adresa = PrvaNeprazna(Str(row, "ADRESA"), Str(ldrad, "ADRESA")),
                Preneto = string.Empty,
                PioR = FormatDec(pio),
                ZdrR = FormatDec(zdr),
                NezR = FormatDec(nez),
                PioF = string.Empty,
                ZdrF = string.Empty,
                NezF = string.Empty,
                NBruto = bruto,
                NOsnovicaP = oporezivo,
                NPorez = porez,
                NOsnovicaDop = oporezivo > 0m ? oporezivo : bruto,
                NPio = pio,
                NZdr = zdr,
                NNez = nez,
                NPioben = pioben
            };

            Stavke.Add(stavka);
            dodato++;
        }

        if (Stavke.Count > 0)
            SelektovanaStavka = Stavke[0];

        SacuvajNaDisk(false);
        Poruka = $"Preuzeto {dodato} redova iz ldppp.dbf.";
    }

    [RelayCommand]
    private void PreuzmiOpjOdp()
    {
        if (Stavke.Count == 0 && MessageBox.Show(
                "Tabela PPP-PD je prazna.\nPreuzeti OPJ/ODP redove direktno u PPP-PD?",
                "Preuzimanje OPJ/ODP",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        var deklaracija = OdrediDeklaraciju();
        if (string.IsNullOrWhiteSpace(deklaracija))
            deklaracija = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        var pzar = UcitajPrviRed(_xm2PzarPath);
        var ldradByBroj = UcitajLdradByBroj();
        var ldradByMaticni = UcitajLdradByMaticni();
        var trazeniMesec = (int)Dec(pzar, "ISPZAMES");

        var prepoznateTabele = new[]
        {
            "ldopj1n.dbf",
            "ldopj2.dbf",
            "ldopj3.dbf",
            "ldopj4.dbf",
            "ldopj5.dbf",
            "ldopj6.dbf",
            "ldopj7.dbf",
            "ldopj8.dbf",
            "ldppodp.dbf",
            "ldppodo.dbf"
        };

        var postojeciKljucevi = new HashSet<string>(
            Stavke
                .Where(s => string.Equals(Safe(s.Deklarac), Safe(deklaracija), StringComparison.OrdinalIgnoreCase))
                .Select(KljucImporta),
            StringComparer.OrdinalIgnoreCase);

        var nextRedni = Stavke
            .Where(s => string.Equals(Safe(s.Deklarac), Safe(deklaracija), StringComparison.OrdinalIgnoreCase))
            .Select(s => int.TryParse(Safe(s.RedniBroj), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var pronadjenihTabela = 0;
        var procitanihRedova = 0;
        var dodatihRedova = 0;

        foreach (var fileName in prepoznateTabele)
        {
            var path = PronadjiDbf(_folderPath, fileName);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                continue;

            pronadjenihTabela++;
            var rows = DbfReader.CitajSveZapise(path);
            if (rows.Count == 0)
                continue;

            var zaMesec = trazeniMesec > 0
                ? rows.Where(r => !r.ContainsKey("MESEC") || (int)Dec(r, "MESEC") == trazeniMesec).ToList()
                : rows;

            foreach (var row in zaMesec)
            {
                procitanihRedova++;
                var nova = MapirajOpjOdpUSifru(
                    row,
                    deklaracija,
                    pzar,
                    ldradByBroj,
                    ldradByMaticni,
                    nextRedni);

                if (nova is null)
                    continue;

                var kljuc = KljucImporta(nova);
                if (!postojeciKljucevi.Add(kljuc))
                    continue;

                Stavke.Add(nova);
                nextRedni++;
                dodatihRedova++;
            }
        }

        if (dodatihRedova > 0)
        {
            SelektovanaStavka = Stavke.LastOrDefault();
            SacuvajNaDisk(false);
        }

        if (pronadjenihTabela == 0)
        {
            Poruka = "Nije pronađena nijedna OPJ/ODP tabela za prenos.";
            return;
        }

        if (dodatihRedova == 0)
        {
            Poruka = $"OPJ/ODP tabele su pronađene ({pronadjenihTabela}), ali nema novih redova za prenos (pročitano: {procitanihRedova}).";
            return;
        }

        Poruka = $"Preuzeto iz OPJ/ODP: {dodatihRedova} novih redova (pročitano: {procitanihRedova}, tabele: {pronadjenihTabela}).";
    }

    [RelayCommand]
    private void PopuniOpstePodatke()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema redova za popunu opštih podataka.";
            return;
        }

        var pzar = UcitajPrviRed(_xm2PzarPath);
        var ldradByBroj = UcitajLdradByBroj();
        var ldradByMaticni = UcitajLdradByMaticni();
        var redovi = UcitajAktivneStavke();

        var izmenjeno = 0;
        foreach (var s in redovi)
        {
            var changed = false;

            var ldrad = NadjiLdradZaStavku(s, ldradByBroj, ldradByMaticni);
            var prezime = Str(ldrad, "PREZIME");
            var ime = Str(ldrad, "IME");
            if (string.IsNullOrWhiteSpace(prezime) && string.IsNullOrWhiteSpace(ime))
                SplitImePrezime(Str(ldrad, "IME_PREZ"), out prezime, out ime);

            changed |= PopuniAkoPrazno(s.Prezime, prezime, v => s.Prezime = v);
            changed |= PopuniAkoPrazno(s.Ime, ime, v => s.Ime = v);
            changed |= PopuniAkoPrazno(s.MaticniBr, Str(ldrad, "MATICNIBR"), v => s.MaticniBr = v);
            changed |= PopuniAkoPrazno(s.SifraPrih, Str(ldrad, "SIFRAPRIH"), v => s.SifraPrih = v);
            changed |= PopuniAkoPrazno(s.VrstaId, Str(ldrad, "VRSTAID"), v => s.VrstaId = v);
            changed |= PopuniAkoPrazno(s.Prebival, Str(ldrad, "PREBIVAL"), v => s.Prebival = v);
            changed |= PopuniAkoPrazno(s.Opstina, PrvaNeprazna(Str(pzar, "OPSTINA"), Str(ldrad, "OPSTINA")), v => s.Opstina = v);
            changed |= PopuniAkoPrazno(s.Adresa, Str(ldrad, "ADRESA"), v => s.Adresa = v);
            changed |= PopuniAkoPrazno(s.Mesto, Str(ldrad, "MESTO"), v => s.Mesto = v);

            changed |= PopuniAkoPrazno(s.Pib, Str(pzar, "PIB"), v => s.Pib = v);
            changed |= PopuniAkoPrazno(s.JmbgPodnosioca, Str(pzar, "JMBGPODNOS"), v => s.JmbgPodnosioca = v);
            changed |= PopuniAkoPrazno(s.Maticni, Str(pzar, "MATICNI"), v => s.Maticni = v);
            changed |= PopuniAkoPrazno(s.Naziv, Str(pzar, "NAZIV"), v => s.Naziv = v);
            changed |= PopuniAkoPrazno(s.Sediste, Str(pzar, "SEDISTE"), v => s.Sediste = v);
            changed |= PopuniAkoPrazno(s.Telefon, Str(pzar, "TELEFON"), v => s.Telefon = v);
            changed |= PopuniAkoPrazno(s.UlicaIBr, Str(pzar, "ULICAIBR"), v => s.UlicaIBr = v);
            changed |= PopuniAkoPrazno(s.Email, Str(pzar, "EMAIL"), v => s.Email = v);
            changed |= PopuniAkoPrazno(s.Period, Str(pzar, "PERIOD"), v => s.Period = v);
            changed |= PopuniAkoPrazno(s.RedniIsplate, NumAsText(pzar, "REDISPL"), v => s.RedniIsplate = v);
            changed |= PopuniAkoPrazno(s.BrojDana, Str(pzar, "DANA"), v => s.BrojDana = v);
            changed |= PopuniAkoPrazno(s.BrojSati, Str(pzar, "FONDSATI"), v => s.BrojSati = v);
            changed |= PopuniAkoPrazno(s.FondSati, Str(pzar, "FONDSATI"), v => s.FondSati = v);
            changed |= PopuniAkoPrazno(s.Dana, Str(pzar, "DANA"), v => s.Dana = v);
            changed |= PopuniAkoPrazno(s.BrojZapos, Str(pzar, "BROJZAPOS"), v => s.BrojZapos = v);
            changed |= PopuniAkoPrazno(s.Godina, Str(pzar, "GODINA"), v => s.Godina = v);
            changed |= PopuniAkoPrazno(s.IsplataZaMesec, Str(pzar, "ISPZAMES"), v => s.IsplataZaMesec = v);
            changed |= PopuniAkoPrazno(s.Konacna, Str(pzar, "KONACNA"), v => s.Konacna = v);
            changed |= PopuniAkoPrazno(s.VrstaPrijave, Str(pzar, "VRSTAPRIJ"), v => s.VrstaPrijave = v);
            changed |= PopuniAkoPrazno(s.TipIsplate, Str(pzar, "TIPISP"), v => s.TipIsplate = v);
            changed |= PopuniAkoPrazno(s.VrstaIdIsplatioca, Str(pzar, "VRSTAIDISP"), v => s.VrstaIdIsplatioca = v);

            var datumObav = Dat(pzar, "DATUMOBAV");
            var datumPlac = Dat(pzar, "DATUMPLAC");
            if (!s.DatumObaveze.HasValue && datumObav.HasValue)
            {
                s.DatumObaveze = datumObav;
                s.DatumObavezeC = FormatDatC(datumObav);
                changed = true;
            }

            if (!s.DatumPlacanja.HasValue && datumPlac.HasValue)
            {
                s.DatumPlacanja = datumPlac;
                s.DatumPlacanjaC = FormatDatC(datumPlac);
                changed = true;
            }

            changed |= PopuniAkoPrazno(s.PioR, s.Pio, v => s.PioR = v);
            changed |= PopuniAkoPrazno(s.ZdrR, s.Zdr, v => s.ZdrR = v);
            changed |= PopuniAkoPrazno(s.NezR, s.Nez, v => s.NezR = v);

            changed |= PopuniDecimalAkoNula(s.NBruto, s.Bruto, v => s.NBruto = v);
            changed |= PopuniDecimalAkoNula(s.NOsnovicaP, s.OsnovicaP, v => s.NOsnovicaP = v);
            changed |= PopuniDecimalAkoNula(s.NPorez, s.Porez, v => s.NPorez = v);
            changed |= PopuniDecimalAkoNula(s.NOsnovicaDop, s.OsnovicaDop, v => s.NOsnovicaDop = v);
            changed |= PopuniDecimalAkoNula(s.NPio, s.Pio, v => s.NPio = v);
            changed |= PopuniDecimalAkoNula(s.NZdr, s.Zdr, v => s.NZdr = v);
            changed |= PopuniDecimalAkoNula(s.NNez, s.Nez, v => s.NNez = v);
            changed |= PopuniDecimalAkoNula(s.NPioben, s.PioBen, v => s.NPioben = v);
            changed |= PopuniDecimalAkoNula(s.NKamPor, s.KamPor, v => s.NKamPor = v);
            changed |= PopuniDecimalAkoNula(s.NKamPio, s.KamPio, v => s.NKamPio = v);
            changed |= PopuniDecimalAkoNula(s.NKamZdr, s.KamZdr, v => s.NKamZdr = v);
            changed |= PopuniDecimalAkoNula(s.NKamNez, s.KamNez, v => s.NKamNez = v);
            changed |= PopuniDecimalAkoNula(s.NKamBen, s.KamBen, v => s.NKamBen = v);

            if (changed)
                izmenjeno++;
        }

        if (izmenjeno > 0)
            SacuvajNaDisk(false);

        Poruka = izmenjeno > 0
            ? $"Popunjeni opšti podaci za {izmenjeno} redova."
            : "Nema praznih polja za popunu opštih podataka.";
    }

    [RelayCommand]
    private void PreracunajOdPoreza()
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema redova za preračun.";
            return;
        }

        var redovi = UcitajAktivneStavke();
        var izmenjeno = 0;

        foreach (var s in redovi)
        {
            var porez = ParseDecText(s.Porez);
            if (porez <= 0m)
                continue;

            var stopa = ParseDecText(s.Mfp1);
            if (stopa <= 0m)
                stopa = 10m;
            if (stopa <= 0m)
                continue;

            var novaOsnovica = Math.Round(porez * 100m / stopa, 2, MidpointRounding.AwayFromZero);
            if (novaOsnovica <= 0m)
                continue;

            var changed = false;
            var staraOsnovica = ParseDecText(s.OsnovicaP);
            if (staraOsnovica <= 0m || Math.Abs(staraOsnovica - novaOsnovica) >= 0.01m)
            {
                s.OsnovicaP = FormatDec(novaOsnovica);
                s.NOsnovicaP = novaOsnovica;
                changed = true;
            }

            var stariBruto = ParseDecText(s.Bruto);
            if (stariBruto <= 0m || stariBruto < novaOsnovica)
            {
                s.Bruto = FormatDec(novaOsnovica);
                s.NBruto = novaOsnovica;
                changed = true;
            }

            var staraOsnovicaDop = ParseDecText(s.OsnovicaDop);
            if (staraOsnovicaDop <= 0m)
            {
                s.OsnovicaDop = FormatDec(novaOsnovica);
                s.NOsnovicaDop = novaOsnovica;
                changed = true;
            }

            if (s.NPorez <= 0m && porez > 0m)
            {
                s.NPorez = porez;
                changed = true;
            }

            if (changed)
                izmenjeno++;
        }

        if (izmenjeno > 0)
            SacuvajNaDisk(false);

        Poruka = izmenjeno > 0
            ? $"Preračun od poreza urađen za {izmenjeno} redova."
            : "Nema redova pogodnih za preračun od poreza.";
    }

    [RelayCommand]
    private void NapraviXml()
    {
        var deklaracija = OdrediDeklaraciju();
        if (string.IsNullOrWhiteSpace(deklaracija))
        {
            Poruka = "Nije odredjena deklaracija za XML.";
            return;
        }

        var redovi = Stavke
            .Where(s => string.Equals(Safe(s.Deklarac), Safe(deklaracija), StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (redovi.Count == 0)
        {
            Poruka = $"Nema redova za deklaraciju {deklaracija}.";
            return;
        }

        var pzar = UcitajPrviRed(_xm2PzarPath);
        var targetPath = Path.Combine(_folderPath, "PPPPD.XML");

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement("PoreskaPrijava",
                new XElement("Zaglavlje",
                    new XElement("Deklaracija", deklaracija),
                    new XElement("VrstaPrijave", PrvaNeprazna(Str(pzar, "VRSTAPRIJ"), redovi[0].VrstaPrijave)),
                    new XElement("Godina", PrvaNeprazna(Str(pzar, "GODINA"), redovi[0].Godina)),
                    new XElement("Mesec", PrvaNeprazna(Str(pzar, "ISPZAMES"), redovi[0].IsplataZaMesec)),
                    new XElement("Konacna", PrvaNeprazna(Str(pzar, "KONACNA"), redovi[0].Konacna)),
                    new XElement("DatumObaveze", FormatDatC(Dat(pzar, "DATUMOBAV"))),
                    new XElement("DatumPlacanja", FormatDatC(Dat(pzar, "DATUMPLAC"))),
                    new XElement("TipIsplate", PrvaNeprazna(Str(pzar, "TIPISP"), redovi[0].TipIsplate)),
                    new XElement("VrstaIdIsplatioca", PrvaNeprazna(Str(pzar, "VRSTAIDISP"), redovi[0].VrstaIdIsplatioca))),
                new XElement("Obveznik",
                    new XElement("PIB", PrvaNeprazna(Str(pzar, "PIB"), redovi[0].Pib)),
                    new XElement("JmbgPodnosioca", PrvaNeprazna(Str(pzar, "JMBGPODNOS"), redovi[0].JmbgPodnosioca)),
                    new XElement("Maticni", PrvaNeprazna(Str(pzar, "MATICNI"), redovi[0].Maticni)),
                    new XElement("Naziv", PrvaNeprazna(Str(pzar, "NAZIV"), redovi[0].Naziv)),
                    new XElement("Sediste", PrvaNeprazna(Str(pzar, "SEDISTE"), redovi[0].Sediste)),
                    new XElement("Opstina", PrvaNeprazna(Str(pzar, "OPSTINA"), redovi[0].Opstina))),
                new XElement("Stavke",
                    redovi.Select(r => new XElement("Stavka",
                        new XElement("RedniBroj", Safe(r.RedniBroj)),
                        new XElement("Prezime", Safe(r.Prezime)),
                        new XElement("Ime", Safe(r.Ime)),
                        new XElement("MaticniBr", Safe(r.MaticniBr)),
                        new XElement("SifraPrihoda", Safe(r.SifraPrih)),
                        new XElement("VrstaId", Safe(r.VrstaId)),
                        new XElement("Prebival", Safe(r.Prebival)),
                        new XElement("BrojDana", Safe(r.BrojDana)),
                        new XElement("BrojSati", Safe(r.BrojSati)),
                        new XElement("FondSati", Safe(r.FondSati)),
                        new XElement("Bruto", Safe(r.Bruto)),
                        new XElement("OsnovicaPorez", Safe(r.OsnovicaP)),
                        new XElement("Porez", Safe(r.Porez)),
                        new XElement("OsnovicaDop", Safe(r.OsnovicaDop)),
                        new XElement("Pio", Safe(r.Pio)),
                        new XElement("Zdr", Safe(r.Zdr)),
                        new XElement("Nez", Safe(r.Nez)),
                        new XElement("PioBen", Safe(r.PioBen)),
                        new XElement("Mfp1", Safe(r.Mfp1)),
                        new XElement("Mfp2", Safe(r.Mfp2)),
                        new XElement("Mfp3Proc", Safe(r.Mfp3Proc)),
                        new XElement("Mfp4", Safe(r.Mfp4)),
                        new XElement("Mfp5", Safe(r.Mfp5)),
                        new XElement("Mfp6", Safe(r.Mfp6)),
                        new XElement("Mfp7", Safe(r.Mfp7)),
                        new XElement("Mfp8Nepun", Safe(r.Mfp8Nepun)),
                        new XElement("Mfp9NajOsn", Safe(r.Mfp9NajOsn)),
                        new XElement("Mfp10Dvez", Safe(r.Mfp10Dvez)),
                        new XElement("Mfp11Neop", Safe(r.Mfp11Neop)),
                        new XElement("Mfp12", Safe(r.Mfp12)),
                        new XElement("Radnik", Safe(r.Radnik)),
                        new XElement("Mesto", Safe(r.Mesto)),
                        new XElement("Adresa", Safe(r.Adresa)))))));

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        using var writer = new StreamWriter(targetPath, false, new UTF8Encoding(false));
        doc.Save(writer);

        Poruka = $"Generisan XML: {targetPath}";
    }

    [RelayCommand]
    private void PregledPrijave()
    {
        var xmlPath = Path.Combine(_folderPath, "PPPPD.XML");
        if (!File.Exists(xmlPath))
        {
            Poruka = "PPPPD.XML ne postoji. Prvo kliknite \"3.NAPRAVI XML\".";
            return;
        }

        Process.Start(new ProcessStartInfo(xmlPath) { UseShellExecute = true });
        Poruka = "Otvoren je PPPPD.XML.";
    }

    [RelayCommand]
    private void Uputstvo()
    {
        var view = new Algoritam.WPF.Views.Zarade.PppUputstvoView();
        view.ShowDialog();
    }

    [RelayCommand]
    private void PoreskaUprava()
    {
        var pzar = UcitajPrviRed(_xm2PzarPath);
        var link = Str(pzar, "LINKPUT");
        if (string.IsNullOrWhiteSpace(link))
            link = "http://eporezi.poreskauprava.gov.rs";

        var match = Regex.Match(link, @"https?://\S+", RegexOptions.IgnoreCase);
        var url = match.Success ? match.Value : link;

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            Poruka = "Otvorena je stranica Poreske uprave.";
        }
        catch (Exception ex)
        {
            Poruka = $"Ne mogu da otvorim link: {ex.Message}";
        }
    }

    [RelayCommand]
    private void PrenosUArhivu()
    {
        if (string.IsNullOrWhiteSpace(_xm2LdSvePath))
        {
            Poruka = "Nije pronađen xm2ldsve.dbf.";
            return;
        }

        var deklaracija = OdrediDeklaraciju();
        if (string.IsNullOrWhiteSpace(deklaracija))
        {
            Poruka = "Nema deklaracije za prenos u arhivu.";
            return;
        }

        var sourceRows = Stavke
            .Where(s => string.Equals(Safe(s.Deklarac), Safe(deklaracija), StringComparison.OrdinalIgnoreCase))
            .Select(s => s.ToRowDictionary())
            .ToList();

        if (sourceRows.Count == 0)
        {
            Poruka = $"Nema redova za deklaraciju {deklaracija}.";
            return;
        }

        var arhivaSchema = DbfTableWriter.LoadSchema(_xm2LdSvePath);
        var arhivaRows = DbfReader.CitajSveZapise(_xm2LdSvePath);

        var keys = new HashSet<string>(
            arhivaRows.Select(KljucZaArhivu),
            StringComparer.OrdinalIgnoreCase);

        var appended = 0;
        foreach (var row in sourceRows)
        {
            var key = KljucZaArhivu(row);
            if (keys.Contains(key))
                continue;

            row["PRENETO"] = "D";
            arhivaRows.Add(row);
            keys.Add(key);
            appended++;
        }

        DbfTableWriter.WriteTable(
            _xm2LdSvePath,
            arhivaSchema,
            arhivaRows,
            static (r, fieldName) => r.TryGetValue(fieldName, out var value) ? value : null);

        foreach (var stavka in Stavke.Where(s => string.Equals(Safe(s.Deklarac), Safe(deklaracija), StringComparison.OrdinalIgnoreCase)))
            stavka.Preneto = "D";

        SacuvajNaDisk(false);
        Poruka = $"Preneto u arhivu: {appended} redova (deklaracija {deklaracija}).";
    }

    [RelayCommand]
    private void KorekcijaDoprinosa()
    {
        if (SelektovanaStavka is null)
        {
            Poruka = "Izaberite red za korekciju doprinosa.";
            return;
        }

        var inicijalno = $"{SelektovanaStavka.Pio};{SelektovanaStavka.Zdr};{SelektovanaStavka.Nez};{SelektovanaStavka.PioBen}";
        var unos = PromptText(
            "Korekcija doprinosa",
            "Unesite nove vrednosti u formatu: PIO;ZDR;NEZ;PIOBEN",
            inicijalno);

        if (string.IsNullOrWhiteSpace(unos))
            return;

        var delovi = unos.Split(';', StringSplitOptions.TrimEntries);
        if (delovi.Length < 4)
        {
            Poruka = "Neispravan format unosa.";
            return;
        }

        SelektovanaStavka.Pio = delovi[0];
        SelektovanaStavka.Zdr = delovi[1];
        SelektovanaStavka.Nez = delovi[2];
        SelektovanaStavka.PioBen = delovi[3];

        Poruka = "Korekcija doprinosa je upisana u izabrani red.";
    }

    [RelayCommand]
    private void JedanRadnikSkraceni()
    {
        var redovi = UcitajAktivneStavke();
        if (redovi.Count == 0)
        {
            Poruka = "Nema podataka za izveštaj.";
            return;
        }

        var predlog = !string.IsNullOrWhiteSpace(SelektovanaStavka?.MaticniBr)
            ? SelektovanaStavka!.MaticniBr
            : Safe(SelektovanaStavka?.Prezime);

        var unos = PromptText(
            "Jedan radnik - skraceni",
            "Unesite matični broj ili deo prezimena:",
            predlog);

        if (string.IsNullOrWhiteSpace(unos))
            return;

        var kriterijum = Safe(unos);
        var filtrirani = redovi
            .Where(s =>
                Safe(s.MaticniBr).Contains(kriterijum, StringComparison.OrdinalIgnoreCase) ||
                Safe(s.Prezime).Contains(kriterijum, StringComparison.OrdinalIgnoreCase) ||
                Safe($"{s.Prezime} {s.Ime}").Contains(kriterijum, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filtrirani.Count == 0)
        {
            Poruka = $"Nema podataka za kriterijum: {kriterijum}";
            return;
        }

        var lines = new List<string>
        {
            "MATICNIBR;PREZIME;IME;SIFRAPRIH;BRUTO;POREZ;PIO;ZDR;NEZ;UKUPNO_DOPR;NETO"
        };

        foreach (var g in filtrirani
            .GroupBy(s => new { Maticni = Safe(s.MaticniBr), Prezime = Safe(s.Prezime), Ime = Safe(s.Ime), Sifra = Safe(s.SifraPrih) })
            .OrderBy(g => g.Key.Prezime)
            .ThenBy(g => g.Key.Ime)
            .ThenBy(g => g.Key.Sifra))
        {
            var bruto = g.Sum(IznosBruto);
            var porez = g.Sum(IznosPorez);
            var pio = g.Sum(IznosPio);
            var zdr = g.Sum(IznosZdr);
            var nez = g.Sum(IznosNez);
            var dop = pio + zdr + nez;
            var neto = bruto - porez - dop;

            lines.Add(string.Join(";",
                Csv(g.Key.Maticni),
                Csv(g.Key.Prezime),
                Csv(g.Key.Ime),
                Csv(g.Key.Sifra),
                Csv(FormatDec(bruto)),
                Csv(FormatDec(porez)),
                Csv(FormatDec(pio)),
                Csv(FormatDec(zdr)),
                Csv(FormatDec(nez)),
                Csv(FormatDec(dop)),
                Csv(FormatDec(neto))));
        }

        var putanja = IzaberiTxtPutanju("PPP_JEDAN_RADNIK_SKRACENI.txt", "Jedan radnik - skraceni");
        if (string.IsNullOrWhiteSpace(putanja))
            return;

        SacuvajTxtIzvestaj(putanja, lines, "Generisan izveštaj 'Jedan radnik - skraceni'");
    }

    [RelayCommand]
    private void SaldoSkraceni()
    {
        var redovi = UcitajAktivneStavke();
        if (redovi.Count == 0)
        {
            Poruka = "Nema podataka za saldo skraceni.";
            return;
        }

        var lines = new List<string>
        {
            "MATICNIBR;PREZIME;IME;BRUTO;POREZ;PIO;ZDR;NEZ;UKUPNO_DOPR;NETO"
        };

        foreach (var g in redovi
            .GroupBy(s => new { Maticni = Safe(s.MaticniBr), Prezime = Safe(s.Prezime), Ime = Safe(s.Ime) })
            .OrderBy(g => g.Key.Prezime)
            .ThenBy(g => g.Key.Ime))
        {
            var bruto = g.Sum(IznosBruto);
            var porez = g.Sum(IznosPorez);
            var pio = g.Sum(IznosPio);
            var zdr = g.Sum(IznosZdr);
            var nez = g.Sum(IznosNez);
            var dop = pio + zdr + nez;
            var neto = bruto - porez - dop;

            lines.Add(string.Join(";",
                Csv(g.Key.Maticni),
                Csv(g.Key.Prezime),
                Csv(g.Key.Ime),
                Csv(FormatDec(bruto)),
                Csv(FormatDec(porez)),
                Csv(FormatDec(pio)),
                Csv(FormatDec(zdr)),
                Csv(FormatDec(nez)),
                Csv(FormatDec(dop)),
                Csv(FormatDec(neto))));
        }

        var putanja = IzaberiTxtPutanju("PPP_SALDO_SKRACENI.txt", "Saldo skraceni");
        if (string.IsNullOrWhiteSpace(putanja))
            return;

        SacuvajTxtIzvestaj(putanja, lines, "Generisan izveštaj 'Saldo skraceni'");
    }

    [RelayCommand]
    private void SaldoObrasci()
    {
        var redovi = UcitajAktivneStavke();
        if (redovi.Count == 0)
        {
            Poruka = "Nema podataka za saldo obrasci.";
            return;
        }

        var lines = new List<string>
        {
            "SIFRAPRIH;RADNIK;BROJ_REDOVA;BRUTO;OSNOVICA_P;POREZ;PIO;ZDR;NEZ;UKUPNO_DOPR"
        };

        foreach (var g in redovi
            .GroupBy(s => new { Sifra = Safe(s.SifraPrih), Radnik = Safe(s.Radnik) })
            .OrderBy(g => g.Key.Sifra)
            .ThenBy(g => g.Key.Radnik))
        {
            var bruto = g.Sum(IznosBruto);
            var osnovica = g.Sum(IznosOsnovicaP);
            var porez = g.Sum(IznosPorez);
            var pio = g.Sum(IznosPio);
            var zdr = g.Sum(IznosZdr);
            var nez = g.Sum(IznosNez);
            var dop = pio + zdr + nez;

            lines.Add(string.Join(";",
                Csv(g.Key.Sifra),
                Csv(g.Key.Radnik),
                Csv(g.Count().ToString(CultureInfo.InvariantCulture)),
                Csv(FormatDec(bruto)),
                Csv(FormatDec(osnovica)),
                Csv(FormatDec(porez)),
                Csv(FormatDec(pio)),
                Csv(FormatDec(zdr)),
                Csv(FormatDec(nez)),
                Csv(FormatDec(dop))));
        }

        var putanja = IzaberiTxtPutanju("PPP_SALDO_OBRASCI.txt", "Saldo obrasci");
        if (string.IsNullOrWhiteSpace(putanja))
            return;

        SacuvajTxtIzvestaj(putanja, lines, "Generisan izveštaj 'Saldo obrasci'");
    }

    [RelayCommand]
    private void ObrazacPoMesecima()
    {
        var redovi = UcitajAktivneStavke();
        if (redovi.Count == 0)
        {
            Poruka = "Nema podataka za obrazac po mesecima.";
            return;
        }

        var lines = new List<string>
        {
            "GODINA;MESEC;BROJ_REDOVA;BRUTO;POREZ;PIO;ZDR;NEZ;UKUPNO_DOPR"
        };

        foreach (var g in redovi
            .GroupBy(s => new { Godina = Safe(s.Godina), Mesec = Safe(s.IsplataZaMesec) })
            .OrderBy(g => g.Key.Godina)
            .ThenBy(g => g.Key.Mesec))
        {
            var bruto = g.Sum(IznosBruto);
            var porez = g.Sum(IznosPorez);
            var pio = g.Sum(IznosPio);
            var zdr = g.Sum(IznosZdr);
            var nez = g.Sum(IznosNez);
            var dop = pio + zdr + nez;

            lines.Add(string.Join(";",
                Csv(g.Key.Godina),
                Csv(g.Key.Mesec),
                Csv(g.Count().ToString(CultureInfo.InvariantCulture)),
                Csv(FormatDec(bruto)),
                Csv(FormatDec(porez)),
                Csv(FormatDec(pio)),
                Csv(FormatDec(zdr)),
                Csv(FormatDec(nez)),
                Csv(FormatDec(dop))));
        }

        var putanja = IzaberiTxtPutanju("PPP_OBRAZAC_PO_MESECIMA.txt", "Obrazac po mesecima");
        if (string.IsNullOrWhiteSpace(putanja))
            return;

        SacuvajTxtIzvestaj(putanja, lines, "Generisan izveštaj 'Obrazac po mesecima'");
    }

    [RelayCommand]
    private void IzvozPpp2()
    {
        var redovi = UcitajAktivneStavke();
        if (redovi.Count == 0)
        {
            Poruka = "Nema podataka za izvoz PPP2.";
            return;
        }

        var lines = new List<string>
        {
            "DEKLARAC;REDNIBROJ;MATICNIBR;PREZIME;IME;SIFRAPRIH;BRUTO;OSNOVICAP;POREZ;OSNOVIDOP;PIO;ZDR;NEZ;PIOBEN"
        };

        foreach (var s in redovi.OrderBy(r => r.Deklarac).ThenBy(r => ParseDecText(r.RedniBroj)))
        {
            lines.Add(string.Join(";",
                Csv(Safe(s.Deklarac)),
                Csv(Safe(s.RedniBroj)),
                Csv(Safe(s.MaticniBr)),
                Csv(Safe(s.Prezime)),
                Csv(Safe(s.Ime)),
                Csv(Safe(s.SifraPrih)),
                Csv(FormatDec(IznosBruto(s))),
                Csv(FormatDec(IznosOsnovicaP(s))),
                Csv(FormatDec(IznosPorez(s))),
                Csv(FormatDec(IznosOsnovicaDop(s))),
                Csv(FormatDec(IznosPio(s))),
                Csv(FormatDec(IznosZdr(s))),
                Csv(FormatDec(IznosNez(s))),
                Csv(FormatDec(IznosPioben(s)))));
        }

        var putanja = IzaberiTxtPutanju("PPP2.txt", "Izvoz deo 2 (PPP2)");
        if (string.IsNullOrWhiteSpace(putanja))
            return;

        SacuvajTxtIzvestaj(putanja, lines, "Generisan izvoz PPP2");
    }

    [RelayCommand]
    private void IzvozPpp3()
    {
        var redovi = UcitajAktivneStavke();
        if (redovi.Count == 0)
        {
            Poruka = "Nema podataka za izvoz PPP3.";
            return;
        }

        var lines = new List<string>
        {
            "MATICNIBR;PREZIME;IME;BROJ_REDOVA;BRUTO;POREZ;PIO;ZDR;NEZ;PIOBEN;NETO"
        };

        foreach (var g in redovi
            .GroupBy(s => new { Maticni = Safe(s.MaticniBr), Prezime = Safe(s.Prezime), Ime = Safe(s.Ime) })
            .OrderBy(g => g.Key.Prezime)
            .ThenBy(g => g.Key.Ime))
        {
            var bruto = g.Sum(IznosBruto);
            var porez = g.Sum(IznosPorez);
            var pio = g.Sum(IznosPio);
            var zdr = g.Sum(IznosZdr);
            var nez = g.Sum(IznosNez);
            var pioben = g.Sum(IznosPioben);
            var neto = bruto - porez - pio - zdr - nez - pioben;

            lines.Add(string.Join(";",
                Csv(g.Key.Maticni),
                Csv(g.Key.Prezime),
                Csv(g.Key.Ime),
                Csv(g.Count().ToString(CultureInfo.InvariantCulture)),
                Csv(FormatDec(bruto)),
                Csv(FormatDec(porez)),
                Csv(FormatDec(pio)),
                Csv(FormatDec(zdr)),
                Csv(FormatDec(nez)),
                Csv(FormatDec(pioben)),
                Csv(FormatDec(neto))));
        }

        var putanja = IzaberiTxtPutanju("PPP3.txt", "Izvoz deo 3 (PPP3)");
        if (string.IsNullOrWhiteSpace(putanja))
            return;

        SacuvajTxtIzvestaj(putanja, lines, "Generisan izvoz PPP3");
    }

    [RelayCommand]
    private void IzvozPpp4()
    {
        var redovi = UcitajAktivneStavke();
        if (redovi.Count == 0)
        {
            Poruka = "Nema podataka za izvoz PPP4.";
            return;
        }

        var lines = new List<string>
        {
            "GODINA;MESEC;BROJ_REDOVA;BRUTO;POREZ;PIO;ZDR;NEZ;PIOBEN;NETO"
        };

        foreach (var g in redovi
            .GroupBy(s => new { Godina = Safe(s.Godina), Mesec = Safe(s.IsplataZaMesec) })
            .OrderBy(g => g.Key.Godina)
            .ThenBy(g => g.Key.Mesec))
        {
            var bruto = g.Sum(IznosBruto);
            var porez = g.Sum(IznosPorez);
            var pio = g.Sum(IznosPio);
            var zdr = g.Sum(IznosZdr);
            var nez = g.Sum(IznosNez);
            var pioben = g.Sum(IznosPioben);
            var neto = bruto - porez - pio - zdr - nez - pioben;

            lines.Add(string.Join(";",
                Csv(g.Key.Godina),
                Csv(g.Key.Mesec),
                Csv(g.Count().ToString(CultureInfo.InvariantCulture)),
                Csv(FormatDec(bruto)),
                Csv(FormatDec(porez)),
                Csv(FormatDec(pio)),
                Csv(FormatDec(zdr)),
                Csv(FormatDec(nez)),
                Csv(FormatDec(pioben)),
                Csv(FormatDec(neto))));
        }

        var putanja = IzaberiTxtPutanju("PPP4.txt", "Izvoz deo 4 (PPP4)");
        if (string.IsNullOrWhiteSpace(putanja))
            return;

        SacuvajTxtIzvestaj(putanja, lines, "Generisan izvoz PPP4");
    }

    [RelayCommand]
    private void TraziSifruPrihoda() => TraziPoPolju("Sifra prihoda", s => s.SifraPrih);

    [RelayCommand]
    private void TraziMaticniBroj() => TraziPoPolju("Maticni broj", s => s.MaticniBr);

    [RelayCommand]
    private void TraziPrezime() => TraziPoPolju("Prezime", s => s.Prezime);

    [RelayCommand]
    private void EksportXls()
    {
        var deklaracija = OdrediDeklaraciju();
        var redovi = string.IsNullOrWhiteSpace(deklaracija)
            ? Stavke.ToList()
            : Stavke.Where(s => string.Equals(Safe(s.Deklarac), Safe(deklaracija), StringComparison.OrdinalIgnoreCase)).ToList();

        if (redovi.Count == 0)
        {
            Poruka = "Nema redova za eksport.";
            return;
        }

        var suggested = string.IsNullOrWhiteSpace(deklaracija) ? "PPPPD_EXPORT.csv" : $"PPPPD_{deklaracija}.csv";
        var dialog = new SaveFileDialog
        {
            Title = "Eksport PPP-PD u Excel (CSV)",
            Filter = "CSV fajl (*.csv)|*.csv|Excel fajl (*.xls)|*.xls|Svi fajlovi (*.*)|*.*",
            FileName = suggested,
            InitialDirectory = _folderPath
        };

        if (dialog.ShowDialog() != true)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("Deklarac;Rednibroj;Prezime;Ime;Vrstaid;Prebival;Sifraprih;Maticnibr;Brojdana;Brojsati;Fondsati;Bruto;Osnovicap;Porez;Osnovidop;Pio;Zdr;Nez;Pioben;Mfp1");
        foreach (var r in redovi)
        {
            sb.AppendLine(string.Join(";",
                Csv(r.Deklarac), Csv(r.RedniBroj), Csv(r.Prezime), Csv(r.Ime), Csv(r.VrstaId), Csv(r.Prebival),
                Csv(r.SifraPrih), Csv(r.MaticniBr), Csv(r.BrojDana), Csv(r.BrojSati), Csv(r.FondSati),
                Csv(r.Bruto), Csv(r.OsnovicaP), Csv(r.Porez), Csv(r.OsnovicaDop), Csv(r.Pio), Csv(r.Zdr), Csv(r.Nez), Csv(r.PioBen), Csv(r.Mfp1)));
        }

        File.WriteAllText(dialog.FileName, sb.ToString(), new UTF8Encoding(false));
        Poruka = $"Eksport zavrsen: {dialog.FileName}";
    }

    [RelayCommand]
    private void Izlaz()
    {
        SacuvajNaDisk(false);
        ZatvoriAction?.Invoke();
    }

    partial void OnSelektovanaStavkaChanged(PppLdStavka? value)
    {
        if (value is null)
        {
            StranaTekst = "- / -";
            return;
        }

        var rbInt = int.TryParse(value.RedniBroj.Trim(), out var rb) ? rb : 0;
        var strana = rbInt > 0 ? (int)Math.Ceiling(rbInt / 8.0) : 1;

        var deklaracija = Safe(value.Deklarac);
        var maxRb = Stavke
            .Where(s => string.Equals(Safe(s.Deklarac), deklaracija, StringComparison.OrdinalIgnoreCase))
            .Select(s => int.TryParse(s.RedniBroj.Trim(), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        var totalStrane = maxRb > 0 ? (int)Math.Ceiling(maxRb / 8.0) : 1;
        StranaTekst = $"{strana} / {totalStrane}";
    }

    private void Ucitaj()
    {
        Stavke.Clear();

        var firma = UcitajFirmuInfo();
        FirmaNaziv = string.IsNullOrWhiteSpace(firma.Naziv) ? "FIRMA" : firma.Naziv.Trim();
        FirmaMesto = firma.Mesto.Trim();

        _xm2LdPath = PronadjiIliKreirajDbf("xm2ld.dbf");
        _xm2LdSvePath = PronadjiIliKreirajDbf("xm2ldsve.dbf");
        _xm2PzarPath = PronadjiIliKreirajDbf("xm2pzar.dbf");

        if (string.IsNullOrWhiteSpace(_xm2LdPath) || !File.Exists(_xm2LdPath))
        {
            Poruka = "Nije pronađen xm2ld.dbf.";
            return;
        }

        _xm2LdSchema = DbfTableWriter.LoadSchema(_xm2LdPath);
        var rows = DbfReader.CitajSveZapise(_xm2LdPath);
        foreach (var row in rows)
            Stavke.Add(PppLdStavka.FromRow(row));

        SelektovanaStavka = Stavke.Count > 0 ? Stavke[0] : null;
        Poruka = $"Ucitano {Stavke.Count} redova iz xm2ld.dbf.";
    }

    private List<PppLdStavka> UcitajAktivneStavke()
    {
        var deklaracija = OdrediDeklaraciju();
        if (!string.IsNullOrWhiteSpace(deklaracija))
        {
            var filtrirani = Stavke
                .Where(s => string.Equals(Safe(s.Deklarac), Safe(deklaracija), StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filtrirani.Count > 0)
                return filtrirani;
        }

        return Stavke.ToList();
    }

    private static bool PopuniAkoPrazno(string trenutno, string novo, Action<string> setter)
    {
        var novaVrednost = Safe(novo);
        if (!string.IsNullOrWhiteSpace(trenutno) || string.IsNullOrWhiteSpace(novaVrednost))
            return false;

        setter(novaVrednost);
        return true;
    }

    private static bool PopuniDecimalAkoNula(decimal trenutno, string tekst, Action<decimal> setter)
    {
        if (trenutno != 0m)
            return false;

        var parsed = ParseDecText(tekst);
        if (parsed == 0m)
            return false;

        setter(parsed);
        return true;
    }

    private static Dictionary<string, object?> NadjiLdradZaStavku(
        PppLdStavka stavka,
        Dictionary<string, Dictionary<string, object?>> ldradByBroj,
        Dictionary<string, Dictionary<string, object?>> ldradByMaticni)
    {
        var broj = Safe(stavka.RedniBroj);
        if (!string.IsNullOrWhiteSpace(broj) && ldradByBroj.TryGetValue(broj, out var poBroju))
            return poBroju;

        var maticni = Safe(stavka.MaticniBr);
        if (!string.IsNullOrWhiteSpace(maticni) && ldradByMaticni.TryGetValue(maticni, out var poMaticnom))
            return poMaticnom;

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    private static string KljucImporta(PppLdStavka s)
        => string.Join("|",
            Safe(s.Deklarac),
            Safe(s.MaticniBr),
            Safe(s.SifraPrih),
            IznosBruto(s).ToString("0.00", CultureInfo.InvariantCulture),
            IznosPorez(s).ToString("0.00", CultureInfo.InvariantCulture),
            Safe(s.Radnik));

    private PppLdStavka? MapirajOpjOdpUSifru(
        Dictionary<string, object?> row,
        string deklaracija,
        Dictionary<string, object?> pzar,
        Dictionary<string, Dictionary<string, object?>> ldradByBroj,
        Dictionary<string, Dictionary<string, object?>> ldradByMaticni,
        int redniBroj)
    {
        var broj = NumAsText(row, "BROJ");
        var maticni = PrvaNeprazna(StrPrvi(row, "MATICNIBR", "MATICNIB"), string.Empty);

        Dictionary<string, object?> ldrad = new(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(broj) && ldradByBroj.TryGetValue(broj, out var byBroj))
            ldrad = byBroj;
        else if (!string.IsNullOrWhiteSpace(maticni) && ldradByMaticni.TryGetValue(maticni, out var byMaticni))
            ldrad = byMaticni;

        maticni = PrvaNeprazna(maticni, Str(ldrad, "MATICNIBR"));
        var sifraPrih = PrvaNeprazna(Str(row, "SIFRAPRIH"), Str(ldrad, "SIFRAPRIH"));

        var prezime = PrvaNeprazna(StrPrvi(row, "PREZIME"), Str(ldrad, "PREZIME"));
        var ime = PrvaNeprazna(StrPrvi(row, "IME"), Str(ldrad, "IME"));
        if (string.IsNullOrWhiteSpace(prezime) && string.IsNullOrWhiteSpace(ime))
            SplitImePrezime(PrvaNeprazna(Str(row, "IME_PREZ"), Str(ldrad, "IME_PREZ")), out prezime, out ime);

        var bruto = DecPrviNenulti(row, "BRUTOLD", "BRUTO", "UKBRUTOLD", "ZARADA", "OSNOVICA", "UKUPNO", "BRUTOOPOR", "ABRUTO", "BRUTO2");
        if (bruto == 0m)
            bruto = DecPrviNenulti(row, "NETO", "NETOLD", "ISPLACENO", "ZAISPLATU", "CNETO");

        var osnovicaP = DecPrviNenulti(row, "OPOREZIVO", "OSNOVICA", "BRUTOOPOR", "NETOOPOR", "NOPOREZ", "N2OPOREZ", "N3OPOREZ", "POSNOVICA", "IOSNOVICA");
        if (osnovicaP == 0m)
            osnovicaP = bruto;

        var porez = DecPrviNenulti(row, "POREZ", "PORDOH", "PPORDOH", "IPORDOH", "KAMPOREZ", "DIVPOREZ", "INVPOREZ", "DOBPOREZ", "VLAPOREZ");
        var pio = DecPrviNenulti(row, "PIO", "PENSOC", "DOPPIO", "PDOPPIO", "IDOPPIO");
        var zdr = DecPrviNenulti(row, "ZDR", "ZDRSOC", "DOPZDR", "PDOPZDR", "IDOPZDR");
        var nez = DecPrviNenulti(row, "NEZSOC", "DOPNEZ", "PDOPNEZ", "IDOPNEZ");
        var pioben = DecPrviNenulti(row, "PIOBEN", "DOPBEN", "PDOPBEN", "IDOPBEN");

        if (string.IsNullOrWhiteSpace(maticni) &&
            string.IsNullOrWhiteSpace(sifraPrih) &&
            bruto == 0m &&
            porez == 0m &&
            pio == 0m &&
            zdr == 0m &&
            nez == 0m)
        {
            return null;
        }

        var tipRadnika = PrvaNeprazna(Str(row, "RADNIK"), "R");
        if (!string.Equals(tipRadnika, "R", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(tipRadnika, "F", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(tipRadnika, "V", StringComparison.OrdinalIgnoreCase))
        {
            tipRadnika = "R";
        }

        var vrstaId = PrvaNeprazna(Str(row, "VRSTAID"), Str(ldrad, "VRSTAID"));
        if (string.IsNullOrWhiteSpace(vrstaId))
            vrstaId = string.Equals(tipRadnika, "R", StringComparison.OrdinalIgnoreCase) ? "1" : "0";

        return new PppLdStavka
        {
            Deklarac = deklaracija,
            RedniBroj = redniBroj.ToString(CultureInfo.InvariantCulture),
            Prezime = prezime,
            Ime = ime,
            VrstaId = vrstaId,
            Prebival = PrvaNeprazna(Str(row, "PREBIVAL"), Str(ldrad, "PREBIVAL")),
            SifraPrih = sifraPrih,
            MaticniBr = maticni,
            BrojDana = PrvaNeprazna(NumAsText(row, "BROJDANA"), Str(pzar, "DANA")),
            BrojSati = Str(pzar, "FONDSATI"),
            FondSati = Str(pzar, "FONDSATI"),
            Bruto = FormatDec(bruto),
            OsnovicaP = FormatDec(osnovicaP),
            Porez = FormatDec(porez),
            OsnovicaDop = FormatDec(DecPrviNenulti(row, "OSNOVIDOP", "OSNOVICA", "POSNOVICA", "IOSNOVICA")),
            Pio = FormatDec(pio),
            Zdr = FormatDec(zdr),
            Nez = FormatDec(nez),
            PioBen = FormatDec(pioben),
            Mfp1 = FormatDec(DecPrviNenulti(row, "MFP1", "PROCPOR", "PPROCPOR", "IPROCPOR")),
            Mfp10Dvez = Str(ldrad, "MFP10DVEZ"),
            VrstaPrijave = Str(pzar, "VRSTAPRIJ"),
            Godina = PrvaNeprazna(Str(pzar, "GODINA"), DateTime.Today.Year.ToString(CultureInfo.InvariantCulture)),
            IsplataZaMesec = PrvaNeprazna(NumAsText(row, "MESEC"), Str(pzar, "ISPZAMES")),
            Konacna = Str(pzar, "KONACNA"),
            DatumObaveze = Dat(pzar, "DATUMOBAV"),
            DatumPlacanja = Dat(pzar, "DATUMPLAC"),
            DatumObavezeC = FormatDatC(Dat(pzar, "DATUMOBAV")),
            DatumPlacanjaC = FormatDatC(Dat(pzar, "DATUMPLAC")),
            VrstaIzmene = Str(pzar, "VRSTAIZM"),
            Identifikacija = Str(pzar, "IDENTIFIK"),
            BrojResenja = Str(pzar, "BROJRES"),
            Osnov = Str(pzar, "OSNOV"),
            TipIsplate = Str(pzar, "TIPISP"),
            VrstaIdIsplatioca = Str(pzar, "VRSTAIDISP"),
            Najniza = Str(pzar, "PROPISANAO"),
            Pib = Str(pzar, "PIB"),
            Dana = Str(pzar, "DANA"),
            BrojZapos = Str(pzar, "BROJZAPOS"),
            JmbgPodnosioca = Str(pzar, "JMBGPODNOS"),
            Maticni = Str(pzar, "MATICNI"),
            Naziv = PrvaNeprazna(Str(row, "NAZIV"), Str(pzar, "NAZIV")),
            Sediste = Str(pzar, "SEDISTE"),
            Telefon = Str(pzar, "TELEFON"),
            UlicaIBr = PrvaNeprazna(Str(row, "ADRESA"), Str(pzar, "ULICAIBR")),
            Email = Str(pzar, "EMAIL"),
            Period = Str(pzar, "PERIOD"),
            RedniIsplate = PrvaNeprazna(NumAsText(row, "REDISPL"), NumAsText(pzar, "REDISPL")),
            Opstina = Str(pzar, "OPSTINA"),
            Radnik = tipRadnika,
            Mesto = PrvaNeprazna(Str(row, "NAZIV"), Str(ldrad, "MESTO")),
            Adresa = PrvaNeprazna(Str(row, "ADRESA"), Str(ldrad, "ADRESA")),
            Preneto = "O",
            PioR = FormatDec(pio),
            ZdrR = FormatDec(zdr),
            NezR = FormatDec(nez),
            NBruto = bruto,
            NOsnovicaP = osnovicaP,
            NPorez = porez,
            NOsnovicaDop = DecPrviNenulti(row, "OSNOVIDOP", "OSNOVICA", "POSNOVICA", "IOSNOVICA"),
            NPio = pio,
            NZdr = zdr,
            NNez = nez,
            NPioben = pioben
        };
    }

    private string? IzaberiTxtPutanju(string predlog, string title)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = "TXT fajl (*.txt)|*.txt|Svi fajlovi (*.*)|*.*",
            FileName = predlog,
            InitialDirectory = _folderPath
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private void SacuvajTxtIzvestaj(string path, IReadOnlyCollection<string> lines, string poruka)
    {
        File.WriteAllLines(path, lines, new UTF8Encoding(false));

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch
        {
            // Otvaranje fajla je best-effort.
        }

        Poruka = $"{poruka}: {path}";
    }

    private static decimal IznosBruto(PppLdStavka s) => s.NBruto != 0m ? s.NBruto : ParseDecText(s.Bruto);
    private static decimal IznosOsnovicaP(PppLdStavka s) => s.NOsnovicaP != 0m ? s.NOsnovicaP : ParseDecText(s.OsnovicaP);
    private static decimal IznosPorez(PppLdStavka s) => s.NPorez != 0m ? s.NPorez : ParseDecText(s.Porez);
    private static decimal IznosOsnovicaDop(PppLdStavka s) => s.NOsnovicaDop != 0m ? s.NOsnovicaDop : ParseDecText(s.OsnovicaDop);
    private static decimal IznosPio(PppLdStavka s) => s.NPio != 0m ? s.NPio : ParseDecText(s.Pio);
    private static decimal IznosZdr(PppLdStavka s) => s.NZdr != 0m ? s.NZdr : ParseDecText(s.Zdr);
    private static decimal IznosNez(PppLdStavka s) => s.NNez != 0m ? s.NNez : ParseDecText(s.Nez);
    private static decimal IznosPioben(PppLdStavka s) => s.NPioben != 0m ? s.NPioben : ParseDecText(s.PioBen);

    private static string StrPrvi(Dictionary<string, object?> row, params string[] fieldNames)
    {
        foreach (var field in fieldNames)
        {
            if (!row.ContainsKey(field))
                continue;

            var value = Str(row, field);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return string.Empty;
    }

    private static decimal DecPrviNenulti(Dictionary<string, object?> row, params string[] fieldNames)
    {
        foreach (var field in fieldNames)
        {
            if (!row.ContainsKey(field))
                continue;

            var value = Dec(row, field);
            if (value != 0m)
                return value;
        }

        return 0m;
    }

    private static decimal ParseDecText(string? text)
    {
        var value = (text ?? string.Empty).Trim();
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : (decimal.TryParse(value, out parsed) ? parsed : 0m);
    }

    private void TraziPoPolju(string nazivPolja, Func<PppLdStavka, string> selektor)
    {
        if (Stavke.Count == 0)
        {
            Poruka = "Nema podataka za pretragu.";
            return;
        }

        var unos = PromptText($"Trazi {nazivPolja}", $"Unesite vrednost za {nazivPolja.ToLowerInvariant()}:", string.Empty);
        if (string.IsNullOrWhiteSpace(unos))
            return;

        var value = unos.Trim();
        var found = Stavke.FirstOrDefault(s =>
            selektor(s).Contains(value, StringComparison.OrdinalIgnoreCase));

        if (found is null)
        {
            Poruka = $"Nije pronađen zapis za: {value}";
            return;
        }

        SelektovanaStavka = found;
        Poruka = $"Pronađen zapis za: {value}";
    }

    private string OdrediDeklaraciju()
    {
        if (SelektovanaStavka is not null && !string.IsNullOrWhiteSpace(SelektovanaStavka.Deklarac))
            return SelektovanaStavka.Deklarac.Trim();

        var fromRows = Stavke.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.Deklarac))?.Deklarac ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(fromRows))
            return fromRows.Trim();

        return UcitajDeklaracijuIzPzar();
    }

    private Dictionary<string, Dictionary<string, object?>> UcitajLdradByBroj()
    {
        var result = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        var ldradPath = PronadjiDbf(_folderPath, "ldrad.dbf");
        if (string.IsNullOrWhiteSpace(ldradPath) || !File.Exists(ldradPath))
            return result;

        foreach (var row in DbfReader.CitajSveZapise(ldradPath))
        {
            var broj = NumAsText(row, "BROJ");
            if (string.IsNullOrWhiteSpace(broj))
                continue;

            if (!result.ContainsKey(broj))
                result[broj] = row;
        }

        return result;
    }

    private Dictionary<string, Dictionary<string, object?>> UcitajLdradByMaticni()
    {
        var result = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        var ldradPath = PronadjiDbf(_folderPath, "ldrad.dbf");
        if (string.IsNullOrWhiteSpace(ldradPath) || !File.Exists(ldradPath))
            return result;

        foreach (var row in DbfReader.CitajSveZapise(ldradPath))
        {
            var maticni = Str(row, "MATICNIBR");
            if (string.IsNullOrWhiteSpace(maticni))
                continue;

            if (!result.ContainsKey(maticni))
                result[maticni] = row;
        }

        return result;
    }

    private Dictionary<string, object?> UcitajPrviRed(string? dbfPath)
    {
        if (string.IsNullOrWhiteSpace(dbfPath) || !File.Exists(dbfPath))
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        return DbfReader.CitajSveZapise(dbfPath).FirstOrDefault()
               ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    private string UcitajDeklaracijuIzPzar()
    {
        var row = UcitajPrviRed(_xm2PzarPath);
        return Str(row, "DEKLARAC");
    }

    private string UcitajFondSatiIzPzar()
    {
        var row = UcitajPrviRed(_xm2PzarPath);
        return Str(row, "FONDSATI");
    }

    private string UcitajBrojDanaIzPzar()
    {
        var row = UcitajPrviRed(_xm2PzarPath);
        return Str(row, "DANA");
    }

    private string UcitajBrojZaposIzPzar()
    {
        var row = UcitajPrviRed(_xm2PzarPath);
        return Str(row, "BROJZAPOS");
    }

    private string UcitajKonacnaIzPzar()
    {
        var row = UcitajPrviRed(_xm2PzarPath);
        return Str(row, "KONACNA");
    }

    private string UcitajVrstuPrijaveIzPzar()
    {
        var row = UcitajPrviRed(_xm2PzarPath);
        return Str(row, "VRSTAPRIJ");
    }

    private string UcitajGodinuIzPzar()
    {
        var row = UcitajPrviRed(_xm2PzarPath);
        return Str(row, "GODINA");
    }

    private string UcitajMesecIzPzar()
    {
        var row = UcitajPrviRed(_xm2PzarPath);
        return Str(row, "ISPZAMES");
    }

    private FirmaInfo UcitajFirmuInfo()
    {
        var path = PronadjiDbf(_folderPath, "firma.dbf");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new FirmaInfo();

        try
        {
            var red = DbfReader.CitajSveZapise(path).FirstOrDefault();
            if (red is null)
                return new FirmaInfo();

            return new FirmaInfo
            {
                Naziv = Str(red, "FIME"),
                Mesto = Str(red, "FMES")
            };
        }
        catch
        {
            return new FirmaInfo();
        }
    }

    private string? PronadjiIliKreirajDbf(string fileName)
    {
        var found = PronadjiDbf(_folderPath, fileName);
        if (!string.IsNullOrWhiteSpace(found) && File.Exists(found))
            return found;

        if (string.IsNullOrWhiteSpace(_folderPath))
            return null;

        var targetPath = Path.Combine(_folderPath, fileName);
        var template = PronadjiTemplateDbf(fileName);
        if (template is null)
            return null;

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.Copy(template, targetPath, overwrite: true);
        return targetPath;
    }

    private static string? PronadjiTemplateDbf(string fileName)
    {
        foreach (var root in KandidatiZaRoot())
        {
            var kandidati = new[]
            {
                Path.Combine(root, "newproject", "templates", "F1", fileName),
                Path.Combine(root, "newproject", "instalacije", "AlgoritamOffice", "templates", "F1", fileName),
                Path.Combine(root, "instalacije", "AlgoritamOffice", "templates", "F1", fileName)
            };

            foreach (var kandidat in kandidati)
            {
                if (File.Exists(kandidat))
                    return kandidat;
            }
        }

        return null;
    }

    private static IEnumerable<string> KandidatiZaRoot()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        DodajParents(roots, AppContext.BaseDirectory);
        DodajParents(roots, Environment.CurrentDirectory);
        return roots;

        static void DodajParents(HashSet<string> target, string? startPath)
        {
            if (string.IsNullOrWhiteSpace(startPath))
                return;

            var info = new DirectoryInfo(startPath);
            while (info != null)
            {
                target.Add(info.FullName);
                info = info.Parent;
            }
        }
    }

    private static string? PronadjiDbf(string folderPath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return null;

        var kandidati = new List<string>();
        if (Directory.Exists(folderPath))
        {
            kandidati.Add(folderPath);
            kandidati.Add(Path.Combine(folderPath, "zarade"));
            kandidati.Add(Path.Combine(folderPath, "data00"));
            kandidati.Add(Path.Combine(folderPath, "01"));
            kandidati.Add(Path.Combine(folderPath, "data01"));
        }

        var parent = Directory.Exists(folderPath) ? Directory.GetParent(folderPath)?.FullName : null;
        if (!string.IsNullOrWhiteSpace(parent))
        {
            kandidati.Add(parent);
            kandidati.Add(Path.Combine(parent, "zarade"));
            kandidati.Add(Path.Combine(parent, "data00"));
            kandidati.Add(Path.Combine(parent, "01"));
            kandidati.Add(Path.Combine(parent, "data01"));
        }

        foreach (var kandidat in kandidati.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(kandidat) || !Directory.Exists(kandidat))
                continue;

            var exact = Path.Combine(kandidat, fileName);
            if (File.Exists(exact))
                return exact;

            var found = Directory.GetFiles(kandidat, "*.dbf", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(found))
                return found;
        }

        return null;
    }

    private static string KljucZaArhivu(Dictionary<string, object?> row)
        => $"{Str(row, "DEKLARAC")}|{Str(row, "REDNIBROJ")}|{Str(row, "MATICNIBR")}|{Str(row, "SIFRAPRIH")}";

    private static string Safe(string? value) => (value ?? string.Empty).Trim();

    private static string Str(Dictionary<string, object?> row, string fieldName)
        => row.TryGetValue(fieldName, out var value) ? (value?.ToString() ?? string.Empty).Trim() : string.Empty;

    private static string NumAsText(Dictionary<string, object?> row, string fieldName)
    {
        if (!row.TryGetValue(fieldName, out var value) || value is null)
            return string.Empty;

        return value switch
        {
            decimal d => d.ToString("0", CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            _ => (value.ToString() ?? string.Empty).Trim()
        };
    }

    private static decimal Dec(Dictionary<string, object?> row, string fieldName)
    {
        if (!row.TryGetValue(fieldName, out var value) || value is null)
            return 0m;

        if (value is decimal d)
            return d;

        var text = (value.ToString() ?? string.Empty).Trim();
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : (decimal.TryParse(text, out parsed) ? parsed : 0m);
    }

    private static DateTime? Dat(Dictionary<string, object?> row, string fieldName)
        => row.TryGetValue(fieldName, out var value) && value is DateTime dt ? dt : null;

    private static decimal PrvaVecaOdNule(decimal first, decimal second)
        => first != 0m ? first : second;

    private static string PrvaNeprazna(string prvi, string drugi)
        => !string.IsNullOrWhiteSpace(prvi) ? prvi : drugi;

    private static string FormatDec(decimal value)
        => value == 0m ? string.Empty : value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string FormatDatC(DateTime? date)
        => date.HasValue ? date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;

    private static string Csv(string? value)
    {
        var text = (value ?? string.Empty).Replace("\"", "\"\"");
        return $"\"{text}\"";
    }

    private static void SplitImePrezime(string imePrezime, out string prezime, out string ime)
    {
        var text = (imePrezime ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            prezime = string.Empty;
            ime = string.Empty;
            return;
        }

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            prezime = parts[0];
            ime = string.Empty;
            return;
        }

        prezime = parts[0];
        ime = string.Join(' ', parts.Skip(1));
    }

    private static string? PromptText(string title, string label, string initial)
    {
        var window = new Window
        {
            Title = title,
            Width = 460,
            Height = 170,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Background = System.Windows.Media.Brushes.WhiteSmoke
        };

        var root = new System.Windows.Controls.Grid { Margin = new Thickness(12) };
        root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });

        var text = new System.Windows.Controls.TextBlock
        {
            Text = label,
            Margin = new Thickness(0, 0, 0, 8),
            FontFamily = new System.Windows.Media.FontFamily("Tahoma"),
            FontSize = 13
        };

        var box = new System.Windows.Controls.TextBox
        {
            Text = initial ?? string.Empty,
            MinWidth = 400,
            Height = 28,
            Padding = new Thickness(6, 3, 6, 3),
            FontFamily = new System.Windows.Media.FontFamily("Tahoma"),
            FontSize = 13
        };

        var buttons = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var ok = new System.Windows.Controls.Button
        {
            Content = "U redu",
            Width = 92,
            Height = 30,
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true
        };

        var cancel = new System.Windows.Controls.Button
        {
            Content = "Odustani",
            Width = 92,
            Height = 30,
            IsCancel = true
        };

        ok.Click += (_, _) => window.DialogResult = true;
        cancel.Click += (_, _) => window.DialogResult = false;

        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);

        System.Windows.Controls.Grid.SetRow(text, 0);
        System.Windows.Controls.Grid.SetRow(box, 1);
        System.Windows.Controls.Grid.SetRow(buttons, 2);

        root.Children.Add(text);
        root.Children.Add(box);
        root.Children.Add(buttons);
        window.Content = root;

        window.Loaded += (_, _) =>
        {
            box.Focus();
            box.SelectAll();
        };

        var result = window.ShowDialog();
        return result == true ? box.Text?.Trim() : null;
    }

    private static List<Dictionary<string, object?>> CitajLdDbfZaMesec(string folderPath, int mesec)
    {
        var rezultat = new List<Dictionary<string, object?>>();
        var poseceni = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var kandidati = new List<string> { "LD.DBF", "LD0.DBF", "LD00.DBF", "LDP.DBF", "LDB.DBF" };
        for (var i = 1; i <= 30; i++)
        {
            kandidati.Add($"LD{i}.DBF");
            kandidati.Add($"LDP{i}.DBF");
        }

        foreach (var name in kandidati)
        {
            var path = PronadjiDbf(folderPath, name);
            if (string.IsNullOrWhiteSpace(path) || !poseceni.Add(path))
                continue;

            try
            {
                var rows = DbfReader.CitajSveZapise(path);
                rezultat.AddRange(rows.Where(r => (int)Dec(r, "MESEC") == mesec));
            }
            catch { }
        }

        return rezultat;
    }

    private sealed class FirmaInfo
    {
        public string Naziv { get; init; } = string.Empty;
        public string Mesto { get; init; } = string.Empty;
    }
}

public partial class PppLdStavka : ObservableObject
{
    private readonly Dictionary<string, object?> _rawValues = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty] private string _deklarac = string.Empty;
    [ObservableProperty] private string _redniBroj = string.Empty;
    [ObservableProperty] private string _prezime = string.Empty;
    [ObservableProperty] private string _ime = string.Empty;
    [ObservableProperty] private string _vrstaId = string.Empty;
    [ObservableProperty] private string _prebival = string.Empty;
    [ObservableProperty] private string _sifraPrih = string.Empty;
    [ObservableProperty] private string _maticniBr = string.Empty;
    [ObservableProperty] private string _brojDana = string.Empty;
    [ObservableProperty] private string _brojSati = string.Empty;
    [ObservableProperty] private string _fondSati = string.Empty;
    [ObservableProperty] private string _bruto = string.Empty;
    [ObservableProperty] private string _osnovicaP = string.Empty;
    [ObservableProperty] private string _porez = string.Empty;
    [ObservableProperty] private string _osnovicaDop = string.Empty;
    [ObservableProperty] private string _pio = string.Empty;
    [ObservableProperty] private string _zdr = string.Empty;
    [ObservableProperty] private string _nez = string.Empty;
    [ObservableProperty] private string _pioBen = string.Empty;
    [ObservableProperty] private string _mfp1 = string.Empty;

    [ObservableProperty] private string _mfp2 = string.Empty;
    [ObservableProperty] private string _mfp3Proc = string.Empty;
    [ObservableProperty] private string _mfp4 = string.Empty;
    [ObservableProperty] private string _mfp5 = string.Empty;
    [ObservableProperty] private string _mfp6 = string.Empty;
    [ObservableProperty] private string _mfp7 = string.Empty;
    [ObservableProperty] private string _mfp8Nepun = string.Empty;
    [ObservableProperty] private string _mfp9NajOsn = string.Empty;
    [ObservableProperty] private string _mfp10Dvez = string.Empty;
    [ObservableProperty] private string _mfp11Neop = string.Empty;
    [ObservableProperty] private string _mfp12 = string.Empty;

    [ObservableProperty] private string _kamPor = string.Empty;
    [ObservableProperty] private string _kamPio = string.Empty;
    [ObservableProperty] private string _kamZdr = string.Empty;
    [ObservableProperty] private string _kamNez = string.Empty;
    [ObservableProperty] private string _kamBen = string.Empty;

    [ObservableProperty] private string _vrstaPrijave = string.Empty;
    [ObservableProperty] private string _godina = string.Empty;
    [ObservableProperty] private string _isplataZaMesec = string.Empty;
    [ObservableProperty] private string _konacna = string.Empty;
    [ObservableProperty] private DateTime? _datumObaveze;
    [ObservableProperty] private DateTime? _datumPlacanja;
    [ObservableProperty] private string _datumObavezeC = string.Empty;
    [ObservableProperty] private string _datumPlacanjaC = string.Empty;
    [ObservableProperty] private string _vrstaIzmene = string.Empty;
    [ObservableProperty] private string _identifikacija = string.Empty;
    [ObservableProperty] private string _brojResenja = string.Empty;
    [ObservableProperty] private string _osnov = string.Empty;
    [ObservableProperty] private string _tipIsplate = string.Empty;
    [ObservableProperty] private string _vrstaIdIsplatioca = string.Empty;
    [ObservableProperty] private string _najniza = string.Empty;
    [ObservableProperty] private string _pib = string.Empty;
    [ObservableProperty] private string _dana = string.Empty;
    [ObservableProperty] private string _brojZapos = string.Empty;
    [ObservableProperty] private string _jmbgPodnosioca = string.Empty;
    [ObservableProperty] private string _maticni = string.Empty;
    [ObservableProperty] private string _naziv = string.Empty;
    [ObservableProperty] private string _sediste = string.Empty;
    [ObservableProperty] private string _telefon = string.Empty;
    [ObservableProperty] private string _ulicaIBr = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _period = string.Empty;
    [ObservableProperty] private string _redniIsplate = string.Empty;
    [ObservableProperty] private string _opstina = string.Empty;
    [ObservableProperty] private string _radnik = string.Empty;
    [ObservableProperty] private string _mesto = string.Empty;
    [ObservableProperty] private string _adresa = string.Empty;
    [ObservableProperty] private string _preneto = string.Empty;
    [ObservableProperty] private string _pioR = string.Empty;
    [ObservableProperty] private string _zdrR = string.Empty;
    [ObservableProperty] private string _nezR = string.Empty;
    [ObservableProperty] private string _pioF = string.Empty;
    [ObservableProperty] private string _zdrF = string.Empty;
    [ObservableProperty] private string _nezF = string.Empty;

    [ObservableProperty] private decimal _nBruto;
    [ObservableProperty] private decimal _nOsnovicaP;
    [ObservableProperty] private decimal _nPorez;
    [ObservableProperty] private decimal _nOsnovicaDop;
    [ObservableProperty] private decimal _nPio;
    [ObservableProperty] private decimal _nZdr;
    [ObservableProperty] private decimal _nNez;
    [ObservableProperty] private decimal _nPioben;
    [ObservableProperty] private decimal _nKamPor;
    [ObservableProperty] private decimal _nKamPio;
    [ObservableProperty] private decimal _nKamZdr;
    [ObservableProperty] private decimal _nKamNez;
    [ObservableProperty] private decimal _nKamBen;
    [ObservableProperty] private decimal _idbr;

    public static PppLdStavka FromRow(Dictionary<string, object?> row)
    {
        var model = new PppLdStavka
        {
            Deklarac = Str(row, "DEKLARAC"),
            RedniBroj = Str(row, "REDNIBROJ"),
            Prezime = Str(row, "PREZIME"),
            Ime = Str(row, "IME"),
            VrstaId = Str(row, "VRSTAID"),
            Prebival = Str(row, "PREBIVAL"),
            SifraPrih = Str(row, "SIFRAPRIH"),
            MaticniBr = Str(row, "MATICNIBR"),
            BrojDana = Str(row, "BROJDANA"),
            BrojSati = Str(row, "BROJSATI"),
            FondSati = Str(row, "FONDSATI"),
            Bruto = Str(row, "BRUTO"),
            OsnovicaP = Str(row, "OSNOVICAP"),
            Porez = Str(row, "POREZ"),
            OsnovicaDop = Str(row, "OSNOVIDOP"),
            Pio = Str(row, "PIO"),
            Zdr = Str(row, "ZDR"),
            Nez = Str(row, "NEZ"),
            PioBen = Str(row, "PIOBEN"),
            Mfp1 = Str(row, "MFP1"),
            Mfp2 = Str(row, "MFP2"),
            Mfp3Proc = Str(row, "MFP3PROC"),
            Mfp4 = Str(row, "MFP4"),
            Mfp5 = Str(row, "MFP5"),
            Mfp6 = Str(row, "MFP6"),
            Mfp7 = Str(row, "MFP7"),
            Mfp8Nepun = Str(row, "MFP8NEPUN"),
            Mfp9NajOsn = Str(row, "MFP9NAJOSN"),
            Mfp10Dvez = Str(row, "MFP10DVEZ"),
            Mfp11Neop = Str(row, "MFP11NEOP"),
            Mfp12 = Str(row, "MFP12"),
            KamPor = Str(row, "KAMPOR"),
            KamPio = Str(row, "KAMPIO"),
            KamZdr = Str(row, "KAMZDR"),
            KamNez = Str(row, "KAMNEZ"),
            KamBen = Str(row, "KAMBEN"),
            VrstaPrijave = Str(row, "VRSTAPRIJ"),
            Godina = Str(row, "GODINA"),
            IsplataZaMesec = Str(row, "ISPZAMES"),
            Konacna = Str(row, "KONACNA"),
            DatumObaveze = Dat(row, "DATUMOBAV"),
            DatumPlacanja = Dat(row, "DATUMPLAC"),
            DatumObavezeC = Str(row, "DATUMOBAVC"),
            DatumPlacanjaC = Str(row, "DATUMPLACC"),
            VrstaIzmene = Str(row, "VRSTAIZM"),
            Identifikacija = Str(row, "IDENTIFIK"),
            BrojResenja = Str(row, "BROJRES"),
            Osnov = Str(row, "OSNOV"),
            TipIsplate = Str(row, "TIPISP"),
            VrstaIdIsplatioca = Str(row, "VRSTAIDISP"),
            Najniza = Str(row, "NAJNIZA"),
            Pib = Str(row, "PIB"),
            Dana = Str(row, "DANA"),
            BrojZapos = Str(row, "BROJZAPOS"),
            JmbgPodnosioca = Str(row, "JMBGPODNOS"),
            Maticni = Str(row, "MATICNI"),
            Naziv = Str(row, "NAZIV"),
            Sediste = Str(row, "SEDISTE"),
            Telefon = Str(row, "TELEFON"),
            UlicaIBr = Str(row, "ULICAIBR"),
            Email = Str(row, "EMAIL"),
            Period = Str(row, "PERIOD"),
            RedniIsplate = NumAsText(row, "REDISPL"),
            Opstina = Str(row, "OPSTINA"),
            Radnik = Str(row, "RADNIK"),
            Mesto = Str(row, "MESTO"),
            Adresa = Str(row, "ADRESA"),
            Preneto = Str(row, "PRENETO"),
            PioR = NumAsText(row, "PIOR"),
            ZdrR = NumAsText(row, "ZDRR"),
            NezR = NumAsText(row, "NEZR"),
            PioF = NumAsText(row, "PIOF"),
            ZdrF = NumAsText(row, "ZDRF"),
            NezF = NumAsText(row, "NEZF"),
            NBruto = Dec(row, "NBRUTO"),
            NOsnovicaP = Dec(row, "NOSNOVICAP"),
            NPorez = Dec(row, "NPOREZ"),
            NOsnovicaDop = Dec(row, "NOSNOVIDOP"),
            NPio = Dec(row, "NPIO"),
            NZdr = Dec(row, "NZDR"),
            NNez = Dec(row, "NNEZ"),
            NPioben = Dec(row, "NPIOBEN"),
            NKamPor = Dec(row, "NKAMPOR"),
            NKamPio = Dec(row, "NKAMPIO"),
            NKamZdr = Dec(row, "NKAMZDR"),
            NKamNez = Dec(row, "NKAMNEZ"),
            NKamBen = Dec(row, "NKAMBEN"),
            Idbr = Dec(row, "IDBR")
        };

        foreach (var pair in row)
            model._rawValues[pair.Key] = pair.Value;

        return model;
    }

    public Dictionary<string, object?> ToRowDictionary()
    {
        var row = new Dictionary<string, object?>(_rawValues, StringComparer.OrdinalIgnoreCase)
        {
            ["DEKLARAC"] = Safe(Deklarac),
            ["REDNIBROJ"] = Safe(RedniBroj),
            ["PREZIME"] = Safe(Prezime),
            ["IME"] = Safe(Ime),
            ["VRSTAID"] = Safe(VrstaId),
            ["PREBIVAL"] = Safe(Prebival),
            ["SIFRAPRIH"] = Safe(SifraPrih),
            ["MATICNIBR"] = Safe(MaticniBr),
            ["BROJDANA"] = Safe(BrojDana),
            ["BROJSATI"] = Safe(BrojSati),
            ["FONDSATI"] = Safe(FondSati),
            ["BRUTO"] = Safe(Bruto),
            ["OSNOVICAP"] = Safe(OsnovicaP),
            ["POREZ"] = Safe(Porez),
            ["OSNOVIDOP"] = Safe(OsnovicaDop),
            ["PIO"] = Safe(Pio),
            ["ZDR"] = Safe(Zdr),
            ["NEZ"] = Safe(Nez),
            ["PIOBEN"] = Safe(PioBen),
            ["MFP1"] = Safe(Mfp1),
            ["MFP2"] = Safe(Mfp2),
            ["MFP3PROC"] = Safe(Mfp3Proc),
            ["MFP4"] = Safe(Mfp4),
            ["MFP5"] = Safe(Mfp5),
            ["MFP6"] = Safe(Mfp6),
            ["MFP7"] = Safe(Mfp7),
            ["MFP8NEPUN"] = Safe(Mfp8Nepun),
            ["MFP9NAJOSN"] = Safe(Mfp9NajOsn),
            ["MFP10DVEZ"] = Safe(Mfp10Dvez),
            ["MFP11NEOP"] = Safe(Mfp11Neop),
            ["MFP12"] = Safe(Mfp12),
            ["KAMPOR"] = Safe(KamPor),
            ["KAMPIO"] = Safe(KamPio),
            ["KAMZDR"] = Safe(KamZdr),
            ["KAMNEZ"] = Safe(KamNez),
            ["KAMBEN"] = Safe(KamBen),
            ["VRSTAPRIJ"] = Safe(VrstaPrijave),
            ["GODINA"] = Safe(Godina),
            ["ISPZAMES"] = Safe(IsplataZaMesec),
            ["KONACNA"] = Safe(Konacna),
            ["DATUMOBAV"] = DatumObaveze,
            ["DATUMPLAC"] = DatumPlacanja,
            ["DATUMOBAVC"] = Safe(DatumObavezeC),
            ["DATUMPLACC"] = Safe(DatumPlacanjaC),
            ["VRSTAIZM"] = Safe(VrstaIzmene),
            ["IDENTIFIK"] = Safe(Identifikacija),
            ["BROJRES"] = Safe(BrojResenja),
            ["OSNOV"] = Safe(Osnov),
            ["TIPISP"] = Safe(TipIsplate),
            ["VRSTAIDISP"] = Safe(VrstaIdIsplatioca),
            ["NAJNIZA"] = Safe(Najniza),
            ["PIB"] = Safe(Pib),
            ["DANA"] = Safe(Dana),
            ["BROJZAPOS"] = Safe(BrojZapos),
            ["JMBGPODNOS"] = Safe(JmbgPodnosioca),
            ["MATICNI"] = Safe(Maticni),
            ["NAZIV"] = Safe(Naziv),
            ["SEDISTE"] = Safe(Sediste),
            ["TELEFON"] = Safe(Telefon),
            ["ULICAIBR"] = Safe(UlicaIBr),
            ["EMAIL"] = Safe(Email),
            ["PERIOD"] = Safe(Period),
            ["REDISPL"] = ParseDec(RedniIsplate),
            ["OPSTINA"] = Safe(Opstina),
            ["RADNIK"] = Safe(Radnik),
            ["MESTO"] = Safe(Mesto),
            ["ADRESA"] = Safe(Adresa),
            ["PRENETO"] = Safe(Preneto),
            ["NBRUTO"] = NBruto,
            ["NOSNOVICAP"] = NOsnovicaP,
            ["NPOREZ"] = NPorez,
            ["NOSNOVIDOP"] = NOsnovicaDop,
            ["NPIO"] = NPio,
            ["NZDR"] = NZdr,
            ["NNEZ"] = NNez,
            ["NPIOBEN"] = NPioben,
            ["NKAMPOR"] = NKamPor,
            ["NKAMPIO"] = NKamPio,
            ["NKAMZDR"] = NKamZdr,
            ["NKAMNEZ"] = NKamNez,
            ["NKAMBEN"] = NKamBen,
            ["PIOR"] = ParseDec(PioR),
            ["ZDRR"] = ParseDec(ZdrR),
            ["NEZR"] = ParseDec(NezR),
            ["PIOF"] = ParseDec(PioF),
            ["ZDRF"] = ParseDec(ZdrF),
            ["NEZF"] = ParseDec(NezF),
            ["IDBR"] = Idbr <= 0 ? 0m : Idbr
        };

        return row;
    }

    private static string Safe(string? value) => (value ?? string.Empty).Trim();

    private static string Str(Dictionary<string, object?> row, string fieldName)
        => row.TryGetValue(fieldName, out var value) ? (value?.ToString() ?? string.Empty).Trim() : string.Empty;

    private static decimal Dec(Dictionary<string, object?> row, string fieldName)
    {
        if (!row.TryGetValue(fieldName, out var value) || value is null)
            return 0m;

        if (value is decimal d)
            return d;

        var text = value.ToString() ?? string.Empty;
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : (decimal.TryParse(text, out parsed) ? parsed : 0m);
    }

    private static DateTime? Dat(Dictionary<string, object?> row, string fieldName)
        => row.TryGetValue(fieldName, out var value) && value is DateTime dt ? dt : null;

    private static string NumAsText(Dictionary<string, object?> row, string fieldName)
    {
        if (!row.TryGetValue(fieldName, out var value) || value is null)
            return string.Empty;

        return value switch
        {
            decimal d => d.ToString("0.##", CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            _ => (value.ToString() ?? string.Empty).Trim()
        };
    }

    private static decimal ParseDec(string? text)
    {
        var value = (text ?? string.Empty).Trim();
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : (decimal.TryParse(value, out parsed) ? parsed : 0m);
    }
}
