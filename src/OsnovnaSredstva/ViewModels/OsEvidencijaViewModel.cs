using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using OsnovnaSredstva.Views;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;

namespace OsnovnaSredstva.ViewModels;

public partial class OsEvidencijaViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext<OsEvidencijaViewModel>();
    private readonly AppState _appState;
    private readonly string _dbfIme;

    private List<OsKartica> _sveKartice = [];
    private List<OsKartica> _osnovniPoredak = [];
    private int _sortOrder;

    [ObservableProperty] private ObservableCollection<OsKartica> _kartice = [];
    [ObservableProperty] private OsKartica? _izabranaKartica;
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private string _filterText = "";

    private bool _izmijenjeno;
    public bool ImaNeSnimljenih => _izmijenjeno;

    public string NazivIzabrane =>
        IzabranaKartica == null ? "" : $"{IzabranaKartica.Osifra}   {IzabranaKartica.Naz}";

    public string InfoIzabrane =>
        IzabranaKartica == null ? "" :
        $"Konto: {IzabranaKartica.Konto}   Mesto: {IzabranaKartica.Mesto}   InvBroj: {IzabranaKartica.InvBroj}   AG: {IzabranaKartica.Ag}   AgPod: {IzabranaKartica.AgPod}";

    partial void OnIzabranaKarticaChanged(OsKartica? value)
    {
        OnPropertyChanged(nameof(NazivIzabrane));
        OnPropertyChanged(nameof(InfoIzabrane));
    }

    partial void OnFilterTextChanged(string value) => PrimeniFiIlter();

    private static readonly HashSet<string> PoznataPolja = new(StringComparer.OrdinalIgnoreCase)
    {
        "OSIFRA","NAZ","DATNAB","BRNAL","KONTO","VRSTA",
        "AG","AGPOD","INVBROJ","MESTO","NAB0","ISP0","SAD0",
        "KOM","CENA","STOPAOT","OSNOVKOR","IZVOR","PRENETO","IDBR"
    };

    public OsEvidencijaViewModel(AppState appState, string dbfIme = "os.dbf")
    {
        _appState = appState;
        _dbfIme = dbfIme;
        Ucitaj();
    }

    private void PrimeniFiIlter()
    {
        var f = FilterText?.Trim() ?? "";
        Kartice = string.IsNullOrEmpty(f)
            ? new ObservableCollection<OsKartica>(_sveKartice)
            : new ObservableCollection<OsKartica>(
                _sveKartice.Where(k =>
                    (k.Osifra ?? "").ToLowerInvariant().Contains(f.ToLowerInvariant()) ||
                    (k.Naz    ?? "").ToLowerInvariant().Contains(f.ToLowerInvariant())));
    }

    private void Ucitaj()
    {
        var path = DbfPutanja(_dbfIme);
        if (path == null) { Kartice = []; Poruka = $"{_dbfIme} nije pronađen u folderu firme."; return; }

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
                            'D'        => (object?)r.DajDate(field.Name),
                            'N' or 'F' => r.DajDecimal(field.Name),
                            'L'        => r.DajBool(field.Name),
                            _          => r.DajString(field.Name)
                        };
                    }
                }

                stavke.Add(k);
            }

            _sveKartice = stavke;
            _osnovniPoredak = [.. stavke];
            PrimeniFiIlter();
            Poruka = $"Učitano {_sveKartice.Count} zapisa iz {_dbfIme}.";
            _log.Debug("OsEvidencija — učitano {Count} zapisa iz {File}", _sveKartice.Count, path);
            _izmijenjeno = false;
        }
        catch (Exception ex)
        {
            _sveKartice = [];
            _osnovniPoredak = [];
            Kartice = [];
            Poruka = $"Greška: {ex.Message}";
            _log.Error(ex, "OsEvidencija — greška pri učitavanju {File}", _dbfIme);
        }
    }

    [RelayCommand] private void Osvezi() => Ucitaj();

    [RelayCommand]
    private void Dodaj()
    {
        var max = _sveKartice.Select(k => k.IDBr).DefaultIfEmpty(0).Max();
        var nova = new OsKartica { IDBr = max + 1, Preneto = "N" };
        _sveKartice.Add(nova);
        _osnovniPoredak.Add(nova);
        PrimeniFiIlter();
        IzabranaKartica = nova;
        Poruka = "Novi red dodan. Unesite podatke i kliknite Sačuvaj.";
        _log.Information("OsEvidencija — dodan novi red (IDBr={IDBr}) u {File}", nova.IDBr, _dbfIme);
        _izmijenjeno = true;
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        var path = DbfPutanja(_dbfIme);
        if (path == null) { Poruka = $"{_dbfIme} nije pronađen."; return; }

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
                    "OSIFRA"   => (object?)k.Osifra,
                    "NAZ"      => k.Naz,
                    "DATNAB"   => k.DatNab,
                    "BRNAL"    => k.BrNal,
                    "KONTO"    => k.Konto,
                    "VRSTA"    => k.Vrsta,
                    "AG"       => k.Ag,
                    "AGPOD"    => k.AgPod,
                    "INVBROJ"  => k.InvBroj,
                    "MESTO"    => k.Mesto,
                    "NAB0"     => k.Nab0,
                    "ISP0"     => k.Isp0,
                    "SAD0"     => k.Sad0,
                    "KOM"      => k.Kom,
                    "CENA"     => k.Cena,
                    "STOPAOT"  => k.StopaOt,
                    "OSNOVKOR" => k.OsnovKor,
                    "IZVOR"    => k.Izvor,
                    "PRENETO"  => k.Preneto,
                    "IDBR"     => (object?)k.IDBr,
                    _          => k.ExtraPolja.TryGetValue(f, out var v) ? v : null
                });
            Poruka = $"Sačuvano ({_sveKartice.Count} zapisa).";
            _log.Information("OsEvidencija — sačuvano {Count} zapisa u {File}", _sveKartice.Count, path);
            _izmijenjeno = false;
        }
        catch (Exception ex)
        {
            Poruka = $"Greška: {ex.Message}";
            _log.Error(ex, "OsEvidencija — greška pri snimanju {File}", _dbfIme);
        }
    }

    [RelayCommand]
    private void Sort()
    {
        _sortOrder = _sortOrder >= 8 ? 0 : _sortOrder + 1;

        _sveKartice = _sortOrder switch
        {
            1 => [.. _sveKartice.OrderBy(k => (k.Osifra ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)],
            2 => [.. _sveKartice.OrderBy(k => (k.Naz ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)],
            3 => [.. _sveKartice.OrderBy(k => (k.Konto ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)],
            4 => [.. _sveKartice.OrderBy(k => (k.Ag ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)],
            5 => [.. _sveKartice.OrderBy(k => (k.AgPod ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)],
            6 => [.. _sveKartice.OrderBy(k => (k.InvBroj ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)],
            7 => [.. _sveKartice.OrderBy(k => (k.Mesto ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)],
            8 => [.. _sveKartice.OrderBy(k => k.IDBr)],
            _ => [.. _osnovniPoredak]
        };

        var staraSifra = IzabranaKartica?.Osifra;
        PrimeniFiIlter();
        if (!string.IsNullOrWhiteSpace(staraSifra))
        {
            var vrati = Kartice.FirstOrDefault(k => string.Equals(k.Osifra, staraSifra, StringComparison.OrdinalIgnoreCase));
            if (vrati != null) IzabranaKartica = vrati;
        }

        Poruka = $"Sortirano (redosled {_sortOrder}/8).";
    }

    [RelayCommand]
    private void Obrisi()
    {
        if (IzabranaKartica == null) { Poruka = "Nije izabran red za brisanje."; return; }
        var naziv = $"{IzabranaKartica.Osifra?.Trim()} — {IzabranaKartica.Naz?.Trim()}";
        if (System.Windows.MessageBox.Show($"Obrisati karticu:\n{naziv}?",
                "Brisanje kartice", System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes) return;
        _sveKartice.Remove(IzabranaKartica);
        _osnovniPoredak.Remove(IzabranaKartica);
        IzabranaKartica = null;
        PrimeniFiIlter();
        Poruka = "Kartica obrisana. Kliknite Sačuvaj.";
        _izmijenjeno = true;
    }

    [RelayCommand]
    private void BrisanjePraznina()
    {
        var count = _sveKartice.RemoveAll(k => string.IsNullOrWhiteSpace(k.Osifra));
        _osnovniPoredak.RemoveAll(k => string.IsNullOrWhiteSpace(k.Osifra));
        PrimeniFiIlter();
        Poruka = count > 0
            ? $"Obrisano {count} praznih kartica. Kliknite Sačuvaj."
            : "Nema praznih kartica.";
        if (count > 0) _izmijenjeno = true;
    }

    [RelayCommand]
    private void PreuzmiIzOs0()
    {
        var os0Path = DbfPutanja("os0.dbf");
        if (os0Path == null) { Poruka = "os0.dbf nije pronađen u folderu firme."; return; }

        try
        {
            var postojeceSifre = _sveKartice
                .Select(k => (k.Osifra ?? "").Trim().ToUpperInvariant())
                .Where(s => s.Length > 0)
                .ToHashSet();

            var reader = new SimpleDbfReader(os0Path);
            int dodate = 0;
            var maxIdbr = _sveKartice.Count > 0 ? _sveKartice.Max(k => k.IDBr) : 0;

            foreach (var r in reader.Zapisi())
            {
                var sifra = r.DajString("OSIFRA").Trim();
                if (string.IsNullOrWhiteSpace(sifra)) continue;
                if (postojeceSifre.Contains(sifra.ToUpperInvariant())) continue;

                var nova = new OsKartica
                {
                    IDBr     = ++maxIdbr,
                    Osifra   = sifra,
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
                };

                foreach (var field in reader.Fields)
                {
                    if (!PoznataPolja.Contains(field.Name))
                    {
                        nova.ExtraPolja[field.Name] = field.Type switch
                        {
                            'D'        => (object?)r.DajDate(field.Name),
                            'N' or 'F' => r.DajDecimal(field.Name),
                            'L'        => r.DajBool(field.Name),
                            _          => r.DajString(field.Name)
                        };
                    }
                }

                _sveKartice.Add(nova);
                _osnovniPoredak.Add(nova);
                postojeceSifre.Add(sifra.ToUpperInvariant());
                dodate++;
            }

            PrimeniFiIlter();
            Poruka = dodate > 0
                ? $"Preuzeto {dodate} novih kartica iz OS0. Kliknite Sačuvaj."
                : "Sve kartice iz OS0 već postoje u Evidenciji.";
            if (dodate > 0) _izmijenjeno = true;
            _log.Information("PreuzmiIzOs0 — preuzeto {Count} kartica iz os0.dbf", dodate);
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri preuzimanju iz OS0: {ex.Message}";
            _log.Error(ex, "Greška pri PreuzmiIzOs0");
        }
    }

    [RelayCommand] private void Prvi()   { if (Kartice.Count > 0) IzabranaKartica = Kartice[0]; }
    [RelayCommand] private void Zadnji() { if (Kartice.Count > 0) IzabranaKartica = Kartice[^1]; }
    [RelayCommand] private void Dole()
    {
        if (IzabranaKartica == null || Kartice.Count == 0) return;
        var idx = Kartice.IndexOf(IzabranaKartica);
        if (idx < Kartice.Count - 1) IzabranaKartica = Kartice[idx + 1];
    }
    [RelayCommand] private void Gore()
    {
        if (IzabranaKartica == null || Kartice.Count == 0) return;
        var idx = Kartice.IndexOf(IzabranaKartica);
        if (idx > 0) IzabranaKartica = Kartice[idx - 1];
    }

    [RelayCommand]
    private void TrazenjeSifre()
    {
        var dlg = new OsEvidencijaPretragaWindow(
            "TRAZENJE SIFRE",
            "SIFRA OSNOVNOG SREDSTVA",
            4);

        if (dlg.ShowDialog() != true) return;

        var unos = dlg.Unos.Trim();
        if (string.IsNullOrWhiteSpace(unos)) return;

        int.TryParse(unos, out var trazeniBroj);
        var nadjeno = _sveKartice
            .OrderBy(k => (k.Osifra ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(k =>
            {
                var sifra = (k.Osifra ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(sifra)) return false;
                if (int.TryParse(sifra, out var broj) && trazeniBroj > 0) return broj == trazeniBroj;
                return sifra.StartsWith(unos, StringComparison.OrdinalIgnoreCase);
            });

        if (nadjeno != null)
        {
            SelektujKarticu(nadjeno);
            Poruka = $"Pronađena: {nadjeno.Osifra?.Trim()} - {nadjeno.Naz}";
            return;
        }

        Poruka = $"Sifra '{unos}' nije pronađena.";
    }

    [RelayCommand]
    private void TrazenjeInventarnogBroja()
    {
        var dlg = new OsEvidencijaPretragaWindow(
            "TRAZENJE INVENTARNOG BROJA",
            "INVENTARNI BROJ OSNOVNOG SREDSTVA",
            20);

        if (dlg.ShowDialog() != true) return;

        var unos = dlg.Unos.Trim();
        if (string.IsNullOrWhiteSpace(unos)) return;

        var nadjeno = _sveKartice
            .OrderBy(k => (k.InvBroj ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(k =>
                (k.InvBroj ?? string.Empty).Trim().StartsWith(unos, StringComparison.OrdinalIgnoreCase));

        if (nadjeno != null)
        {
            SelektujKarticu(nadjeno);
            Poruka = $"Pronađena: {nadjeno.Osifra?.Trim()} - {nadjeno.Naz} (InvBroj: {nadjeno.InvBroj?.Trim()})";
            return;
        }

        Poruka = $"Inventarski broj '{unos}' nije pronađen.";
    }

    // Legacy "PREGLED KARTICA" dugme na OS.scx → DO FORM OSPOPIS4 → saldo po kontu
    // za kartice nabavljene pre početka perioda (vidi OsSaldoViewModel.KarticaPoKontu).
    [RelayCommand]
    private void PregledKartica()
    {
        var kriterijumi = OsPopisFilterSupport.Ucitaj(_appState);
        var wnd = new OsPopisFilterWindow(OsPopisFilterMode.Kartica, kriterijumi);
        if (wnd.ShowDialog() != true)
            return;

        kriterijumi = wnd.ReadData();
        OsPopisFilterSupport.Sacuvaj(_appState, kriterijumi);
        var filtrirano = OsPopisFilterSupport.Primeni(_sveKartice, kriterijumi);

        var vm = OsMrsViewModel.KarticePregled(filtrirano);
        new OsMrsWindow(vm).ShowDialog();
    }

    // Nije iz legacy OS.scx (nema DblClick/dugme za otvaranje pojedinačne kartice na
    // glavnom Evidencija ekranu) — zadržano kao koristan dodatak, ali odvojeno od
    // legacy "PREGLED KARTICA" naziva da ne bude pogrešno protumačeno kao 1:1 prepis.
    [RelayCommand] private void DetaljiKartice()
    {
        if (IzabranaKartica == null) { Poruka = "Nije izabran red."; return; }
        var vm = new OsKarticaKarticaViewModel(IzabranaKartica, _appState);
        var win = new OsKarticaKarticaWindow(vm);
        if (win.ShowDialog() == true) { Poruka = "Kartica ažurirana. Kliknite Sačuvaj."; _izmijenjeno = true; }
    }

    // Legacy "ZADNJE U POČETNO" (OSPRENOSZUP.scx, "PRENOS ZADNJEG STANJA U POČETNO"):
    // KRSTI tabele — kopira TEKUĆE vrednosti iz OS (Evidencija: NAB/ISP/SAD/NAB2/ISP2/SAD2)
    // u POČETNE vrednosti OS0 (Kartice: NAB0/ISP0/SAD0/NAB02/ISP02/SAD02), spojeno po OSIFRA.
    // NE menja ništa u os.dbf (ova VM-ova sopstvena tabela) — original samo čita iz OS,
    // upisuje u OS0. Takođe resetuje IZNOSULAG/DATULAG na SVIM os0 zapisima (i nespojenim).
    // Pošto cilj nije os.dbf nego os0.dbf, upis ide DIREKTNO (ne čeka se Sačuvaj ovog VM-a).
    [RelayCommand]
    private void ZadnjeUPocetno()
    {
        var os0Path = DbfPutanja("os0.dbf");
        if (os0Path == null) { Poruka = "os0.dbf nije pronađen u folderu firme."; return; }

        if (System.Windows.MessageBox.Show(
                "DA LI ŽELITE PRENOS ZADNJEG STANJA U POČETNO?\n\n" +
                "Ovo upisuje TEKUĆE vrednosti iz Evidencije (NAB/ISP/SAD/NAB2/ISP2/SAD2) kao " +
                "POČETNE vrednosti u Kartice OS (os0.dbf, spojeno po šifri), i resetuje " +
                "IZNOSULAG/DATULAG na svim karticama. Upisuje se DIREKTNO u os0.dbf.",
                "PRENOS ZADNJEG STANJA U POČETNO",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes)
            return;

        try
        {
            var poOsifri = _sveKartice
                .Where(k => !string.IsNullOrWhiteSpace(k.Osifra))
                .GroupBy(k => k.Osifra!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First());

            var schema = DbfTableWriter.LoadSchema(os0Path);
            var reader = new SimpleDbfReader(os0Path);
            var redovi = new List<Dictionary<string, object?>>();
            var azurirano = 0;

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

                var sifra = r.DajString("OSIFRA").Trim();
                if (!string.IsNullOrWhiteSpace(sifra) && poOsifri.TryGetValue(sifra, out var k))
                {
                    red["NAB0"]  = k.Nab0;                  // MNAB  = NAB
                    red["SAD0"]  = XDec(k, "SAD");          // MSAD  = SAD
                    red["ISP0"]  = XDec(k, "ISP");          // MISP  = ISP
                    red["NAB02"] = XDec(k, "NAB2");         // MNAB2 = NAB2
                    red["SAD02"] = XDec(k, "SAD2");         // MSAD2 = SAD2
                    red["ISP02"] = XDec(k, "ISP2");         // MISP2 = ISP2
                    azurirano++;
                }

                // Legacy: REPLACE ALL iznosulag WITH 0 / datulag WITH CTOD(' / / ') — bezuslovno.
                red["IZNOSULAG"] = 0m;
                red["DATULAG"] = null;

                redovi.Add(red);
            }

            DbfTableWriter.WriteTable(os0Path, schema, redovi, (red, f) => red.TryGetValue(f, out var v) ? v : null);
            Poruka = $"Prenos zadnjeg stanja u početno završen — ažurirano {azurirano}/{redovi.Count} kartica u os0.dbf.";
            _log.Information("ZadnjeUPocetno — ažurirano {Az}/{Sve} kartica u os0.dbf", azurirano, redovi.Count);
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri prenosu zadnjeg stanja: {ex.Message}";
            _log.Error(ex, "Greška pri ZadnjeUPocetno");
        }
    }

    [RelayCommand]
    private void SaldoKonta()
    {
        var izbor = new OsSaldoKontaIzborWindow();
        if (izbor.ShowDialog() != true) return;

        var (periodOd, _) = ProcitajPeriod();

        var vm = izbor.Action switch
        {
            OsSaldoKontaIzborAction.SaldoSintetika => OsSaldoViewModel.PoKontuSintetika(_sveKartice),
            OsSaldoKontaIzborAction.SaldoAnalitika => OsSaldoViewModel.PoKontu(_sveKartice),
            OsSaldoKontaIzborAction.SaldoNabavkePoAg => OsSaldoViewModel.SaldoNabavkePoAgrupama(_sveKartice, periodOd),
            // Legacy: "POČETNO STANJE" dugme unutar SALDO KONTA lanca (ospopis3.scx)
            // zove DO OSPOPIS4 — ISTU rutinu kao "PREGLED KARTICA" dugme na OS.scx.
            OsSaldoKontaIzborAction.PocetnoStanje => OsSaldoViewModel.KarticaPoKontu(_sveKartice, periodOd),
            _ => null
        };

        if (vm == null) return;
        new OsSaldoWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void SaldoMesta()
    {
        var dlg = new OsEvidencijaPretragaWindow(
            "SALDO PO MESTIMA",
            "KONTO",
            8);

        var konto = string.Empty;
        if (dlg.ShowDialog() == true)
            konto = dlg.Unos?.Trim() ?? string.Empty;

        var vm = OsSaldoViewModel.PoMestu(_sveKartice, konto);
        new OsSaldoWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PregledMrs()
    {
        var kriterijumi = UcitajPopisKriterijume();
        var wnd = new OsPopisFilterWindow(OsPopisFilterMode.Mrs, kriterijumi);
        if (wnd.ShowDialog() != true) return;

        kriterijumi = wnd.ReadData();
        SacuvajPopisKriterijume(kriterijumi);

        var filtrirano = PrimeniPopisFilter(_sveKartice, kriterijumi);
        var skraceni = wnd.Action == OsPopisFilterAction.PregledSkraceni;
        var vm = OsMrsViewModel.MrsPregled(filtrirano, skraceni);
        new OsMrsWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PreglPoreskaStara()
    {
        var kriterijumi = UcitajPopisKriterijume();
        var wnd = new OsPopisFilterWindow(OsPopisFilterMode.Poreska, kriterijumi);
        if (wnd.ShowDialog() != true) return;

        kriterijumi = wnd.ReadData();
        SacuvajPopisKriterijume(kriterijumi);

        var filtrirano = PrimeniPopisFilter(_sveKartice, kriterijumi)
            .Where(k => string.IsNullOrWhiteSpace(DajExtra(k, "NACINOB")));

        var vm = OsMrsViewModel.PoreskaStara(filtrirano);
        new OsMrsWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void PreglPoreskaNova()
    {
        var kriterijumi = UcitajPopisKriterijume();
        var wnd = new OsPopisFilterWindow(OsPopisFilterMode.Poreska, kriterijumi);
        if (wnd.ShowDialog() != true) return;

        kriterijumi = wnd.ReadData();
        SacuvajPopisKriterijume(kriterijumi);

        var filtrirano = PrimeniPopisFilter(_sveKartice, kriterijumi)
            .Where(k => !string.IsNullOrWhiteSpace(DajExtra(k, "NACINOB")));

        var vm = OsMrsViewModel.PoreskaNova(filtrirano);
        new OsMrsWindow(vm).ShowDialog();
    }

    // ═══ PRENOS IZ ŠIFARNIKA (osprenos.scx) — DOSLOVNI legacy ZAP + APPEND FROM ═══
    // Legacy: ZAP (briše SVE redove os.dbf) pa APPEND FROM os0.dbf FOR NAZ<>SPACE(40).
    // Razlikuje se od PrenosCommand ispod (bezbedan merge) — ovo je destruktivna zamena,
    // tačno kao u OSPRENOS.scx. Brisanje se primenjuje tek na Sačuvaj (isti obrazac kao
    // za sve ostale komande u ovom VM-u — ništa se ne piše na disk dok korisnik ne potvrdi).
    [RelayCommand]
    private void PrenosIzSifarnika()
    {
        var os0Path = DbfPutanja("os0.dbf");
        if (os0Path == null) { Poruka = "os0.dbf nije pronađen u folderu firme."; return; }

        if (System.Windows.MessageBox.Show(
                "DA LI ŽELITE PRENOS IZ ŠIFARNIKA?\n\n" +
                "OVO ĆE OBRISATI SVE postojeće kartice u Evidenciji OS i zameniti ih " +
                $"kompletnim sadržajem iz Kartica OS (os0.dbf, {_sveKartice.Count} → zamena).\n\n" +
                "Podaci se ne brišu na disku dok ne kliknete Sačuvaj.",
                "PRENOS IZ ŠIFARNIKA",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes)
            return;

        try
        {
            var reader = new SimpleDbfReader(os0Path);
            var nove = new List<OsKartica>();
            var idbr = 0;

            foreach (var r in reader.Zapisi())
            {
                // Legacy: COPY TO ... FOR NAZ<>SPACE(40) — preskače redove s praznim nazivom.
                if (string.IsNullOrEmpty(r.DajString("NAZ"))) continue;

                var nova = new OsKartica
                {
                    IDBr     = ++idbr,
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
                };

                foreach (var field in reader.Fields)
                {
                    if (!PoznataPolja.Contains(field.Name))
                    {
                        nova.ExtraPolja[field.Name] = field.Type switch
                        {
                            'D'        => (object?)r.DajDate(field.Name),
                            'N' or 'F' => r.DajDecimal(field.Name),
                            'L'        => r.DajBool(field.Name),
                            _          => r.DajString(field.Name)
                        };
                    }
                }

                nove.Add(nova);
            }

            _sveKartice = nove;
            _osnovniPoredak = [.. nove];
            PrimeniFiIlter();
            IzabranaKartica = null;
            Poruka = $"Prenos iz šifarnika završen — Evidencija zamenjena sa {nove.Count} kartica iz os0.dbf. Kliknite Sačuvaj.";
            _log.Information("PrenosIzSifarnika — Evidencija zamenjena sa {Count} kartica iz os0.dbf", nove.Count);
            _izmijenjeno = true;
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri prenosu iz šifarnika: {ex.Message}";
            _log.Error(ex, "Greška pri PrenosIzSifarnika");
        }
    }

    // ═══ PRENOS IZ KARTICA OS (os0.dbf → os.dbf) ═══
    // Uzima SVE zapise iz Kartica OS i upisuje ih u Evidenciju OS:
    //   • Ako šifra već postoji → ažurira polja iz os0
    //   • Ako šifra ne postoji → dodaje novi red
    [RelayCommand]
    private void Prenos()
    {
        var os0Path = DbfPutanja("os0.dbf");
        if (os0Path == null) { Poruka = "os0.dbf (Kartice OS) nije pronađen u folderu firme."; return; }

        if (System.Windows.MessageBox.Show(
                "Prenijeti SVE kartice iz Kartica OS u Evidenciju OS?\n\n" +
                "• Postojeći redovi (ista šifra) biće AŽURIRANI iz Kartica OS\n" +
                "• Novi redovi (kojih nema u Evidenciji) biće DODANI\n\n" +
                "Podaci se ne čuvaju automatski — kliknite Sačuvaj nakon prenosa.",
                "Prenos iz Kartica OS",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
            return;

        try
        {
            var reader = new SimpleDbfReader(os0Path);

            // indeks postojećih kartica po šifri (case-insensitive)
            var indeks = _sveKartice
                .Where(k => !string.IsNullOrWhiteSpace(k.Osifra))
                .ToDictionary(k => k.Osifra!.Trim().ToUpperInvariant(), k => k);

            var maxIdbr = _sveKartice.Count > 0 ? _sveKartice.Max(k => k.IDBr) : 0;
            int azurirano = 0, dodato = 0;

            foreach (var r in reader.Zapisi())
            {
                var sifra = r.DajString("OSIFRA").Trim();
                if (string.IsNullOrWhiteSpace(sifra)) continue;

                var kljuc = sifra.ToUpperInvariant();

                if (indeks.TryGetValue(kljuc, out var postojeca))
                {
                    // ažuriraj postojeću karticu poljima iz os0
                    postojeca.Naz      = r.DajString("NAZ");
                    postojeca.DatNab   = r.DajDate("DATNAB");
                    postojeca.BrNal    = r.DajString("BRNAL");
                    postojeca.Konto    = r.DajString("KONTO");
                    postojeca.Vrsta    = r.DajString("VRSTA");
                    postojeca.Ag       = r.DajString("AG");
                    postojeca.AgPod    = r.DajString("AGPOD");
                    postojeca.InvBroj  = r.DajString("INVBROJ");
                    postojeca.Mesto    = r.DajString("MESTO");
                    postojeca.Nab0     = r.DajDecimal("NAB0");
                    postojeca.Isp0     = r.DajDecimal("ISP0");
                    postojeca.Sad0     = r.DajDecimal("SAD0");
                    postojeca.Kom      = r.DajDecimal("KOM");
                    postojeca.Cena     = r.DajDecimal("CENA");
                    postojeca.StopaOt  = r.DajDecimal("STOPAOT");
                    postojeca.OsnovKor = r.DajString("OSNOVKOR");
                    postojeca.Izvor    = r.DajString("IZVOR");
                    postojeca.Preneto  = r.DajString("PRENETO");

                    // extra polja iz os0 koja ne postoje u poznatim poljima
                    foreach (var field in reader.Fields)
                    {
                        if (!PoznataPolja.Contains(field.Name))
                        {
                            postojeca.ExtraPolja[field.Name] = field.Type switch
                            {
                                'D'        => (object?)r.DajDate(field.Name),
                                'N' or 'F' => r.DajDecimal(field.Name),
                                'L'        => r.DajBool(field.Name),
                                _          => r.DajString(field.Name)
                            };
                        }
                    }
                    azurirano++;
                }
                else
                {
                    // dodaj novu karticu
                    var nova = new OsKartica
                    {
                        IDBr     = ++maxIdbr,
                        Osifra   = sifra,
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
                    };

                    foreach (var field in reader.Fields)
                    {
                        if (!PoznataPolja.Contains(field.Name))
                        {
                            nova.ExtraPolja[field.Name] = field.Type switch
                            {
                                'D'        => (object?)r.DajDate(field.Name),
                                'N' or 'F' => r.DajDecimal(field.Name),
                                'L'        => r.DajBool(field.Name),
                                _          => r.DajString(field.Name)
                            };
                        }
                    }

                    _sveKartice.Add(nova);
                    _osnovniPoredak.Add(nova);
                    indeks[kljuc] = nova;
                    dodato++;
                }
            }

            PrimeniFiIlter();
            Poruka = $"Prenos završen — ažurirano {azurirano}, dodato {dodato} kartica iz os0.dbf. Kliknite Sačuvaj.";
            _log.Information("Prenos iz os0 — ažurirano {Az}, dodato {Dod}", azurirano, dodato);
            if (azurirano > 0 || dodato > 0) _izmijenjeno = true;
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri prenosu iz Kartica OS: {ex.Message}";
            _log.Error(ex, "Greška pri Prenos iz os0");
        }
    }

    [RelayCommand]
    private void Podaci()
    {
        // UNOS PODATAKA čita os.dbf s DISKA i ažurira DATUM0/DATUM1/BRMES.
        // Ako postoje nesačuvane kartice (samo u memoriji, nisu u os.dbf),
        // UNOS PODATAKA ih neće vidjeti i one neće biti ažurirane.
        // Zato: automatski sačuvaj prije otvaranja PODACI dijaloga.
        if (_izmijenjeno)
        {
            var odg = System.Windows.MessageBox.Show(
                $"Evidencija ima {_sveKartice.Count} nesačuvanih kartica.\n\n" +
                "UNOS PODATAKA može ažurirati samo kartice koje su snimljene u os.dbf.\n\n" +
                "Sačuvati sada (preporučeno)?",
                "Nesačuvane izmjene",
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Question);

            if (odg == System.Windows.MessageBoxResult.Cancel)
                return;

            if (odg == System.Windows.MessageBoxResult.Yes)
            {
                Sacuvaj();
                // Ako je Sacuvaj() bio blokiran (npr. duplikati), _izmijenjeno je i dalje true
                if (_izmijenjeno)
                {
                    Poruka = "PODACI otkazan — ispravite greške i sačuvajte evidenciju.";
                    return;
                }
            }
        }

        var vm = new OsPodaciViewModel(_appState);
        var win = new OsPodaciWindow(vm);
        win.ShowDialog();
        // NE pozivamo Ucitaj() ovdje — UNOS PODATAKA ažurira samo DATUM0/DATUM1/BRMES
        // u os.dbf ali ne mijenja listu kartica.
        // Osvježimo samo DATUM0/DATUM1/BRMES u memoriji kako bi OBRAČUN koristio
        // ispravne vrijednosti.
        if (vm.UnosPodatakaIzvrsen)
            OsveziBrmesIzDiska();
        Poruka = string.IsNullOrWhiteSpace(vm.Poruka) ? "" : $"Podaci: {vm.Poruka}";
    }

    /// <summary>
    /// Nakon PODACI → UNOS PODATAKA, os.dbf je ažuriran na disku.
    /// Ovaj metod učitava samo DATUM0/DATUM1/BRMES iz os.dbf i osvježava
    /// in-memory ExtraPolja bez zamjene cijele liste kartica.
    /// </summary>
    private void OsveziBrmesIzDiska()
    {
        var path = DbfPutanja(_dbfIme);
        if (path == null) return;

        try
        {
            var reader = new SimpleDbfReader(path);

            // izgradi mapu osifra → (datum0, datum1, brmes) iz tekućeg stanja na disku
            var mapa = new Dictionary<string, (DateTime? datum0, DateTime? datum1, decimal brmes)>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var r in reader.Zapisi())
            {
                var sifra = r.DajString("OSIFRA").Trim();
                if (string.IsNullOrWhiteSpace(sifra)) continue;
                mapa[sifra] = (r.DajDate("DATUM0"), r.DajDate("DATUM1"), r.DajDecimal("BRMES"));
            }

            int osvjezeno = 0;
            foreach (var k in _sveKartice)
            {
                var sifra = (k.Osifra ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(sifra)) continue;
                if (!mapa.TryGetValue(sifra, out var vals)) continue;

                k.ExtraPolja["DATUM0"] = vals.datum0;
                k.ExtraPolja["DATUM1"] = vals.datum1;
                k.ExtraPolja["BRMES"]  = vals.brmes;
                osvjezeno++;
            }

            _log.Debug("OsveziBrmesIzDiska — osvježeno {Count} kartica (DATUM0/DATUM1/BRMES)", osvjezeno);
        }
        catch (Exception ex)
        {
            // Obračun može nastaviti sa vrijednostima koje su već u memoriji;
            // ne prekidamo rad ako osvježavanje ne uspije.
            _log.Warning(ex, "OsveziBrmesIzDiska — greška pri osvježavanju iz {File}", _dbfIme);
        }
    }

    [RelayCommand]
    private void Obracun()
    {
        if (_sveKartice.Count == 0) { Poruka = "Nema kartica za obračun."; return; }

        if (System.Windows.MessageBox.Show(
                $"Izračunati amortizaciju za sve kartice ({_sveKartice.Count})?\n\n" +
                "Ažurira AMORT, ISP, SAD (MRS) i AMORT2, ISP2, SAD2 (PP).",
                "Obračun amortizacije",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
            return;

        var granVrednost = new DateTime(2019, 1, 1);
        int obradjeno = 0;

        foreach (var k in _sveKartice)
        {
            // Fox: Replace SAD0 With NAB0-ISP0 / Replace SAD02 With NAB02-ISP02
            var sad0  = k.Nab0 - k.Isp0;
            var nab02 = XDec(k, "NAB02");
            var isp02 = XDec(k, "ISP02");
            var sad02 = nab02 - isp02;
            k.Sad0 = sad0;
            k.ExtraPolja["SAD02"] = sad02;

            var datStartAm = XDate(k, "DATSTARTAM");
            var nacinob    = DajExtra(k, "NACINOB").ToUpperInvariant();
            var ag         = (k.Ag ?? string.Empty).Trim();
            var brmes      = (int)XDec(k, "BRMES");
            var stopaot2   = XDec(k, "STOPAOT2");
            var procgod    = XDec(k, "PROCGOD");
            var iznosulag  = XDec(k, "IZNOSULAG");
            var datulag    = XDate(k, "DATULAG");
            var datum0     = XDate(k, "DATUM0");
            var datum1     = XDate(k, "DATUM1");

            bool isNovo = datStartAm.HasValue && datStartAm.Value >= granVrednost;

            // ═══ BLOK 1: STARA OSNOVNA SREDSTVA (datstartam < 01/01/2019) ═══
            if (!isNovo)
            {
                // PP
                if (nab02 != 0)
                {
                    var mamort = ag == "1"
                        ? Math.Round(nab02  * stopaot2 * brmes / (12m * 100m), 2)
                        : Math.Round(sad02  * stopaot2 * brmes / (12m * 100m), 2);
                    var mmamort = Math.Abs(mamort) > Math.Abs(nab02) - Math.Abs(isp02) ? sad02 : mamort;
                    k.ExtraPolja["AMORT2"] = mmamort;
                }
                else k.ExtraPolja["AMORT2"] = 0m;

                var amort2a = XDec(k, "AMORT2");
                if (Math.Abs(isp02) + Math.Abs(amort2a) > Math.Abs(nab02))
                    k.ExtraPolja["AMORT2"] = nab02 - isp02;

                var amort2 = XDec(k, "AMORT2");
                k.ExtraPolja["NAB2"] = nab02;
                k.ExtraPolja["ISP2"] = isp02 + amort2;
                k.ExtraPolja["SAD2"] = nab02 - (isp02 + amort2);

                // MRS
                if (k.Nab0 != 0)
                {
                    var mamort = procgod != 0
                        ? Math.Round(sad0    * k.StopaOt * brmes / (12m * procgod * 100m), 2)
                        : Math.Round(k.Nab0  * k.StopaOt * brmes / (12m * 100m), 2);
                    var mmamort = Math.Abs(mamort) > Math.Abs(k.Nab0) - Math.Abs(k.Isp0) ? sad0 : mamort;
                    k.ExtraPolja["AMORT"] = mmamort;
                }
                else k.ExtraPolja["AMORT"] = 0m;

                var amortA = XDec(k, "AMORT");
                if (Math.Abs(k.Isp0) + Math.Abs(amortA) > Math.Abs(k.Nab0))
                    k.ExtraPolja["AMORT"] = k.Nab0 - k.Isp0;

                var amort = XDec(k, "AMORT");
                k.ExtraPolja["NAB"] = k.Nab0;
                k.ExtraPolja["ISP"] = k.Isp0 + amort;
                k.ExtraPolja["SAD"] = k.Nab0 - (k.Isp0 + amort);
            }

            // ═══ BLOK 2: NOVA NEMATERIJALNA ULAGANJA — MRS samo (datstartam >= 2019, nacinob='014') ═══
            // Legacy OSOBRAC.PRG: "Nova osnovna sredstva racunovodstveni (MRS) obracun amortizacije
            // Samo za nacinob ='014' nematerijalna ulaganja" — ovaj blok NE dira AMORT2/NAB2/ISP2/SAD2,
            // za razliku od POA bloka. Za isNovo sa nacinob koji nije ni "014" ni "POA" legacy PRG
            // nema nijedan blok uopšte (polja ostaju netaknuta iz prethodnog obračuna) — namjerno
            // ne računamo ništa za taj slučaj, isto kao original.
            if (isNovo && nacinob == "014")
            {
                if (k.Nab0 != 0)
                {
                    var mamort = procgod != 0
                        ? Math.Round(k.Nab0 * k.StopaOt * brmes / (12m * procgod * 100m), 2)
                        : Math.Round(k.Nab0 * k.StopaOt * brmes / (12m * 100m), 2);
                    var mmamort = Math.Abs(mamort) > Math.Abs(k.Nab0) - Math.Abs(k.Isp0) ? sad0 : mamort;
                    k.ExtraPolja["AMORT"] = mmamort;
                }
                else k.ExtraPolja["AMORT"] = 0m;

                var amortA = XDec(k, "AMORT");
                if (Math.Abs(k.Isp0) + Math.Abs(amortA) > Math.Abs(k.Nab0))
                    k.ExtraPolja["AMORT"] = k.Nab0 - k.Isp0;

                var amort = XDec(k, "AMORT");
                k.ExtraPolja["NAB"] = k.Nab0;
                k.ExtraPolja["ISP"] = k.Isp0 + amort;
                k.ExtraPolja["SAD"] = k.Nab0 - (k.Isp0 + amort);
            }

            // ═══ BLOK 3: NOVA POA SREDSTVA (datstartam >= 2019 AND nacinob='POA') ═══
            // Legacy: "If SAD0 # 0" u OSOBRAC.PRG obavija SAMO PP/AMORT2/NAB2 podblok.
            // MRS/AMORT/NAB/ISP/SAD i ISP2/SAD2 se računaju BEZUSLOVNO za svaki POA novi
            // osnovno sredstvo, bez obzira na SAD0 — zato sad0!=0 NIJE uslov cijelog bloka.
            if (isNovo && nacinob == "POA")
            {
                bool ulagUPeriodu = datulag.HasValue && datum0.HasValue && datum1.HasValue
                    && datulag.Value >= datum0.Value && datulag.Value <= datum1.Value;

                // PP — broj DANA (ne meseci); samo kada je SAD0 # 0 (legacy ugnježdeni If)
                decimal wnab02 = nab02;
                if (sad0 != 0)
                {
                    wnab02 = ulagUPeriodu ? nab02 + iznosulag : nab02;

                    if (nab02 != 0)
                    {
                        decimal wbrmesDana = 0m;
                        if (datum0.HasValue && datum1.HasValue)
                        {
                            if (datum0.Value.Day != 1)
                            {
                                var prviSledeci = new DateTime(datum0.Value.Year, datum0.Value.Month, 1).AddMonths(1);
                                wbrmesDana = (decimal)(datum1.Value - prviSledeci).TotalDays + 1;
                            }
                            else
                            {
                                wbrmesDana = (decimal)(datum1.Value - datum0.Value).TotalDays + 1;
                            }
                            if (brmes == 0) wbrmesDana = 0;
                        }
                        k.ExtraPolja["POLJE1"] = wbrmesDana;

                        var mamort2 = Math.Round(wnab02 * stopaot2 * wbrmesDana / (365m * 100m), 2);
                        k.ExtraPolja["AMORT2"] = mamort2;

                        var a2check = XDec(k, "AMORT2");
                        if (Math.Abs(isp02) + Math.Abs(a2check) > Math.Abs(wnab02))
                            k.ExtraPolja["AMORT2"] = wnab02 - isp02;
                    }
                    else k.ExtraPolja["AMORT2"] = 0m;

                    k.ExtraPolja["NAB2"] = wnab02;
                }

                // MRS — sa IZNOSULAG ako je ulaganje u periodu (bezuslovno, izvan SAD0 provere)
                decimal wnab0, wAMORT;
                if (ulagUPeriodu && datum1.HasValue && datulag.HasValue)
                {
                    var wbrmesMrs = datum1.Value.Month - datulag.Value.Month + 1;
                    wnab0  = k.Nab0 + iznosulag;
                    wAMORT = Math.Round(iznosulag * k.StopaOt * wbrmesMrs / (12m * 100m), 2);
                }
                else { wnab0 = k.Nab0; wAMORT = 0m; }

                if (wnab0 != 0)
                {
                    var base0 = wnab0 - iznosulag;
                    var mamort = procgod != 0
                        ? Math.Round(base0 * k.StopaOt * brmes / (12m * procgod * 100m), 2)
                        : Math.Round(base0 * k.StopaOt * brmes / (12m * 100m), 2);
                    mamort += wAMORT;
                    var mmamort = Math.Abs(mamort) > Math.Abs(wnab0) - Math.Abs(k.Isp0) ? sad0 : mamort;
                    k.ExtraPolja["AMORT"] = mmamort;
                }
                else k.ExtraPolja["AMORT"] = 0m;

                var amortB = XDec(k, "AMORT");
                if (Math.Abs(k.Isp0) + Math.Abs(amortB) > Math.Abs(wnab0))
                    k.ExtraPolja["AMORT"] = wnab0 - k.Isp0;

                // ISP2/SAD2 — uzimamo manju amortizaciju (MRS ili PP)
                var amortFin  = XDec(k, "AMORT");
                var amort2Fin = XDec(k, "AMORT2");
                var wnab2     = XDec(k, "NAB2");
                if (amortFin < amort2Fin)
                {
                    k.ExtraPolja["ISP2"] = isp02 + amortFin;
                    k.ExtraPolja["SAD2"] = wnab2 - (isp02 + amortFin);
                }
                else
                {
                    k.ExtraPolja["ISP2"] = isp02 + amort2Fin;
                    k.ExtraPolja["SAD2"] = sad0 != 0 ? wnab2 - (isp02 + amort2Fin) : sad02;
                }

                k.ExtraPolja["NAB"] = wnab0;
                k.ExtraPolja["ISP"] = k.Isp0 + amortFin;
                k.ExtraPolja["SAD"] = wnab0 - (k.Isp0 + amortFin);
            }

            // ═══ BLOK 4: PAM/RAM — za sva NOVA sredstva ═══
            if (isNovo)
            {
                var amortF  = XDec(k, "AMORT");
                var amort2F = XDec(k, "AMORT2");
                if (nacinob == "014")
                    k.ExtraPolja["RAM"] = amortF;
                else if (amortF >= amort2F)
                    k.ExtraPolja["PAM"] = amort2F;
                else
                    k.ExtraPolja["RAM"] = amortF;
            }

            obradjeno++;
        }

        Poruka = $"Obračun završen — {obradjeno} kartica obrađeno. Kliknite Sačuvaj.";
        _log.Information("OsEvidencija — obračun amortizacije završen, obrađeno {Count} kartica", obradjeno);
        if (obradjeno > 0) _izmijenjeno = true;
    }

    private static DateTime? XDate(OsKartica k, string polje)
    {
        if (!k.ExtraPolja.TryGetValue(polje, out var v) || v is null) return null;
        if (v is DateTime dt) return dt;
        if (v is string s && DateTime.TryParse(s, out dt)) return dt;
        return null;
    }

    [RelayCommand]
    private void PoaObrazac()
    {
        var (_, periodDo) = ProcitajPeriod();
        var vm = new OsPoaIzvestajViewModel(_sveKartice, OsPoaIzvestajViewModel.TipIzvestaja.PoaObrazac, periodDo);
        new OsPoaIzvestajWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private void EvidencijaPoa()
    {
        var vm = new OsPoaIzvestajViewModel(_sveKartice, OsPoaIzvestajViewModel.TipIzvestaja.EvidencijaPoa, null);
        new OsPoaIzvestajWindow(vm).ShowDialog();
    }

    private static decimal XDec(OsKartica k, string polje)
        => OsSaldoViewModel.DajDec(k, polje);

    private void SelektujKarticu(OsKartica kartica)
    {
        if (!Kartice.Contains(kartica))
        {
            FilterText = string.Empty;
            PrimeniFiIlter();
        }

        IzabranaKartica = kartica;
    }

    private (DateTime? od, DateTime? @do) ProcitajPeriod()
    {
        var path = DbfPutanja("ospodaci.dbf");
        if (path == null) return (null, null);

        try
        {
            var reader = new SimpleDbfReader(path);
            foreach (var r in reader.Zapisi())
                return (r.DajDate("EDAT0"), r.DajDate("EDAT1"));
        }
        catch
        {
            // Izvestaj moze i bez perioda.
        }

        return (null, null);
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
        catch
        {
            // Ako ne mozemo da procitamo kriterijume, nastavljamo sa praznim.
        }

        return new OsPopisFilterData();
    }

    private void SacuvajPopisKriterijume(OsPopisFilterData data)
    {
        var path = DbfPutanja("ospopis.dbf");
        if (path == null) return;

        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            var redovi = CitajSveRedove(path);

            if (redovi.Count == 0)
                redovi.Add(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase));

            var r = redovi[0];
            r["MESTO"] = data.Mesto?.Trim() ?? string.Empty;
            r["MTR"] = data.Mtr < 1 ? 0 : data.Mtr;
            r["KONTO"] = data.Konto?.Trim() ?? string.Empty;
            r["AG"] = data.Ag?.Trim() ?? string.Empty;
            r["AGPOD"] = data.AgPod?.Trim() ?? string.Empty;
            r["GRUPA"] = data.Grupa?.Trim() ?? string.Empty;

            DbfTableWriter.WriteTable(path, schema, redovi,
                (red, f) => red.TryGetValue(f, out var v) ? v : null);
        }
        catch
        {
            // Ne prekidamo rad ako ne uspe upis kriterijuma.
        }
    }

    [RelayCommand]
    private void ExportCsv()
    {
        if (_sveKartice.Count == 0) { Poruka = "Nema podataka za izvoz."; return; }
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title    = "Izvoz u CSV",
            Filter   = "CSV (*.csv)|*.csv|Svi fajlovi (*.*)|*.*",
            FileName = $"{_dbfIme.Replace(".dbf", "")}_{DateTime.Today:yyyyMMdd}.csv"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            using var sw = new StreamWriter(dlg.FileName, false, new System.Text.UTF8Encoding(true));
            sw.WriteLine("Sifra;Naziv;DatNab;BrNal;Konto;Vrsta;AG;AgPod;InvBroj;Mesto;Nab0;Isp0;Sad0;Kom;Cena;StopaOt;OsnovKor;Izvor;Preneto");
            foreach (var k in _sveKartice)
                sw.WriteLine(string.Join(";",
                    k.Osifra, k.Naz, k.DatNab?.ToString("dd.MM.yyyy") ?? "",
                    k.BrNal, k.Konto, k.Vrsta, k.Ag, k.AgPod, k.InvBroj, k.Mesto,
                    k.Nab0.ToString("N2"), k.Isp0.ToString("N2"), k.Sad0.ToString("N2"),
                    k.Kom.ToString("N2"), k.Cena.ToString("N2"), k.StopaOt.ToString("N3"),
                    k.OsnovKor, k.Izvor, k.Preneto));
            Poruka = $"CSV izvoz završen: {Path.GetFileName(dlg.FileName)} ({_sveKartice.Count} zapisa).";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }
        catch (Exception ex) { Poruka = $"Greška pri izvozu: {ex.Message}"; }
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (_sveKartice.Count == 0) { Poruka = "Nema podataka za izvoz."; return; }
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title    = "Izvoz u Excel",
            Filter   = "Excel (*.xlsx)|*.xlsx|Svi fajlovi (*.*)|*.*",
            FileName = $"{_dbfIme.Replace(".dbf", "")}_{DateTime.Today:yyyyMMdd}.xlsx"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            ExcelExportHelper.SnimiKartice(dlg.FileName, _sveKartice, "OS Evidencija");
            Poruka = $"Excel izvoz završen: {Path.GetFileName(dlg.FileName)} ({_sveKartice.Count} zapisa).";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }
        catch (Exception ex) { Poruka = $"Greška pri izvozu: {ex.Message}"; }
    }

    [RelayCommand]
    private void UvozExcel()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title  = "Uvoz iz Excel",
            Filter = "Excel (*.xlsx)|*.xlsx|Svi fajlovi (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true) return;

        ExcelImportHelper.ImportRezultat rez;
        try
        {
            rez = ExcelImportHelper.CitajKartice(dlg.FileName);
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čitanju fajla: {ex.Message}";
            return;
        }

        if (rez.Greske.Count > 0 && rez.Kartice.Count == 0)
        {
            Poruka = $"Uvoz nije moguć: {rez.Greske[0]}";
            return;
        }

        var postojeceSifre = _sveKartice
            .Select(k => (k.Osifra ?? "").Trim().ToUpperInvariant())
            .Where(s => s.Length > 0)
            .ToHashSet();

        var nove    = rez.Kartice.Where(k => !postojeceSifre.Contains((k.Osifra ?? "").Trim().ToUpperInvariant())).ToList();
        var duplikat = rez.Kartice.Count - nove.Count;

        var porukaPotvrda = $"Pronađeno {rez.Kartice.Count} kartica u fajlu.\n" +
                            $"Novih za uvoz: {nove.Count}\n" +
                            (duplikat > 0 ? $"Preskočeno (duplikat šifre): {duplikat}\n" : "") +
                            (rez.Greske.Count > 0 ? $"Upozorenja: {rez.Greske.Count}\n" : "") +
                            "\nUvesti?";

        if (System.Windows.MessageBox.Show(porukaPotvrda, "Uvoz iz Excel",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes) return;

        if (nove.Count == 0) { Poruka = "Nema novih kartica za uvoz (sve su duplikati)."; return; }

        var maxIdbr = _sveKartice.Count > 0 ? _sveKartice.Max(k => k.IDBr) : 0;
        foreach (var k in nove)
        {
            k.IDBr = ++maxIdbr;
            _sveKartice.Add(k);
            _osnovniPoredak.Add(k);
        }

        PrimeniFiIlter();
        Poruka = $"Uvezeno {nove.Count} kartica. Kliknite Sačuvaj.";
        _izmijenjeno = true;
        _log.Information("UvozExcel — uvezeno {Count} kartica iz {File}", nove.Count, dlg.FileName);
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
            (mmtr == 0 || DajExtraInt(k, "MTR") == mmtr) &&
            (string.IsNullOrWhiteSpace(mag) || string.Equals((k.Ag ?? string.Empty).Trim(), mag, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(magpod) || string.Equals((k.AgPod ?? string.Empty).Trim(), magpod, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(mkonto) || (k.Konto ?? string.Empty).TrimEnd().StartsWith(mkonto, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(mgrupa) || string.Equals(DajExtra(k, "GRUPA"), mgrupa, StringComparison.OrdinalIgnoreCase)));
    }

    private List<Dictionary<string, object?>> CitajSveRedove(string path)
    {
        var reader = new SimpleDbfReader(path);
        var svi = new List<Dictionary<string, object?>>();

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
            svi.Add(red);
        }

        return svi;
    }

    private static string DajExtra(OsKartica k, string polje)
    {
        if (!k.ExtraPolja.TryGetValue(polje, out var v) || v is null)
            return string.Empty;

        return Convert.ToString(v)?.Trim() ?? string.Empty;
    }

    private static int DajExtraInt(OsKartica k, string polje)
    {
        if (!k.ExtraPolja.TryGetValue(polje, out var v) || v is null)
            return 0;

        return v switch
        {
            int i => i,
            long l => (int)l,
            decimal d => (int)d,
            double db => (int)db,
            float f => (int)f,
            _ when int.TryParse(Convert.ToString(v), out var n) => n,
            _ => 0
        };
    }


    private string? DbfPutanja(string ime) => DbfHelper.NadjiDbf(_appState, ime);
}
