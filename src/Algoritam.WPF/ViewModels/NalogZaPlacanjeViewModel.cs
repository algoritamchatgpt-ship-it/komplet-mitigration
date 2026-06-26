using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace Algoritam.WPF.ViewModels;

public partial class NalogZaPlacanjeViewModel : ObservableObject
{
    private static readonly string[] AziroracFields =
    [
        "AZIRORAC",
        "AZIRORAC2",
        "AZIRORAC3",
        "AZIRORAC4",
        "AZIRORAC5",
        "AZIRORAC6",
    ];

    private const string KontoPot = "2020000000";
    private const string KontoDug = "4330000000";
    private const string KontoNepoznat = "9999999999";

    private readonly string _folderPath;
    private DbfTableWriter.DbfSchema? _schema;

    [ObservableProperty] private ObservableCollection<NalogStavka> _stavke = [];
    [ObservableProperty] private NalogStavka? _selectedStavka;
    [ObservableProperty] private string _naslov = "NALOZI ZA PLACANJE - NALPEP";
    [ObservableProperty] private string _poruka = string.Empty;
    [ObservableProperty] private int _brojZapisa;
    [ObservableProperty] private string _ukupnoDug = "0,00";
    [ObservableProperty] private string _ukupnoPot = "0,00";
    [ObservableProperty] private string _globalniSaldo = "0,00";
    [ObservableProperty] private bool _globalniBalansiran = true;
    [ObservableProperty] private string _nalogDug = "-";
    [ObservableProperty] private string _nalogPot = "-";
    [ObservableProperty] private string _nalogSaldo = "-";
    [ObservableProperty] private bool _nalogBalansiran = true;
    [ObservableProperty] private string _tekuciNalog = string.Empty;
    [ObservableProperty] private bool _imaNeacuvana;

    public NalogZaPlacanjeViewModel(string folderPath)
    {
        _folderPath = folderPath;
        UcitajPodatke(_folderPath);
    }

    private void UcitajPodatke(string folderPath)
    {
        foreach (var s in Stavke)
            s.PropertyChanged -= OnStavkaChanged;

        Stavke.Clear();

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            Poruka = "Nije izabrana firma.";
            return;
        }

        var dbfPath = Path.Combine(folderPath, "nalpep.dbf");
        if (!File.Exists(dbfPath))
        {
            Poruka = "Fajl nalpep.dbf nije pronađen.";
            return;
        }

        try
        {
            _schema = DbfTableWriter.LoadSchema(dbfPath);
            var zapisi = DbfReader.CitajSveZapise(dbfPath);

            foreach (var z in zapisi)
            {
                var stavka = NalogStavka.IzZapisa(z);
                stavka.PropertyChanged += OnStavkaChanged;
                Stavke.Add(stavka);
            }

            ImaNeacuvana = false;
            AzurirajZbirove();
            Poruka = $"Ucitano {BrojZapisa} stavki.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska: {ex.Message}";
        }
    }

    private void OnStavkaChanged(object? sender, PropertyChangedEventArgs e)
    {
        ImaNeacuvana = true;

        if (e.PropertyName is nameof(NalogStavka.Dug) or nameof(NalogStavka.Pot))
            AzurirajZbirove();
        else if (e.PropertyName is nameof(NalogStavka.BrNal) && sender == SelectedStavka)
            AzurirajNalogZbirove(SelectedStavka?.BrNal);
    }

    partial void OnSelectedStavkaChanged(NalogStavka? value)
        => AzurirajNalogZbirove(value?.BrNal);

    private void AzurirajZbirove()
    {
        BrojZapisa = Stavke.Count;

        var sumDug = Stavke.Sum(s => s.Dug);
        var sumPot = Stavke.Sum(s => s.Pot);
        var saldo = sumDug - sumPot;

        UkupnoDug = sumDug.ToString("N2");
        UkupnoPot = sumPot.ToString("N2");
        GlobalniSaldo = saldo.ToString("N2");
        GlobalniBalansiran = saldo == 0m;

        AzurirajNalogZbirove(SelectedStavka?.BrNal);
    }

    private void AzurirajNalogZbirove(string? brNal)
    {
        var key = (brNal ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            NalogDug = "-";
            NalogPot = "-";
            NalogSaldo = "-";
            NalogBalansiran = true;
            TekuciNalog = string.Empty;
            return;
        }

        TekuciNalog = $"Nalog: {key}";

        var redovi = Stavke.Where(s => string.Equals(s.BrNal.Trim(), key, StringComparison.OrdinalIgnoreCase)).ToList();
        var sumDug = redovi.Sum(s => s.Dug);
        var sumPot = redovi.Sum(s => s.Pot);
        var saldo = sumDug - sumPot;

        NalogDug = sumDug.ToString("N2");
        NalogPot = sumPot.ToString("N2");
        NalogSaldo = saldo.ToString("N2");
        NalogBalansiran = saldo == 0m;
    }

    [RelayCommand]
    private void DodajStavku()
    {
        var brNal = (SelectedStavka?.BrNal ?? "000001").Trim();
        if (string.IsNullOrWhiteSpace(brNal))
            brNal = "000001";

        var datDok = SelectedStavka?.DatDok ?? DateTime.Today;
        var stavka = new NalogStavka
        {
            BrNal = brNal,
            DatDok = datDok,
            Valuta = datDok,
        };

        stavka.PropertyChanged += OnStavkaChanged;
        Stavke.Add(stavka);
        SelectedStavka = stavka;
        ImaNeacuvana = true;

        AzurirajZbirove();
        Poruka = "Dodat novi red.";
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        if (_schema is null)
        {
            Poruka = "Nema seme - osvezite podatke.";
            return;
        }

        var dbfPath = Path.Combine(_folderPath, "nalpep.dbf");

        try
        {
            DbfTableWriter.WriteTable(
                dbfPath,
                _schema,
                Stavke.ToList(),
                static (st, fn) => fn switch
                {
                    "KONTO" => st.Konto,
                    "DUG" => st.Dug,
                    "POT" => st.Pot,
                    "OPIS" => st.Opis,
                    "SIFRA" => st.Sifra,
                    "NAZIV" => st.Naziv,
                    "BRRAC" => st.BrRac,
                    "DATDOK" => st.DatDok,
                    "VALUTA" => st.Valuta,
                    "BRNAL" => st.BrNal,
                    "POZIVZ" => st.PozivZ,
                    "POZIVP" => st.PozivP,
                    "MP" => st.Mp,
                    "DOK" => st.Dok,
                    _ => null,
                });

            ImaNeacuvana = false;
            Poruka = $"Sačuvano {Stavke.Count} stavki u nalpep.dbf.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri cuvanju: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Osvezi() => UcitajPodatke(_folderPath);

    [RelayCommand]
    private void ObrisiPrazne()
    {
        var prazne = Stavke.Where(s => s.Dug == 0m && s.Pot == 0m).ToList();
        if (prazne.Count == 0)
        {
            Poruka = "Nema praznih redova (DUG=0 i POT=0).";
            return;
        }

        foreach (var s in prazne)
        {
            s.PropertyChanged -= OnStavkaChanged;
            Stavke.Remove(s);
        }

        ImaNeacuvana = true;
        AzurirajZbirove();
        Poruka = $"Obrisano {prazne.Count} praznih redova.";
    }

    [RelayCommand]
    private void ObrisiVrednostiNaloga()
    {
        if (SelectedStavka is null)
        {
            Poruka = "Nije izabrana stavka.";
            return;
        }

        var key = SelectedStavka.BrNal.Trim();
        foreach (var s in Stavke.Where(s => string.Equals(s.BrNal.Trim(), key, StringComparison.OrdinalIgnoreCase)))
        {
            s.Dug = 0m;
            s.Pot = 0m;
        }

        ImaNeacuvana = true;
        AzurirajZbirove();
        Poruka = $"Vrednosti naloga '{key}' ponistene.";
    }

    [RelayCommand]
    private void ObrisiSveVrednosti()
    {
        foreach (var s in Stavke)
        {
            s.Dug = 0m;
            s.Pot = 0m;
        }

        ImaNeacuvana = true;
        AzurirajZbirove();
        Poruka = "Sve vrednosti (DUG/POT) su ponistene.";
    }

    [RelayCommand]
    private void Saldiraj()
    {
        var brNal = (SelectedStavka?.BrNal ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(brNal))
        {
            Poruka = "Selektujte stavku naloga koji zelite da saldirate.";
            return;
        }

        var original = Stavke
            .Where(s => string.Equals(s.BrNal.Trim(), brNal, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (original.Count == 0)
        {
            Poruka = $"Nalog '{brNal}' nema stavke.";
            return;
        }

        // Fox logika: saldiranje na broj racuna (sifra + brrac + datdok + valuta),
        // zatim svodjenje DUG/POT na neto iznos.
        var zbirni = original
            .GroupBy(s => new
            {
                Sifra = s.Sifra.Trim().ToUpperInvariant(),
                BrRac = s.BrRac.Trim().ToUpperInvariant(),
                DatDok = s.DatDok.Date,
                Valuta = s.Valuta.Date,
            })
            .Select(g =>
            {
                var first = g.First();
                var saldo = g.Sum(x => x.Dug) - g.Sum(x => x.Pot);
                return new NalogStavka
                {
                    BrNal = brNal,
                    DatDok = first.DatDok,
                    Valuta = first.Valuta,
                    Konto = first.Konto,
                    Sifra = first.Sifra,
                    Naziv = first.Naziv,
                    BrRac = first.BrRac,
                    PozivZ = first.PozivZ,
                    PozivP = first.PozivP,
                    Mp = first.Mp,
                    Dok = first.Dok,
                    Opis = first.Opis,
                    Dug = saldo >= 0m ? decimal.Round(saldo, 2) : 0m,
                    Pot = saldo < 0m ? decimal.Round(-saldo, 2) : 0m,
                };
            })
            .Where(s => s.Dug != s.Pot)
            .OrderBy(s => s.Sifra)
            .ThenBy(s => s.BrRac)
            .ThenBy(s => s.DatDok)
            .ToList();

        var oldCount = original.Count;

        for (var i = Stavke.Count - 1; i >= 0; i--)
        {
            if (!string.Equals(Stavke[i].BrNal.Trim(), brNal, StringComparison.OrdinalIgnoreCase))
                continue;

            Stavke[i].PropertyChanged -= OnStavkaChanged;
            Stavke.RemoveAt(i);
        }

        foreach (var red in zbirni)
        {
            red.PropertyChanged += OnStavkaChanged;
            Stavke.Add(red);
        }

        SelectedStavka = Stavke.FirstOrDefault(s => string.Equals(s.BrNal.Trim(), brNal, StringComparison.OrdinalIgnoreCase));
        ImaNeacuvana = true;
        AzurirajZbirove();

        Poruka = $"Saldiranje naloga {brNal}: {oldCount} -> {zbirni.Count} stavki.";
    }

    [RelayCommand]
    private void Stampa()
    {
        var brNal = (SelectedStavka?.BrNal ?? string.Empty).Trim();
        var redovi = string.IsNullOrWhiteSpace(brNal)
            ? Stavke.ToList()
            : Stavke.Where(s => string.Equals(s.BrNal.Trim(), brNal, StringComparison.OrdinalIgnoreCase)).ToList();

        if (redovi.Count == 0)
        {
            Poruka = "Nema podataka za stampu.";
            return;
        }

        var data = redovi.Select(s => new NalogPrintRow
        {
            BrNal = s.BrNal,
            DatDok = s.DatDok,
            Valuta = s.Valuta,
            Konto = s.Konto,
            Sifra = s.Sifra,
            Naziv = s.Naziv,
            BrRac = s.BrRac,
            PozivZ = s.PozivZ,
            PozivP = s.PozivP,
            Opis = s.Opis,
            Dug = s.Dug,
            Pot = s.Pot,
            Saldo = s.Dug - s.Pot,
        }).ToList();

        var naslov = string.IsNullOrWhiteSpace(brNal)
            ? "NALOZI ZA PLACANJE - SVE STAVKE"
            : $"NALOZI ZA PLACANJE - NALOG {brNal}";

        var view = new Views.Zarade.LdBolGenericReportView(naslov, data, data.Count);
        view.ShowDialog();
        Poruka = $"Otvoren pregled za stampu ({data.Count} stavki).";
    }

    [RelayCommand]
    private void PreuzmiSdf()
    {
        var file = IzaberiFajl("SDF ili DBF izvod|*.sdf;*.txt;*.dbf|Svi fajlovi|*.*");
        if (string.IsNullOrWhiteSpace(file))
        {
            Poruka = "Preuzimanje SDF otkazano.";
            return;
        }

        if (file.EndsWith(".dbf", StringComparison.OrdinalIgnoreCase))
        {
            UveziIzDbf(file, "SDF");
            return;
        }

        UveziIzSdfTeksta(file, "SDF");
    }

    [RelayCommand]
    private void PreuzmiRf()
    {
        var file = IzaberiFajl("RF ili DBF izvod|*.rf;*.txt;*.dbf|Svi fajlovi|*.*");
        if (string.IsNullOrWhiteSpace(file))
        {
            Poruka = "Preuzimanje RF otkazano.";
            return;
        }

        if (file.EndsWith(".dbf", StringComparison.OrdinalIgnoreCase))
        {
            UveziIzDbf(file, "RF");
            return;
        }

        UveziIzRfTeksta(file, "RF");
    }

    [RelayCommand]
    private void PreuzmiXml()
    {
        var file = IzaberiFajl("XML izvod|*.xml|Svi fajlovi|*.*");
        if (string.IsNullOrWhiteSpace(file))
        {
            Poruka = "Preuzimanje XML otkazano.";
            return;
        }

        UveziIzXml(file);
    }

    private string? IzaberiFajl(string filter)
    {
        var dlg = new OpenFileDialog
        {
            Filter = filter,
            CheckFileExists = true,
            Multiselect = false,
        };

        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    private void UveziIzDbf(string filePath, string tip)
    {
        try
        {
            var source = DbfReader.CitajSveZapise(filePath);
            var stavke = source.Select(MapFromDbfRow).Where(s => s is not null).Select(s => s!).ToList();
            DodajUStavke(stavke, tip);
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri uvozu {tip}: {ex.Message}";
        }
    }

    private void UveziIzSdfTeksta(string filePath, string tip)
    {
        try
        {
            var lines = File.ReadAllLines(filePath)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            var result = new List<IzvodStavka>();
            foreach (var line in lines)
            {
                if (line.Length < 176)
                    continue;

                var racund = Slice(line, 0, 18);
                var naziv = Slice(line, 18, 55);
                var pozivz = Slice(line, 76, 20);
                var svrha = Slice(line, 99, 35);
                var iznos = Slice(line, 134, 13);
                var dp = Slice(line, 147, 1);
                var pozivp = Slice(line, 168, 20);
                var dat = Slice(line, 188, 8);

                var amount = ParsePackedAmount(iznos);
                var dug = dp == "1" || dp.Equals("D", StringComparison.OrdinalIgnoreCase) ? amount : 0m;
                var pot = dug == 0m ? amount : 0m;

                result.Add(new IzvodStavka
                {
                    Naziv = naziv,
                    PozivP = pozivp,
                    PozivZ = pozivz,
                    Svrha = svrha,
                    Racun = racund,
                    Dug = dug,
                    Pot = pot,
                    Datum = ParseDate(dat),
                });
            }

            DodajUStavke(result, tip);
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri uvozu {tip}: {ex.Message}";
        }
    }

    private void UveziIzRfTeksta(string filePath, string tip)
    {
        try
        {
            var lines = File.ReadAllLines(filePath)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            var result = new List<IzvodStavka>();
            foreach (var line in lines)
            {
                if (line.Length < 197)
                    continue;

                var racund = Slice(line, 0, 18);
                var dp = Slice(line, 18, 1);
                var datum = Slice(line, 20, 8);
                var naziv = Slice(line, 30, 36);
                var iznos = Slice(line, 92, 13);
                var pozivz = Slice(line, 113, 22);
                var pozivp = Slice(line, 137, 22);
                var svrha = Slice(line, 159, 46);

                var amount = ParsePackedAmount(iznos);
                var dug = dp.Equals("D", StringComparison.OrdinalIgnoreCase) ? amount : 0m;
                var pot = dug == 0m ? amount : 0m;

                result.Add(new IzvodStavka
                {
                    Naziv = naziv,
                    PozivP = pozivp,
                    PozivZ = pozivz,
                    Svrha = svrha,
                    Racun = racund,
                    Dug = dug,
                    Pot = pot,
                    Datum = ParseDate(datum),
                });
            }

            DodajUStavke(result, tip);
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri uvozu {tip}: {ex.Message}";
        }
    }

    private void UveziIzXml(string filePath)
    {
        try
        {
            var doc = XDocument.Load(filePath);
            var result = new List<IzvodStavka>();

            var candidateElements = doc
                .Descendants()
                .Where(e => e.Attributes().Any(a => a.Name.LocalName == "BrojRacunaPrimaocaPosiljaoca"))
                .ToList();

            foreach (var el in candidateElements)
            {
                var naziv = Attr(el, "NalogKorisnik");
                var racun = Attr(el, "BrojRacunaPrimaocaPosiljaoca");
                var svrha = Attr(el, "Opis");
                var dugText = Attr(el, "Duguje");
                var potText = Attr(el, "Potrazuje");
                var pozivz = Attr(el, "PozivNaBrojZaduzenjaOdobrenja");
                var pozivp = Attr(el, "PozivNaBrojKorisnika");
                var datumText = Attr(el, "DatumValute");

                var dug = ParseDecimalLoose(dugText);
                var pot = ParseDecimalLoose(potText);

                result.Add(new IzvodStavka
                {
                    Naziv = naziv,
                    PozivP = pozivp,
                    PozivZ = pozivz,
                    Svrha = svrha,
                    Racun = racun,
                    Dug = dug,
                    Pot = pot,
                    Datum = ParseDate(datumText),
                });
            }

            DodajUStavke(result, "XML");
        }
        catch (Exception ex)
        {
            Poruka = $"Greska pri uvozu XML: {ex.Message}";
        }
    }

    private IzvodStavka? MapFromDbfRow(Dictionary<string, object?> row)
    {
        var racun = Str(row, "RACUND");
        var naziv = Str(row, "NAZIV");
        var svrha = Str(row, "SVRHA", "OPIS");
        var pozivz = Str(row, "POZIVZ");
        var pozivp = Str(row, "POZIVP");
        var datum = Date(row, "DATDOK", "DATUM", "DATVRE");

        var dug = Dec(row, "DUG");
        var pot = Dec(row, "POT");

        if (dug == 0m && pot == 0m)
        {
            var iznos = Str(row, "IZNOS");
            var dp = Str(row, "DP");
            var amount = ParsePackedAmount(iznos);
            if (dp == "1" || dp.Equals("D", StringComparison.OrdinalIgnoreCase))
                dug = amount;
            else
                pot = amount;
        }

        if (dug == 0m && pot == 0m && string.IsNullOrWhiteSpace(racun) && string.IsNullOrWhiteSpace(naziv))
            return null;

        return new IzvodStavka
        {
            Naziv = naziv,
            PozivP = pozivp,
            PozivZ = pozivz,
            Svrha = svrha,
            Racun = racun,
            Dug = dug,
            Pot = pot,
            Datum = datum,
        };
    }

    private void DodajUStavke(List<IzvodStavka> source, string tip)
    {
        if (source.Count == 0)
        {
            Poruka = $"Nisu pronađene stavke za uvoz ({tip}).";
            return;
        }

        var accountMap = UcitajSifrePoRacunu();
        var brNal = OdrediCiljniBrojNaloga();

        var added = 0;
        foreach (var src in source)
        {
            if (src.Dug == 0m && src.Pot == 0m)
                continue;

            var normalized = NormalizeAccount(src.Racun);
            accountMap.TryGetValue(normalized, out var sifra);

            var red = new NalogStavka
            {
                BrNal = brNal,
                DatDok = src.Datum == default ? DateTime.Today : src.Datum,
                Valuta = src.Datum == default ? DateTime.Today : src.Datum,
                Naziv = src.Naziv,
                PozivP = src.PozivP,
                PozivZ = src.PozivZ,
                Opis = src.Svrha,
                Sifra = sifra ?? string.Empty,
                BrRac = src.PozivP,
                Dug = decimal.Round(src.Dug, 2),
                Pot = decimal.Round(src.Pot, 2),
                Konto = string.IsNullOrWhiteSpace(sifra)
                    ? KontoNepoznat
                    : (src.Pot != 0m ? KontoPot : KontoDug),
            };

            red.PropertyChanged += OnStavkaChanged;
            Stavke.Add(red);
            added++;
        }

        if (added == 0)
        {
            Poruka = $"Nijedna stavka iz {tip} izvoda nema iznos za knjiženje.";
            return;
        }

        ImaNeacuvana = true;
        AzurirajZbirove();

        SelectedStavka = Stavke.LastOrDefault(s => string.Equals(s.BrNal.Trim(), brNal, StringComparison.OrdinalIgnoreCase));
        Poruka = $"Preuzeto {added} stavki iz {tip} izvoda u nalog {brNal}.";
    }

    private Dictionary<string, string> UcitajSifrePoRacunu()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var an0Path = Path.Combine(_folderPath, "an0.dbf");
        if (!File.Exists(an0Path))
            return result;

        try
        {
            var rows = DbfReader.CitajSveZapise(an0Path);
            foreach (var row in rows)
            {
                var sifra = Str(row, "SIFRA").Trim();
                if (string.IsNullOrWhiteSpace(sifra))
                    continue;

                foreach (var field in AziroracFields)
                {
                    var normalized = NormalizeAccount(Str(row, field));
                    if (string.IsNullOrWhiteSpace(normalized))
                        continue;

                    if (!result.ContainsKey(normalized))
                        result[normalized] = sifra;
                }
            }
        }
        catch
        {
            // Ako mapa partnera ne uspe, import i dalje može da radi sa default kontom.
        }

        return result;
    }

    private string OdrediCiljniBrojNaloga()
    {
        var selected = (SelectedStavka?.BrNal ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(selected))
            return selected;

        var max = Stavke
            .Select(s => new string(s.BrNal.Where(char.IsDigit).ToArray()))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => int.TryParse(s, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return (max + 1).ToString("000000", CultureInfo.InvariantCulture);
    }

    private static string NormalizeAccount(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value
            .Trim()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("0", string.Empty, StringComparison.Ordinal);
    }

    private static string Slice(string text, int start, int length)
    {
        if (start < 0 || length <= 0 || start >= text.Length)
            return string.Empty;

        var safeLength = Math.Min(length, text.Length - start);
        return text.Substring(start, safeLength).Trim();
    }

    private static string Attr(XElement element, string localName)
        => element.Attributes().FirstOrDefault(a => a.Name.LocalName == localName)?.Value?.Trim() ?? string.Empty;

    private static decimal ParsePackedAmount(string value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return 0m;

        if (digits.Length == 1)
            return decimal.Parse($"0.0{digits}", CultureInfo.InvariantCulture);

        if (digits.Length == 2)
            return decimal.Parse($"0.{digits}", CultureInfo.InvariantCulture);

        var intPart = digits[..^2];
        var decPart = digits[^2..];
        return decimal.Parse($"{intPart}.{decPart}", CultureInfo.InvariantCulture);
    }

    private static decimal ParseDecimalLoose(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;

        var cleaned = value.Trim().Replace(",", ".", StringComparison.Ordinal);
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return d;

        return ParsePackedAmount(value);
    }

    private static DateTime ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DateTime.Today;

        var v = value.Trim();
        var formats = new[]
        {
            "dd.MM.yyyy",
            "dd/MM/yyyy",
            "yyyy-MM-dd",
            "yyyyMMdd",
            "ddMMyyyy",
        };

        if (DateTime.TryParseExact(v, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        if (DateTime.TryParse(v, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
            return dt;

        return DateTime.Today;
    }

    private static string Str(Dictionary<string, object?> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!row.TryGetValue(key, out var raw) || raw is null)
                continue;

            if (raw is string s)
                return s.Trim();

            return raw.ToString()?.Trim() ?? string.Empty;
        }

        return string.Empty;
    }

    private static decimal Dec(Dictionary<string, object?> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!row.TryGetValue(key, out var raw) || raw is null)
                continue;

            if (raw is decimal dec)
                return dec;

            var text = raw.ToString() ?? string.Empty;
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out dec))
                return dec;

            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out dec))
                return dec;
        }

        return 0m;
    }

    private static DateTime Date(Dictionary<string, object?> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!row.TryGetValue(key, out var raw) || raw is null)
                continue;

            if (raw is DateTime dt && dt != DateTime.MinValue)
                return dt;

            var text = raw.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
                return ParseDate(text);
        }

        return DateTime.Today;
    }
}

public partial class NalogStavka : ObservableObject
{
    [ObservableProperty] private string _brNal = string.Empty;
    [ObservableProperty] private string _konto = string.Empty;
    [ObservableProperty] private string _naziv = string.Empty;
    [ObservableProperty] private string _brRac = string.Empty;
    [ObservableProperty] private DateTime _datDok = DateTime.Today;
    [ObservableProperty] private DateTime _valuta = DateTime.Today;
    [ObservableProperty] private decimal _dug;
    [ObservableProperty] private decimal _pot;
    [ObservableProperty] private string _opis = string.Empty;
    [ObservableProperty] private string _sifra = string.Empty;
    [ObservableProperty] private string _pozivZ = string.Empty;
    [ObservableProperty] private string _pozivP = string.Empty;
    [ObservableProperty] private string _mp = string.Empty;
    [ObservableProperty] private string _dok = string.Empty;

    internal static NalogStavka IzZapisa(Dictionary<string, object?> z) => new()
    {
        BrNal = Str(z, "BRNAL"),
        Konto = Str(z, "KONTO"),
        Naziv = Str(z, "NAZIV"),
        BrRac = Str(z, "BRRAC"),
        DatDok = Date(z, "DATDOK"),
        Valuta = Date(z, "VALUTA"),
        Dug = Dec(z, "DUG"),
        Pot = Dec(z, "POT"),
        Opis = Str(z, "OPIS"),
        Sifra = Str(z, "SIFRA"),
        PozivZ = Str(z, "POZIVZ"),
        PozivP = Str(z, "POZIVP"),
        Mp = Str(z, "MP"),
        Dok = Str(z, "DOK"),
    };

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s : string.Empty;

    private static decimal Dec(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is decimal d ? d : 0m;

    private static DateTime Date(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is DateTime d && d != DateTime.MinValue ? d : DateTime.Today;
}

public class NalogPrintRow
{
    public string BrNal { get; set; } = string.Empty;
    public DateTime DatDok { get; set; }
    public DateTime Valuta { get; set; }
    public string Konto { get; set; } = string.Empty;
    public string Sifra { get; set; } = string.Empty;
    public string Naziv { get; set; } = string.Empty;
    public string BrRac { get; set; } = string.Empty;
    public string PozivZ { get; set; } = string.Empty;
    public string PozivP { get; set; } = string.Empty;
    public string Opis { get; set; } = string.Empty;
    public decimal Dug { get; set; }
    public decimal Pot { get; set; }
    public decimal Saldo { get; set; }
}

public class IzvodStavka
{
    public string Naziv { get; set; } = string.Empty;
    public string PozivP { get; set; } = string.Empty;
    public string PozivZ { get; set; } = string.Empty;
    public string Svrha { get; set; } = string.Empty;
    public string Racun { get; set; } = string.Empty;
    public decimal Dug { get; set; }
    public decimal Pot { get; set; }
    public DateTime Datum { get; set; } = DateTime.Today;
}
