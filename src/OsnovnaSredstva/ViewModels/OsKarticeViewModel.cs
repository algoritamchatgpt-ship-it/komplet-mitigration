using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using OsnovnaSredstva.Views;
using Serilog;
using System.Collections.ObjectModel;

namespace OsnovnaSredstva.ViewModels;

public partial class OsKarticeViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext<OsKarticeViewModel>();
    private readonly AppState _appState;

    private List<OsKartica> _sveKartice = [];

    [ObservableProperty] private ObservableCollection<OsKartica> _kartice = [];
    [ObservableProperty] private OsKartica? _izabranaKartica;
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private string _filterText = "";

    private bool _izmijenjeno;
    public bool ImaNeSnimljenih => _izmijenjeno;

    public string NazivIzabrane =>
        IzabranaKartica == null ? "" : $"{IzabranaKartica.Osifra}   {IzabranaKartica.Naz}";

    partial void OnIzabranaKarticaChanged(OsKartica? value) =>
        OnPropertyChanged(nameof(NazivIzabrane));

    private static readonly HashSet<string> PoznataPolja = new(StringComparer.OrdinalIgnoreCase)
    {
        "OSIFRA","NAZ","DATNAB","BRNAL","KONTO","VRSTA",
        "AG","AGPOD","INVBROJ","MESTO","NAB0","ISP0","SAD0",
        "KOM","CENA","STOPAOT","OSNOVKOR","IZVOR","PRENETO","IDBR"
    };

    public OsKarticeViewModel(AppState appState)
    {
        _appState = appState;
        Ucitaj();
    }

    partial void OnFilterTextChanged(string value) => PrimeniFIlter();

    private void PrimeniFIlter()
    {
        var f = FilterText?.Trim() ?? "";
        if (string.IsNullOrEmpty(f))
        {
            Kartice = new ObservableCollection<OsKartica>(_sveKartice);
        }
        else
        {
            var fl = f.ToLowerInvariant();
            Kartice = new ObservableCollection<OsKartica>(
                _sveKartice.Where(k =>
                    (k.Osifra ?? "").ToLowerInvariant().Contains(fl) ||
                    (k.Naz    ?? "").ToLowerInvariant().Contains(fl)));
        }
    }

    private void Ucitaj()
    {
        var path = DbfPutanja("os0.dbf");
        if (path == null) { Kartice = []; Poruka = "os0.dbf nije pronađen u folderu firme."; return; }

        try
        {
            var reader = new SimpleDbfReader(path);
            var stavke = new List<OsKartica>();

            foreach (var r in reader.Zapisi())
            {
                var k = new OsKartica
                {
                    Osifra   = r.DajString("OSIFRA"),
                    Naz      = r.DajString("NAZ"),
                    DatNab   = r.DajDate("DATNAB"),
                    BrNal    = r.DajString("BRNAL"),
                    Konto    = r.DajString("KONTO"),
                    Vrsta    = r.DajString("VRSTA"),
                    Ag       = r.DajString("AG"),
                    AgPod    = r.DajString("AGPOD"),
                    InvBroj  = r.DajString("INVBROJ"),
                    Mesto    = r.DajString("MESTO"),
                    Nab0     = r.DajDecimal("NAB0"),
                    Isp0     = r.DajDecimal("ISP0"),
                    Sad0     = r.DajDecimal("SAD0"),
                    Kom      = r.DajDecimal("KOM"),
                    Cena     = r.DajDecimal("CENA"),
                    StopaOt  = r.DajDecimal("STOPAOT"),
                    OsnovKor = r.DajString("OSNOVKOR"),
                    Izvor    = r.DajString("IZVOR"),
                    Preneto  = r.DajString("PRENETO"),
                    IDBr     = (int)r.DajDecimal("IDBR"),
                };

                foreach (var field in reader.Fields)
                {
                    if (!PoznataPolja.Contains(field.Name))
                    {
                        k.ExtraPolja[field.Name] = field.Type switch
                        {
                            'D'      => (object?)r.DajDate(field.Name),
                            'N' or 'F' => r.DajDecimal(field.Name),
                            'L'      => r.DajBool(field.Name),
                            _        => r.DajString(field.Name)
                        };
                    }
                }

                stavke.Add(k);
            }

            _sveKartice = stavke;
            PrimeniFIlter();
            Poruka = $"Učitano {_sveKartice.Count} kartica.";
            _log.Debug("OsKartice — učitano {Count} kartica iz {File}", _sveKartice.Count, path);
            _izmijenjeno = false;
        }
        catch (Exception ex)
        {
            _sveKartice = [];
            Kartice = [];
            Poruka = $"Greška: {ex.Message}";
            _log.Error(ex, "OsKartice — greška pri učitavanju");
        }
    }

    [RelayCommand]
    private void Dodaj()
    {
        var sledecaSifra = IzracunajSledecuSifru();
        var nova = new OsKartica { Osifra = sledecaSifra };
        _sveKartice.Add(nova);
        PrimeniFIlter();
        IzabranaKartica = nova;
        Poruka = $"Nova kartica dodata sa sifrom {sledecaSifra}. Unesite podatke i kliknite Sačuvaj.";
        _log.Information("OsKartice — dodana nova kartica {Sifra}", sledecaSifra);
        _izmijenjeno = true;
    }

    [RelayCommand]
    private void Obrisi()
    {
        if (IzabranaKartica == null)
        {
            Poruka = "Nije izabrana kartica.";
            return;
        }

        var opis = $"'{IzabranaKartica.Osifra}' — {IzabranaKartica.Naz}";
        if (System.Windows.MessageBox.Show(
                $"Brisanje kartice OS: {opis}\n\nDa li ste sigurni?",
                "Potvrda brisanja",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
            return;

        _sveKartice.Remove(IzabranaKartica);
        PrimeniFIlter();
        Poruka = $"Kartica {opis} obrisana. Kliknite Sačuvaj.";
        _log.Information("OsKartice — obrisana kartica {Opis}", opis);
        _izmijenjeno = true;
    }

    [RelayCommand]
    private void Kartica()
    {
        if (IzabranaKartica == null)
        {
            Poruka = "Nije izabrana kartica. Kliknite red pa dugme KARTICA.";
            return;
        }

        var path = DbfPutanja("os0.dbf");
        if (path == null) { Poruka = "os0.dbf nije pronađen."; return; }

        try
        {
            var vm = new OsKarticaKarticaViewModel(IzabranaKartica, _appState);
            var win = new OsKarticaKarticaWindow(vm);
            if (win.ShowDialog() == true)
            {
                Poruka = "Kartica je ažurirana. Kliknite Sačuvaj za trajni upis u DBF.";
                _izmijenjeno = true;
            }
        }
        catch (Exception ex) { Poruka = $"Kartica: {ex.Message}"; }
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        var path = DbfPutanja("os0.dbf");
        if (path == null) { Poruka = "os0.dbf nije pronađen."; return; }

        var duplikati = _sveKartice
            .Where(k => !string.IsNullOrWhiteSpace(k.Osifra))
            .GroupBy(k => k.Osifra!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplikati.Count > 0)
        {
            var lista = string.Join(", ", duplikati.Take(5));
            if (duplikati.Count > 5) lista += $" ... (+{duplikati.Count - 5})";
            System.Windows.MessageBox.Show(
                $"Duplikati šifara — snimanje onemogućeno:\n{lista}\n\nIspravite šifre prije snimanja.",
                "Duplikati", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            Poruka = $"Duplikati šifara: {lista}";
            return;
        }

        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, _sveKartice,
                (k, f) => f.ToUpperInvariant() switch
                {
                    "OSIFRA"  => (object?)k.Osifra,
                    "NAZ"     => k.Naz,
                    "DATNAB"  => k.DatNab,
                    "BRNAL"   => k.BrNal,
                    "KONTO"   => k.Konto,
                    "VRSTA"   => k.Vrsta,
                    "AG"      => k.Ag,
                    "AGPOD"   => k.AgPod,
                    "INVBROJ" => k.InvBroj,
                    "MESTO"   => k.Mesto,
                    "NAB0"    => k.Nab0,
                    "ISP0"    => k.Isp0,
                    "SAD0"    => k.Sad0,
                    "KOM"     => k.Kom,
                    "CENA"    => k.Cena,
                    "STOPAOT" => k.StopaOt,
                    "OSNOVKOR"=> k.OsnovKor,
                    "IZVOR"   => k.Izvor,
                    "PRENETO" => k.Preneto,
                    "IDBR"    => (object?)k.IDBr,
                    _         => k.ExtraPolja.TryGetValue(f, out var v) ? v : null
                });

            Poruka = $"Kartice sačuvane ({_sveKartice.Count} zapisa).";
            _log.Information("OsKartice — sačuvano {Count} kartica u {File}", _sveKartice.Count, path);
            _izmijenjeno = false;
        }
        catch (Exception ex)
        {
            Poruka = $"Greška: {ex.Message}";
            _log.Error(ex, "OsKartice — greška pri snimanju");
        }
    }

    [RelayCommand]
    private void Sort()
    {
        _sveKartice = [.. _sveKartice.OrderBy(k => k.Osifra)];
        PrimeniFIlter();
        Poruka = "Sortirano po sifri OS.";
    }

    [RelayCommand]
    private void BrisanjePraznina()
    {
        var count = _sveKartice.RemoveAll(k => string.IsNullOrWhiteSpace(k.Osifra));
        PrimeniFIlter();
        Poruka = count > 0
            ? $"Obrisano {count} praznih kartica. Kliknite Sačuvaj."
            : "Nema praznih kartica.";
        if (count > 0) _izmijenjeno = true;
    }

    // Legacy "IZVOZ TXT-1" dugme na OS0.scx piše TRI fajla DIREKTNO u folder firme
    // (FCREATE u SET DEFAULT folderu), bez dijaloga — ne jedan generički izveštaj.
    // Format je fiksan/regulatorni (POPIS_IMOVINE.TXT za eksterni sistem), repliciran
    // doslovno polje-po-polje iz os0dodaj-porodice PRG-ova, uključujući naizgled
    // čudan spoj OSIFRA+NAZ+GODPRO bez ';' između (tako stoji u originalu) i
    // RADNE_JEDINICE_2012.TXT/RACUNOPOLAGAC_2012.TXT koji pišu samo JEDNU liniju sa
    // VRSTA PRVOG zapisa (GO TOP pre tog dela) — izgleda kao nedovršena legacy funkcija,
    // ali je tako u originalu pa je prepisano bez "ispravki".
    [RelayCommand]
    private void IzvozTxt1()
    {
        var os0Path = DbfPutanja("os0.dbf");
        if (os0Path == null) { Poruka = "os0.dbf nije pronađen."; return; }
        var folder = System.IO.Path.GetDirectoryName(os0Path)!;

        try
        {
            var popisPath = System.IO.Path.Combine(folder, "POPIS_IMOVINE.TXT");
            using (var sw = new System.IO.StreamWriter(popisPath, false, System.Text.Encoding.UTF8))
            {
                foreach (var k in _sveKartice)
                {
                    if (string.IsNullOrWhiteSpace(k.Naz)) continue; // legacy: IF naz<>SPACE(40)

                    var godpro  = (int)OsSaldoViewModel.DajDec(k, "GODPRO");
                    var mtr     = (int)OsSaldoViewModel.DajDec(k, "MTR");
                    var invbroj = (k.InvBroj ?? "");
                    var invbroj4 = invbroj.Length > 3 ? invbroj.Substring(3).Trim() : "";
                    var konto6 = (k.Konto ?? "").PadRight(6).Substring(0, 6).Trim();
                    // Legacy DTOC() bez eksplicitnog SET DATE u ovom PRG-u — koristi se
                    // klasični FoxPro default MM/DD/YY format.
                    var datnab = k.DatNab?.ToString("MM/dd/yy") ?? "";

                    var mm = "20;" +
                        $"{(k.Osifra ?? "").Trim()}{(k.Naz ?? "").Trim()}{godpro}" + ";" +
                        $"{konto6};" +
                        $"{(k.Vrsta ?? "").Trim()};" +
                        $"{mtr};" +
                        $"{invbroj4};" +
                        $"{k.StopaOt.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)};" +
                        $"{datnab};" +
                        $"{k.Nab0.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)};" +
                        $"{k.Sad0.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)};";
                    sw.WriteLine(mm);
                }
            }

            // Legacy: GO TOP pre ova dva fajla — VRSTA prvog zapisa, jedna linija.
            var vrstaPrvog = (_sveKartice.FirstOrDefault()?.Vrsta ?? "").Trim();

            System.IO.File.WriteAllText(
                System.IO.Path.Combine(folder, "RADNE_JEDINICE_2012.TXT"),
                $"{vrstaPrvog};NAZIV RADNE JEDINICE                              \r\n",
                new System.Text.UTF8Encoding(false));

            System.IO.File.WriteAllText(
                System.IO.Path.Combine(folder, "RACUNOPOLAGAC_2012.TXT"),
                $"{vrstaPrvog};IME RACUNOPOLAGACA                                \r\n",
                new System.Text.UTF8Encoding(false));

            Poruka = $"Izvoz TXT-1 završen u folderu firme: POPIS_IMOVINE.TXT, RADNE_JEDINICE_2012.TXT, RACUNOPOLAGAC_2012.TXT.";
            _log.Information("OsKartice — IzvozTxt1 završen u {Folder}", folder);
        }
        catch (Exception ex) { Poruka = $"Greška izvoza: {ex.Message}"; }
    }

    // Legacy "IZVOZ EXCEL" = COPY TO OS0 TYPE XLS FOR naz<>SPACE(40) — SVE kolone os0.dbf,
    // filtrirano na neprazan naziv. (Bilo zamenjeno mestima sa IzvozExcel2 pre ove izmene.)
    [RelayCommand]
    private void IzvozExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Izvoz u CSV (Excel) — sve kolone",
            Filter = "CSV fajlovi (*.csv)|*.csv|Svi fajlovi (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = "os0_izvoz.csv"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var zaIzvoz = _sveKartice.Where(k => !string.IsNullOrWhiteSpace(k.Naz)).ToList();
            var extraKljucevi = zaIzvoz
                .SelectMany(k => k.ExtraPolja.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            using var sw = new System.IO.StreamWriter(dlg.FileName, false, System.Text.Encoding.UTF8);
            sw.WriteLine("Šifra;Naziv;DatNab;BrNal;Konto;Vrsta;AG;AgPod;InvBroj;Mesto;NAB0;ISP0;SAD0;Kom;Cena;StopaOt;OsnovKor;Izvor;Preneto;"
                       + string.Join(";", extraKljucevi));
            foreach (var k in zaIzvoz)
            {
                var bazni = string.Join(";",
                    k.Osifra?.Trim() ?? "",
                    k.Naz?.Trim() ?? "",
                    k.DatNab?.ToString("dd.MM.yyyy") ?? "",
                    k.BrNal?.Trim() ?? "",
                    k.Konto?.Trim() ?? "",
                    k.Vrsta?.Trim() ?? "",
                    k.Ag?.Trim() ?? "",
                    k.AgPod?.Trim() ?? "",
                    k.InvBroj?.Trim() ?? "",
                    k.Mesto?.Trim() ?? "",
                    k.Nab0.ToString("N2"),
                    k.Isp0.ToString("N2"),
                    k.Sad0.ToString("N2"),
                    k.Kom.ToString("N2"),
                    k.Cena.ToString("N2"),
                    k.StopaOt.ToString("N3"),
                    k.OsnovKor?.Trim() ?? "",
                    k.Izvor?.Trim() ?? "",
                    k.Preneto?.Trim() ?? "");
                var extra = string.Join(";", extraKljucevi.Select(ek =>
                    k.ExtraPolja.TryGetValue(ek, out var v) ? v?.ToString() ?? "" : ""));
                sw.WriteLine(bazni + ";" + extra);
            }
            Poruka = $"Izvoz CSV (sve kolone) završen: {dlg.FileName} ({zaIzvoz.Count} zapisa).";
        }
        catch (Exception ex) { Poruka = $"Greška izvoza: {ex.Message}"; }
    }

    // Legacy "IZVOZ EXCEL 2" = COPY TO OS0 TYPE XLS FOR naz<>SPACE(40) FIELDS
    // osifra,naz,godpro,konto,vrsta,mtr,invbroj,stopaot,datnab,nab0,isp0,sad0,procena,razlproc
    // — TAČNO ovih 14 kolona, tim redosledom.
    [RelayCommand]
    private void IzvozExcel2()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Izvoz u CSV (Excel) — 14 kolona (legacy FIELDS lista)",
            Filter = "CSV fajlovi (*.csv)|*.csv|Svi fajlovi (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = "os0_izvoz2.csv"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var zaIzvoz = _sveKartice.Where(k => !string.IsNullOrWhiteSpace(k.Naz)).ToList();

            using var sw = new System.IO.StreamWriter(dlg.FileName, false, System.Text.Encoding.UTF8);
            sw.WriteLine("OSIFRA;NAZ;GODPRO;KONTO;VRSTA;MTR;INVBROJ;STOPAOT;DATNAB;NAB0;ISP0;SAD0;PROCENA;RAZLPROC");
            foreach (var k in zaIzvoz)
                sw.WriteLine(string.Join(";",
                    k.Osifra?.Trim() ?? "",
                    k.Naz?.Trim() ?? "",
                    OsSaldoViewModel.DajDec(k, "GODPRO").ToString("0"),
                    k.Konto?.Trim() ?? "",
                    k.Vrsta?.Trim() ?? "",
                    OsSaldoViewModel.DajDec(k, "MTR").ToString("0"),
                    k.InvBroj?.Trim() ?? "",
                    k.StopaOt.ToString("N2"),
                    k.DatNab?.ToString("dd.MM.yyyy") ?? "",
                    k.Nab0.ToString("N2"),
                    k.Isp0.ToString("N2"),
                    k.Sad0.ToString("N2"),
                    OsSaldoViewModel.DajDec(k, "PROCENA").ToString("N2"),
                    OsSaldoViewModel.DajDec(k, "RAZLPROC").ToString("N2")));
            Poruka = $"Izvoz CSV (14 kolona) završen: {dlg.FileName} ({zaIzvoz.Count} zapisa).";
        }
        catch (Exception ex) { Poruka = $"Greška izvoza: {ex.Message}"; }
    }

    [RelayCommand]
    private void GrupeZaAmortizaciju()
    {
        var vm = new OsSifarnikViewModel(_appState);
        vm.AktivniTab = 1;
        new OsSifarnikWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PodgrupeZaAmortizaciju()
    {
        var vm = new OsSifarnikViewModel(_appState);
        vm.AktivniTab = 2;
        new OsSifarnikWindow(vm).ShowDialog();
    }

    // Legacy: DO printer_bullzip WITH mdata02,'OS0NEPOKR', "OSIFRA=MOSIFRA" — štampa
    // detaljni list NEKRETNINE samo za TRENUTNO IZABRANI zapis (jedan zapis, ne lista).
    // FRX pixel-layout je van obima (dogovoreno) — najbliži ekvivalent je otvaranje iste
    // detaljne kartice koju koristi i KARTICA dugme (OsKarticaKarticaWindow), koja već
    // sadrži sva nekretninska polja (parcela, katastar, list nepokretnosti...).
    [RelayCommand]
    private void Nepokretnosti()
    {
        if (IzabranaKartica == null) { Poruka = "Nije izabrana kartica. Kliknite red pa dugme NEPOKRETNOSTI."; return; }
        Kartica();
    }

    // Legacy "POPISNA LISTA" dugme na OS0.scx → DO FORM OSPOPIS → SET FILTER TO
    // EMPTY(datprod) (samo aktivna, neotpisana sredstva) + mesto/mtr/konto/ag/agpod/grupa
    // filter iz ospopis.dbf, pa "SA KOLIČINOM"(OSPOPIS0)/"BEZ KOLIČINE"(OSPOPISBK) varijanta.
    // FRX pixel-layout van obima — prikaz kao tabelarni pregled (isti pattern kao
    // Evidencija's PregledMrs), SA/BEZ KOLIČINE mapirano na puni/skraćeni prikaz.
    [RelayCommand]
    private void PopisnaLista()
    {
        var kriterijumi = UcitajPopisKriterijume();
        var wnd = new OsPopisFilterWindow(OsPopisFilterMode.Mrs, kriterijumi);
        if (wnd.ShowDialog() != true) return;

        kriterijumi = wnd.ReadData();
        SacuvajPopisKriterijume(kriterijumi);

        var aktivna = _sveKartice.Where(k => !DatProd(k).HasValue);
        var filtrirano = PrimeniPopisFilter(aktivna, kriterijumi).ToList();

        var bezKolicine = wnd.Action == OsPopisFilterAction.PregledSkraceni;
        var vm = OsMrsViewModel.MrsPregled(filtrirano, skraceni: bezKolicine);
        vm.Naslov = bezKolicine ? "POPISNA LISTA — BEZ KOLIČINE" : "POPISNA LISTA — SA KOLIČINOM";
        new OsMrsWindow(vm).ShowDialog();
    }

    private static DateTime? DatProd(OsKartica k)
    {
        if (!k.ExtraPolja.TryGetValue("DATPROD", out var v) || v is null) return null;
        if (v is DateTime dt) return dt;
        if (v is string s && DateTime.TryParse(s, out dt)) return dt;
        return null;
    }

    private static IEnumerable<OsKartica> PrimeniPopisFilter(IEnumerable<OsKartica> kartice, OsPopisFilterData data)
    {
        var mmesto = (data.Mesto ?? string.Empty).Trim();
        var mmtr = data.Mtr < 1 ? 0 : data.Mtr;
        var mkonto = (data.Konto ?? string.Empty).Trim();
        var mag = (data.Ag ?? string.Empty).Trim();
        var magpod = (data.AgPod ?? string.Empty).Trim();
        var mgrupa = (data.Grupa ?? string.Empty).Trim();

        return kartice.Where(k =>
            (string.IsNullOrWhiteSpace(mmesto) || string.Equals((k.Mesto ?? string.Empty).Trim(), mmesto, StringComparison.OrdinalIgnoreCase)) &&
            (mmtr == 0 || (int)OsSaldoViewModel.DajDec(k, "MTR") == mmtr) &&
            (string.IsNullOrWhiteSpace(mag) || string.Equals((k.Ag ?? string.Empty).Trim(), mag, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(magpod) || string.Equals((k.AgPod ?? string.Empty).Trim(), magpod, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(mkonto) || (k.Konto ?? string.Empty).TrimEnd().StartsWith(mkonto, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(mgrupa) || string.Equals(
                k.ExtraPolja.TryGetValue("GRUPA", out var g) ? Convert.ToString(g)?.Trim() ?? "" : "",
                mgrupa, StringComparison.OrdinalIgnoreCase)));
    }

    private OsPopisFilterData UcitajPopisKriterijume()
    {
        var path = DbfPutanja("ospopis.dbf");
        if (path == null) return new OsPopisFilterData();
        try
        {
            var reader = new SimpleDbfReader(path);
            foreach (var r in reader.Zapisi())
            {
                var mtr = r.DajInt("MTR");
                return new OsPopisFilterData
                {
                    Mesto = r.DajString("MESTO").Trim(),
                    Mtr = mtr < 1 ? 0 : mtr,
                    Konto = r.DajString("KONTO").Trim(),
                    Ag = r.DajString("AG").Trim(),
                    AgPod = r.DajString("AGPOD").Trim(),
                    Grupa = r.DajString("GRUPA").Trim()
                };
            }
        }
        catch { /* nastavljamo sa praznim kriterijumima */ }
        return new OsPopisFilterData();
    }

    private void SacuvajPopisKriterijume(OsPopisFilterData data)
    {
        var path = DbfPutanja("ospopis.dbf");
        if (path == null) return;
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            var reader = new SimpleDbfReader(path);
            var redovi = new List<Dictionary<string, object?>>();
            foreach (var r in reader.Zapisi())
            {
                var red = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var f in reader.Fields)
                {
                    red[f.Name] = f.Type switch
                    {
                        'D' => (object?)r.DajDate(f.Name),
                        'N' or 'F' => r.DajDecimal(f.Name),
                        'L' => r.DajBool(f.Name),
                        _ => r.DajString(f.Name)
                    };
                }
                redovi.Add(red);
            }
            if (redovi.Count == 0) redovi.Add(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase));

            var rr = redovi[0];
            rr["MESTO"] = data.Mesto?.Trim() ?? string.Empty;
            rr["MTR"] = data.Mtr < 1 ? 0 : data.Mtr;
            rr["KONTO"] = data.Konto?.Trim() ?? string.Empty;
            rr["AG"] = data.Ag?.Trim() ?? string.Empty;
            rr["AGPOD"] = data.AgPod?.Trim() ?? string.Empty;
            rr["GRUPA"] = data.Grupa?.Trim() ?? string.Empty;

            DbfTableWriter.WriteTable(path, schema, redovi, (red, f) => red.TryGetValue(f, out var v) ? v : null);
        }
        catch { /* ne prekidamo rad ako ne uspe upis kriterijuma */ }
    }

    [RelayCommand]
    private void Osvezi() => Ucitaj();

    [RelayCommand]
    private void Prvi()
    {
        if (Kartice.Count > 0) IzabranaKartica = Kartice[0];
    }

    [RelayCommand]
    private void Zadnji()
    {
        if (Kartice.Count > 0) IzabranaKartica = Kartice[^1];
    }

    [RelayCommand]
    private void Dole()
    {
        if (IzabranaKartica == null || Kartice.Count == 0) return;
        var idx = Kartice.IndexOf(IzabranaKartica);
        if (idx < Kartice.Count - 1) IzabranaKartica = Kartice[idx + 1];
    }

    [RelayCommand]
    private void Gore()
    {
        if (IzabranaKartica == null || Kartice.Count == 0) return;
        var idx = Kartice.IndexOf(IzabranaKartica);
        if (idx > 0) IzabranaKartica = Kartice[idx - 1];
    }

    private string? DbfPutanja(string ime) => DbfHelper.NadjiDbf(_appState, ime);

    private string IzracunajSledecuSifru()
    {
        var max = 0;
        foreach (var kartica in _sveKartice)
        {
            var raw = (kartica.Osifra ?? string.Empty).Trim();
            if (int.TryParse(raw, out var broj) && broj > max)
                max = broj;
        }
        // FoxPro: STR(MOSIFRA,4,0) = space-padded right-aligned 4-char string
        return (max + 1).ToString().PadLeft(4);
    }
}
