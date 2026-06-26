using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace OsnovnaSredstva.ViewModels;

// Legacy: xml_ost.scx (forma XMLOST) — "XML SEMA IZVESTAJ O OSTALOM REZULTATU".
// Fizička tabela je xmlost.dbf (legacy alias "xmlbu" — Init radi
// USE xmlost IN 0 EXCLU ALIAS xmlbu; CursorSource u FRX-u je drugačiji naziv ali se
// ne koristi jer je AutoOpenTables=.F.). Kolone grida: Deo1/Aop/Kol/Podatak/Deo2/Sve/
// Preneto/Idbr — tačno polja xmlost.dbf.
public partial class OsXmlOstViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext<OsXmlOstViewModel>();
    private readonly AppState _appState;
    private readonly int _mplica;

    private List<OsXmlOstStavka> _sveStavke = [];

    [ObservableProperty] private ObservableCollection<OsXmlOstStavka> _stavke = [];
    [ObservableProperty] private OsXmlOstStavka? _izabranaStavka;
    [ObservableProperty] private string _poruka = "";
    [ObservableProperty] private string _lblRec = "0/0";

    private bool _izmijenjeno;
    public bool ImaNeSnimljenih => _izmijenjeno;

    // Legacy: "IF ww=2 THEN thisform.BackColor=RGB(255,255,128)" + isti ww bira
    // ost01_2022.xml umesto ost01.xml u NapraviSablon. ww nema ekvivalent u WPF
    // AppState-u — jedini sačuvani trag njegovog značenja je baš taj 2022 prelom
    // šablona, pa se ovde izvodi iz aktivne poslovne godine.
    public bool NoviObrazac => _appState.AktivnaGodina >= 2022;

    public event Action? ZatvaranjeZahtevano;

    partial void OnIzabranaStavkaChanged(OsXmlOstStavka? value) => OsveziLblRec();
    partial void OnStavkeChanged(ObservableCollection<OsXmlOstStavka> value) => OsveziLblRec();

    public OsXmlOstViewModel(AppState appState, int mplica = 0)
    {
        _appState = appState;
        _mplica = mplica;
        Ucitaj();
    }

    private void OsveziLblRec()
    {
        var idx = IzabranaStavka == null ? 0 : Stavke.IndexOf(IzabranaStavka) + 1;
        LblRec = $"{idx}/{Stavke.Count}";
    }

    private void Ucitaj()
    {
        var path = DbfHelper.NadjiDbf(_appState, "xmlost.dbf");
        if (path == null) { Stavke = []; Poruka = "xmlost.dbf nije pronađen u folderu firme."; return; }

        try
        {
            var reader = new SimpleDbfReader(path);
            var lista = new List<OsXmlOstStavka>();
            foreach (var r in reader.Zapisi())
            {
                lista.Add(new OsXmlOstStavka
                {
                    Deo1    = r.DajString("DEO1"),
                    Aop     = r.DajString("AOP"),
                    Kol     = r.DajString("KOL"),
                    Podatak = r.DajString("PODATAK"),
                    Deo2    = r.DajString("DEO2"),
                    Sve     = r.DajString("SVE"),
                    Preneto = r.DajString("PRENETO"),
                    Idbr    = r.DajInt("IDBR"),
                });
            }
            _sveStavke = lista;
            Stavke = new ObservableCollection<OsXmlOstStavka>(_sveStavke);
            IzabranaStavka = Stavke.FirstOrDefault();
            Poruka = $"Učitano {_sveStavke.Count} zapisa iz xmlost.dbf.";
            PretplatiSeNaIzmjene();
            _izmijenjeno = false;
        }
        catch (Exception ex)
        {
            _sveStavke = []; Stavke = [];
            Poruka = $"Greška: {ex.Message}";
            _log.Error(ex, "Greška pri čitanju xmlost.dbf");
        }
    }

    private void PretplatiSeNaIzmjene()
    {
        foreach (var s in _sveStavke)
            s.PropertyChanged += (_, _) => _izmijenjeno = true;
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        var path = DbfHelper.NadjiDbf(_appState, "xmlost.dbf");
        if (path == null) { Poruka = "xmlost.dbf nije pronađen."; return; }
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, _sveStavke,
                (s, f) => f.ToUpperInvariant() switch
                {
                    "DEO1"    => (object?)s.Deo1,
                    "AOP"     => s.Aop,
                    "KOL"     => s.Kol,
                    "PODATAK" => s.Podatak,
                    "DEO2"    => s.Deo2,
                    "SVE"     => s.Sve,
                    "PRENETO" => s.Preneto,
                    "IDBR"    => s.Idbr,
                    _         => null
                });
            Poruka = $"Sačuvano ({_sveStavke.Count} zapisa).";
            _izmijenjeno = false;
            _log.Information("xmlost.dbf: sačuvano {Count} zapisa", _sveStavke.Count);
        }
        catch (Exception ex) { Poruka = $"Greška pri snimanju: {ex.Message}"; _log.Error(ex, "Greška pri snimanju xmlost.dbf"); }
    }

    // ═══ CMDPRVI / CMDGORE / CMDDOLE / CMDZADNJI — navigacija (GO TOP/SKIP-1/SKIP/GO BOTTOM) ═══
    [RelayCommand] private void Prvi()   { if (Stavke.Count > 0) IzabranaStavka = Stavke[0]; }
    [RelayCommand] private void Zadnji() { if (Stavke.Count > 0) IzabranaStavka = Stavke[^1]; }
    [RelayCommand]
    private void Dole()
    {
        if (IzabranaStavka == null || Stavke.Count == 0) return;
        var idx = Stavke.IndexOf(IzabranaStavka);
        if (idx < Stavke.Count - 1) IzabranaStavka = Stavke[idx + 1];
    }
    [RelayCommand]
    private void Gore()
    {
        if (IzabranaStavka == null || Stavke.Count == 0) return;
        var idx = Stavke.IndexOf(IzabranaStavka);
        if (idx > 0) IzabranaStavka = Stavke[idx - 1];
    }

    // CMDDODAJ: "APPEND BLANK"
    [RelayCommand]
    private void Dodaj()
    {
        var nova = new OsXmlOstStavka();
        _sveStavke.Add(nova);
        Stavke = new ObservableCollection<OsXmlOstStavka>(_sveStavke);
        IzabranaStavka = nova;
        nova.PropertyChanged += (_, _) => _izmijenjeno = true;
        _izmijenjeno = true;
    }

    // Legacy Command2 "\<BRIŠI": DELETE NEXT 1 + PACK + THISFORM.Release — briše
    // TRENUTNI red i odmah zatvara formu (doslovno tako u izvoru, neuobičajeno ali
    // namerno). Ovde je dodata potvrda jer je brisanje nepovratno — ista predostrožnost
    // kao kod ostalih ZAP/DELETE komandi u ovom portu (npr. OsObrazacOaViewModel.BrisanjeSve).
    [RelayCommand]
    private void Obrisi()
    {
        if (IzabranaStavka == null) return;
        if (MessageBox.Show("Obrisati izabrani red?", "BRIŠI", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        _sveStavke.Remove(IzabranaStavka);
        Stavke = new ObservableCollection<OsXmlOstStavka>(_sveStavke);
        Sacuvaj();
        ZatvaranjeZahtevano?.Invoke();
    }

    // Legacy Command3 "\<BRIŠI SVE": SELECT XMLbu; ZAP; THISFORM.Release — briše SVE
    // zapise i zatvara formu. Potvrda dodata iz istog razloga kao i kod Obrisi gore.
    [RelayCommand]
    private void ObrisiSve()
    {
        if (MessageBox.Show($"Obrisati SVE zapise ({_sveStavke.Count})?", "BRIŠI SVE", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        _sveStavke.Clear();
        Stavke = [];
        Sacuvaj();
        ZatvaranjeZahtevano?.Invoke();
    }

    // ═══ Command1 "1.NAPRAVI ŠABLON" ═══
    // Legacy: ZAP xmlbu; USE xmlsve IN 0 EXCLUSIVE; ZAP; APPEND FROM <ost01[.._2022]xml> TYPE SDF
    // (puni xmlsve.SVE — prvo/najšire C polje — celom linijom teksta); USE; APPEND FROM xmlsve
    // u xmlbu (po imenu polja — SVE/PRENETO/IDBR se prepisuju, DEO1/AOP/KOL/PODATAK/DEO2 ostaju
    // prazni). Zatim 5 sekvencijalnih GO TOP/SCAN prolaza koji iz SVE popunjavaju DEO1/DEO2,
    // pa iz DEO1 izvlače AOP/KOL.
    [RelayCommand]
    private void NapraviSablon()
    {
        var dplSufiks = _mplica == 0 ? "" : "dpl";
        var templateFile = NoviObrazac ? $"ost01{dplSufiks}_2022.xml" : $"ost01{dplSufiks}.xml";

        var templatePath = NadjiSablonDatoteku(templateFile);
        if (templatePath == null)
        {
            Poruka = $"Šablon {templateFile} nije pronađen.";
            return;
        }

        List<string> linije;
        try { linije = File.ReadAllLines(templatePath).ToList(); }
        catch (Exception ex) { Poruka = $"Greška pri čitanju šablona: {ex.Message}"; return; }

        // APPEND FROM ... TYPE SDF u xmlsve: SVE je C(220) — linije duže od toga se seku.
        var nove = linije.Select(l => new OsXmlOstStavka { Sve = l.Length > 220 ? l[..220] : l }).ToList();

        // Prolaz 1: deo1/deo2 iz </a:NumerickoPolje> markera
        foreach (var s in nove)
        {
            var mat = FoxAt("</a:NumerickoPolje>", s.Sve);
            if (mat > 0)
            {
                s.Deo1 = FoxSubstr(s.Sve, 1, mat - 1);
                s.Deo2 = "</a:NumerickoPolje>";
            }
        }
        // Prolaz 2: deo1/deo2 iz </TekstualnoPolje> markera
        foreach (var s in nove)
        {
            var mat = FoxAt("</TekstualnoPolje>", s.Sve);
            if (mat > 0)
            {
                s.Deo1 = FoxSubstr(s.Sve, 1, mat - 1);
                s.Deo2 = "</TekstualnoPolje>";
            }
        }
        // Prolaz 3: linije koje ne odgovaraju ni jednom markeru (zaglavlje/zatvaranje taga) —
        // cela linija ide u deo1, isto kao legacy "IF deo1=SPACE(160) THEN deo1=sve".
        foreach (var s in nove)
        {
            if (s.Deo1.Length == 0)
                s.Deo1 = s.Sve;
        }
        // Prolaz 4a: AOP/KOL iz <a:Vrednosti i:nil="true"/> (NumerickoPolje varijanta).
        // Legacy dužina SUBSTR-a (MAT2+4+4+1) je veća od širine AOP/KOL polja — FoxPro
        // REPLACE u fiksno polje skraćuje vrednost na širinu polja (C(4)/C(1)), pa je
        // efekat identičan uzimanju tačno 4, odn. 1 znaka počev od MAT2+4 — to je ovde
        // direktno transkribovano kroz širinu polja.
        foreach (var s in nove)
        {
            var mat = FoxAt("<a:Vrednosti i:nil=\"true\"/>", s.Deo1);
            if (mat > 0)
            {
                var mat2 = FoxAt("aop-", s.Deo1);
                if (mat2 > 0)
                {
                    s.Aop = FoxSubstr(s.Deo1, mat2 + 4, 4);
                    s.Kol = FoxSubstr(s.Deo1, mat2 + 4 + 4 + 1, 1);
                }
            }
        }
        // Prolaz 4b: AOP/KOL iz <TekstualnoPolje><Naziv>aop- (TekstualnoPolje varijanta)
        foreach (var s in nove)
        {
            var mat = FoxAt("<TekstualnoPolje><Naziv>aop-", s.Deo1);
            if (mat > 0)
            {
                var mat2 = FoxAt("aop-", s.Deo1);
                if (mat2 > 0)
                {
                    s.Aop = FoxSubstr(s.Deo1, mat2 + 4, 4);
                    s.Kol = FoxSubstr(s.Deo1, mat2 + 4 + 4 + 1, 1);
                }
            }
        }

        _sveStavke = nove;
        Stavke = new ObservableCollection<OsXmlOstStavka>(_sveStavke);
        IzabranaStavka = Stavke.FirstOrDefault();
        PretplatiSeNaIzmjene();
        _izmijenjeno = true;
        Poruka = $"Šablon učitan iz {Path.GetFileName(templatePath)} — {_sveStavke.Count} redova. Kliknite SAČUVAJ.";
    }

    // ═══ Command4 "2.NAPUNI PODATKE" ═══
    // Legacy prvo poziva Set Procedure To xmlbilansi[DPL] / Do xmlostpuni.
    // Izvor procedure nije sačuvan, ali je njen ulaz ostao u bazi: znostrez.dbf sadrži
    // AOP i dve vrednosti obrasca — NETO (XML kolona 5) i PRETH (XML kolona 6).
    // Posle prenosa vrednosti sledi doslovno prepisan legacy deo koji otvara nil-tagove.
    [RelayCommand]
    private void NapuniPodatke()
    {
        if (_sveStavke.Count == 0)
        {
            Poruka = "Nema XML redova — prvo pokrenite NAPRAVI ŠABLON.";
            return;
        }

        var znostrezPath = DbfHelper.NadjiDbf(_appState, "znostrez.dbf");
        if (znostrezPath == null)
        {
            Poruka = "znostrez.dbf nije pronađen. Prvo formirajte Izveštaj o ostalom rezultatu u glavnoj knjizi.";
            return;
        }

        Dictionary<string, (decimal Neto, decimal Preth)> vrednosti;
        try
        {
            vrednosti = UcitajOstVrednosti(znostrezPath);
        }
        catch (Exception ex)
        {
            Poruka = $"Greška pri čitanju znostrez.dbf: {ex.Message}";
            _log.Error(ex, "Greška pri automatskom punjenju XML OST iz znostrez.dbf");
            return;
        }

        if (vrednosti.Count == 0)
        {
            Poruka = "znostrez.dbf nema podataka. Prvo formirajte Izveštaj o ostalom rezultatu u glavnoj knjizi.";
            return;
        }

        var popunjeno = 0;
        foreach (var s in _sveStavke)
        {
            var aop = Alltrim(s.Aop);
            if (!vrednosti.TryGetValue(aop, out var vrednost))
                continue;

            s.Podatak = Alltrim(s.Kol) switch
            {
                "5" => FormatXmlBroj(vrednost.Neto),
                "6" => FormatXmlBroj(vrednost.Preth),
                _ => s.Podatak
            };

            if (Alltrim(s.Podatak).Length > 0)
                popunjeno++;
        }

        foreach (var s in _sveStavke)
        {
            if (Alltrim(s.Podatak).Length == 0) continue;

            var mat = FoxAt("<a:Vrednosti i:nil=\"true\"/>", s.Deo1);
            if (mat > 0)
            {
                s.Deo1 = FoxSubstr(s.Deo1, 1, mat - 1) + "<a:Vrednosti>";
                s.Deo2 = "</a:Vrednosti>" + Alltrim(s.Deo2);
            }
        }
        foreach (var s in _sveStavke)
        {
            if (Alltrim(s.Podatak).Length == 0) continue;

            var mat = FoxAt("</Naziv><Vrednosti/>", s.Deo1);
            if (mat > 0)
            {
                s.Deo1 = FoxSubstr(s.Deo1, 1, mat - 1) + "</Naziv><Vrednosti>";
                s.Deo2 = "</Vrednosti>" + Alltrim(s.Deo2);
            }
        }
        Stavke = new ObservableCollection<OsXmlOstStavka>(_sveStavke);
        _izmijenjeno = true;
        Poruka = $"Automatski popunjeno {popunjeno} XML polja iz znostrez.dbf. Kliknite SAČUVAJ.";
    }

    private static Dictionary<string, (decimal Neto, decimal Preth)> UcitajOstVrednosti(string path)
    {
        var rezultat = new Dictionary<string, (decimal Neto, decimal Preth)>(
            StringComparer.OrdinalIgnoreCase);
        var reader = new SimpleDbfReader(path);

        foreach (var r in reader.Zapisi())
        {
            var aop = r.DajString("AOP").Trim();
            if (aop.Length == 0)
                continue;

            rezultat[aop] = (r.DajDecimal("NETO"), r.DajDecimal("PRETH"));
        }

        return rezultat;
    }

    private static string FormatXmlBroj(decimal value) =>
        value.ToString("0", System.Globalization.CultureInfo.InvariantCulture);

    // ═══ Command11 "3.NAPRAVI XML" ═══
    // Legacy: FCREATE('OBRAZAC_OST.XML') u radnom folderu, pa za svaki red
    // FPUTS(ALLTRIM(deo1)+ALLTRIM(podatak)+ALLTRIM(deo2)).
    [RelayCommand]
    private void NapraviXml()
    {
        if (_sveStavke.Count == 0) { Poruka = "Nema podataka — prvo pokrenite NAPRAVI ŠABLON."; return; }

        var folder = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folder)) { Poruka = "Nema aktivne firme."; return; }

        var ciljniFajl = Path.Combine(folder, "OBRAZAC_OST.XML");
        try
        {
            using var sw = new StreamWriter(ciljniFajl, false, new System.Text.UTF8Encoding(false));
            foreach (var s in _sveStavke)
                sw.WriteLine(Alltrim(s.Deo1) + Alltrim(s.Podatak) + Alltrim(s.Deo2));

            Poruka = $"Kreirali ste datoteku OBRAZAC_OST.XML u folderu {folder}";
            MessageBox.Show(Poruka, "XML SEMA IZVESTAJ O OSTALOM REZULTATU",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { Poruka = $"Greška pri kreiranju XML fajla: {ex.Message}"; }
    }

    // Command10 "Xml": GETFILE('xml') + DO RUN_KOCKA WITH mget. RUN_KOCKA nema izvor u
    // repozitorijumu (eksterna pomoćna procedura) — najbliži dostupan ekvivalent je
    // otvaranje izabranog fajla podrazumevanim sistemskim programom.
    [RelayCommand]
    private void OtvoriXmlEksterno()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Izaberite XML fajl",
            Filter = "XML fajlovi (*.xml)|*.xml|Svi fajlovi (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
        }
        catch (Exception ex) { Poruka = $"Greška pri otvaranju fajla: {ex.Message}"; }
    }

    // CMDIZLAZ
    [RelayCommand] private void Izlaz() => ZatvaranjeZahtevano?.Invoke();

    private string? NadjiSablonDatoteku(string ime)
    {
        var kandidati = new List<string>();
        var folderFirme = _appState.AktivnaFirma?.FolderPath;
        if (!string.IsNullOrWhiteSpace(folderFirme))
        {
            var root = FinWorkspaceResolver.NormalizeRootPath(folderFirme);
            kandidati.Add(root);
            kandidati.Add(Path.Combine(root, ".."));
            kandidati.Add(folderFirme);
        }
        kandidati.Add(AppContext.BaseDirectory);
        kandidati.Add(Directory.GetCurrentDirectory());

        foreach (var dir in kandidati)
        {
            try
            {
                var p = Path.Combine(dir, ime);
                if (File.Exists(p)) return p;
            }
            catch { }
        }
        return null;
    }

    private static string Alltrim(string s) => (s ?? "").Trim();

    private static int FoxAt(string needle, string haystack)
    {
        if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle)) return 0;
        var i = haystack.IndexOf(needle, StringComparison.Ordinal);
        return i < 0 ? 0 : i + 1;
    }

    private static string FoxSubstr(string s, int start1Based, int length)
    {
        if (string.IsNullOrEmpty(s) || length <= 0) return "";
        var startIdx = start1Based - 1;
        if (startIdx < 0) startIdx = 0;
        if (startIdx >= s.Length) return "";
        var len = Math.Min(length, s.Length - startIdx);
        return s.Substring(startIdx, len);
    }
}
