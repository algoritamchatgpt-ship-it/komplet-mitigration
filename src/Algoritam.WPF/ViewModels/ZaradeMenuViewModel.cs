using Algoritam.Application;
using Algoritam.Application.Services;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// ViewModel za glavni meni zarada — ekvivalent FoxPro forme LD000.
/// Caption originala: "GLAVNI MENI ZARADE I DRUGA LICNA PRIMANJA"
/// </summary>
public partial class ZaradeMenuViewModel : ObservableObject
{
    private readonly AppState _appState;
    private readonly IPutanjaService _putanjaService;

    [ObservableProperty] private string _trenutniPeriod = "";

    public ObservableCollection<BrzoUlazStavka> BrzoUlazLista { get; } = [];

    public bool ImaBrzoUlaz => BrzoUlazLista.Count > 0;

    [RelayCommand]
    private void OtvoriBrzoUlaz(BrzoUlazStavka stavka) => Akcija(stavka.Komanda);

    private void ZabeleziBrzoUlaz(string komanda)
    {
        var naziv = NazivZaKomandu(komanda);
        BrzoUlazLista.Remove(BrzoUlazLista.FirstOrDefault(s => s.Komanda == komanda)!);
        BrzoUlazLista.Insert(0, new BrzoUlazStavka(naziv, komanda));
        while (BrzoUlazLista.Count > 6) BrzoUlazLista.RemoveAt(BrzoUlazLista.Count - 1);
        OnPropertyChanged(nameof(ImaBrzoUlaz));
    }

    private static readonly Dictionary<string, string> _komandaNazivi = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PLATNI_SPISAK"]             = "Platni spisak",
        ["FORMIRANJE_TABELA"]         = "Formiranje tabela",
        ["PARAMETRI1"]                = "Parametri 1",
        ["PARAMETRI2"]                = "Parametri 2",
        ["REKAPITULACIJA"]            = "Rekapitulacija",
        ["SPISKOVI"]                  = "Spiskovi isplate",
        ["RADNICI"]                   = "Radnici",
        ["KREDITI"]                   = "Krediti",
        ["OBRAZAC_PPP"]               = "PPP-PD obrazac",
        ["PODACI_M4"]                 = "Podaci za M4",
        ["VIRMANI"]                   = "Virmani",
        ["OPJ"]                       = "OPJ obrazac",
        ["OPJ1_OPJ8"]                 = "OPJ-1 do OPJ-8",
        ["OD1"]                       = "OD-1 obrazac",
        ["OD_RADNICI"]                = "OD radnici",
        ["OD_VLASNIK"]                = "OD vlasnik",
        ["ZDRAVSTVENE_KNJIZICE"]      = "Zdravstvene knjižice",
        ["PREVOZ"]                    = "Prevoz",
        ["PREVOZ_ARHIVA"]             = "Arhiva prevoza",
        ["PUTNI_NALOZI"]              = "Putni nalozi",
        ["PUTARINE"]                  = "Putarine",
        ["BOLOVANJE"]                 = "Bolovanje",
        ["PORODILJE"]                 = "Porodilje",
        ["INVALIDI2"]                 = "Invalidi II kategorije",
        ["PRIPRAVNICI"]               = "Pripravnici",
        ["KNJIZENJE_ZARADA"]          = "Knjiženje zarada",
        ["ARHIVA_ZARADA"]             = "Arhiva zarada",
        ["TABELA_SVIH_ZARADA"]        = "Tabela svih zarada",
        ["PODACI_ZARADE"]             = "Podaci zarade",
        ["PODACI_ZARADE_POJEDINACNO"] = "Podaci zarade pojeunacno",
        ["EVIDENCIJA_RADNIKA"]        = "Evidencija radnika",
        ["RADNO_VREME"]               = "Radno vreme",
        ["GRADOVI"]                   = "Gradovi i mesta",
        ["SAMODOPRINOS"]              = "Samodoprinos",
        ["BAZNA_KONTA"]               = "Bazna konta",
        ["TABELA_KONTA"]              = "Tabela konta",
        ["PARTNERI"]                  = "Partneri",
        ["NALOG_ZA_PLACANJE"]         = "Nalog za plaćanje",
        ["ZAHTEV_TRANSFER"]           = "Zahtev za transfer",
        ["OBRAZAC_ZSP"]               = "Obrazac ZSP",
        ["OBRAZAC_ZSD"]               = "Obrazac ZSD",
        ["OBRAZAC_IOSI"]              = "Obrazac IOSI",
        ["OBRAZAC_OPNR"]              = "Obrazac OPNR",
        ["OBRAZAC_INZS"]              = "Obrazac INZS",
        ["POTVRDA_PENZ"]              = "Potvrda o penziji",
        ["POTVRDA_ISPLATA"]           = "Potvrda o isplati",
        ["POTVRDA_ZARADA"]            = "Potvrda o zaradi",
        ["IZJAVA_DOP"]                = "Izjava o doprinosima",
        ["IZJAVA_RADNOVREME"]         = "Izjava radno vreme",
        ["RADNICI_OLAKSICE"]          = "Radnici — olakšice",
        ["ZAHTEV_POVRACAJ"]           = "Zahtev za povraćaj",
        ["REGISTAR"]                  = "Registar",
        ["LDNAKNADE"]                 = "Naknade",
        ["EMAIL_LISTICI"]             = "E-mail listići",
        ["IZVOZ_TABELE"]              = "Izvoz tabele",
    };

    private static string NazivZaKomandu(string komanda)
    {
        if (_komandaNazivi.TryGetValue(komanda, out var naziv)) return naziv;
        var reci = komanda.Replace("_", " ").ToLowerInvariant();
        if (reci.Length == 0) return komanda;
        return char.ToUpper(reci[0]) + reci[1..];
    }

    public string FirmaNaziv => _appState.AktivnaFirma?.Naziv ?? "— Nije izabrana firma —";
    public string FirmaMesto => _appState.AktivnaFirma?.Mesto ?? "";
    public string FirmaPib   => _appState.AktivnaFirma?.Pib is { Length: > 0 } p ? $"PIB: {p}" : "";

    public void OsveziInfoFirme()
    {
        OnPropertyChanged(nameof(FirmaNaziv));
        OnPropertyChanged(nameof(FirmaMesto));
        OnPropertyChanged(nameof(FirmaPib));
    }

    public ZaradeMenuViewModel(AppState appState, IPutanjaService putanjaService)
    {
        _appState = appState;
        _putanjaService = putanjaService;
        UcitajPeriod();
    }

    private void UcitajPeriod()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folderPath))
            return;

        var dbfPath = NadjiDbf(folderPath, "ldparam.dbf");
        if (dbfPath is null) return;

        try
        {
            var zapisi = DbfReader.CitajSveZapise(dbfPath);
            var prvi = zapisi.FirstOrDefault();
            if (prvi is null) return;

            int mesec = prvi.TryGetValue("MESEC", out var m) && m is decimal dm ? (int)dm : 0;
            string nazmes = prvi.TryGetValue("NAZMES", out var n) && n is string ns ? ns.Trim() : "";
            string godina = prvi.TryGetValue("GODINA", out var g) && g is string gs ? gs.Trim() : "";

            if (mesec > 0 && !string.IsNullOrWhiteSpace(nazmes))
                TrenutniPeriod = string.IsNullOrWhiteSpace(godina) ? nazmes : $"{nazmes}  {godina}";
        }
        catch
        {
            // Period je opcionalan info — ignorisati greške čitanja
        }
    }

    private static string? NadjiDbf(string folderPath, string fileName)
    {
        var pretraga = new[] { folderPath,
            Path.Combine(folderPath, "data00"),
            Path.Combine(folderPath, "01") };

        foreach (var dir in pretraga)
        {
            if (!Directory.Exists(dir)) continue;
            var tacna = Path.Combine(dir, fileName);
            if (File.Exists(tacna)) return tacna;
            var ci = Directory.GetFiles(dir, "*.dbf", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase));
            if (ci is not null) return ci;
        }
        return null;
    }

    public event Action? IzlazTražen;

    [RelayCommand]
    private void Izlaz() => IzlazTražen?.Invoke();

    [RelayCommand]
    private void Kontakt()
    {
        var view = new Algoritam.WPF.Views.Zarade.KontaktView();
        view.ShowDialog();
    }

    [RelayCommand]
    private void Uputstva()
    {
        var dostupnaUputstva = PronadjiDostupnaUputstva();
        if (dostupnaUputstva.Count == 0)
        {
            System.Windows.MessageBox.Show(
                "Nijedno uputstvo nije pronadjeno.\n\n" +
                "Proverite da li postoje fajlovi u folderu Uputstva.",
                "Uputstva",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        var izbor = new Algoritam.WPF.Views.Zarade.UputstvaIzborWindow(dostupnaUputstva);
        var potvrda = izbor.ShowDialog();
        if (potvrda != true || izbor.IzabranoUputstvo is null)
            return;

        OtvoriPdfUputstvo(izbor.IzabranoUputstvo.Putanja);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ArhivirajCommand))]
    private bool _arhiviraUToku = false;

    [RelayCommand(CanExecute = nameof(MozeArhivirati))]
    private async Task Arhiviraj()
    {
        var arhivaPutanja = _putanjaService.DajArhivaPutanju();
        var firmaFolder   = _appState.AktivnaFirma?.FolderPath;
        var firmaNaziv    = _appState.AktivnaFirma?.Naziv ?? "Firma";

        if (string.IsNullOrWhiteSpace(arhivaPutanja))
        {
            System.Windows.MessageBox.Show(
                "Putanja za arhiviranje nije postavljena.\n\nPostavite je na početnom ekranu (Podešavanja → Arhiva).",
                "Arhiviranje",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(firmaFolder) || !Directory.Exists(firmaFolder))
        {
            System.Windows.MessageBox.Show(
                "Folder aktivne firme nije dostupan.",
                "Arhiviranje",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        var potvrda = System.Windows.MessageBox.Show(
            $"Arhivirati podatke firme:\n{firmaFolder}\n\nu:\n{arhivaPutanja}\n\nNastaviti?",
            "Arhiviranje podataka firme",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (potvrda != System.Windows.MessageBoxResult.Yes) return;

        ArhiviraUToku = true;
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var invalid   = new HashSet<char>(Path.GetInvalidFileNameChars());
            var firmaIme  = new string(firmaNaziv.Where(c => !invalid.Contains(c)).ToArray()).Trim();
            if (string.IsNullOrWhiteSpace(firmaIme)) firmaIme = "Firma";
            var odrediste = Path.Combine(arhivaPutanja, $"{firmaIme}_{timestamp}");

            await Task.Run(() => KopirajFolderUArhivu(firmaFolder, odrediste));

            System.Windows.MessageBox.Show(
                $"Arhiviranje uspešno završeno.\n\nPodaci su kopirani u:\n{odrediste}",
                "Arhiviranje — uspešno",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Greška pri arhiviranju:\n{ex.Message}",
                "Arhiviranje — greška",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            ArhiviraUToku = false;
        }
    }

    private bool MozeArhivirati() => !ArhiviraUToku;

    private static void KopirajFolderUArhivu(string izvor, string odrediste)
    {
        Directory.CreateDirectory(odrediste);
        foreach (var fajl in Directory.GetFiles(izvor))
            File.Copy(fajl, Path.Combine(odrediste, Path.GetFileName(fajl)), overwrite: true);
        foreach (var podDir in Directory.GetDirectories(izvor))
            KopirajFolderUArhivu(podDir, Path.Combine(odrediste, Path.GetFileName(podDir)));
    }

    /// <summary>
    /// Centralni komandni handler za sve akcione dugmiće (ekvivalent CommandButton Click u LD000).
    /// Parameter odgovara MISPLATA/DO FORM pozivima u originalnom FoxPro kodu.
    /// </summary>
    [RelayCommand]
    private void Akcija(string parametar)
    {
        if (string.IsNullOrWhiteSpace(parametar))
            return;

        var komanda = parametar.Trim().ToUpperInvariant();

        if (komanda is "UNOS_RADNIKA" or "LDUNOSRAD")
        {
            System.Windows.MessageBox.Show(
                "Unos radnika u platni spisak se obavlja unutar Platnog spiska.\n\nOtvorite PLATNI SPISAK i koristite dugme PRENOSI.",
                "Unos radnika",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        if (komanda is "UNOS_CASOVA" or "LDUNOSCAS")
        {
            System.Windows.MessageBox.Show(
                "Unos časova se obavlja unutar Platnog spiska za svakog radnika.\n\nOtvorite PLATNI SPISAK i koristite taster F9 ili Kartica F7.",
                "Unos časova",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        ZabeleziBrzoUlaz(komanda);

        switch (komanda)
        {
            // ── Obračun ─────────────────────────────────────────────────────────
            case "IZVOZ_TABELE":
                OtvoriIzvozTabele();
                break;
            case "FORMIRANJE_TABELA":
            case "LDPREPAKUJ":
                OtvoriFormiranjeTabela();
                break;
            case "PLATNI_SPISAK":     // → DO FORM LDPLA (obrada i obračun zarada)
            case "LDPLA":
                OtvoriPlatniSpisak();
                break;
            case "PARAMETRI1":        // → DO FORM LDPAR
            case "LDPAR":
                OtvoriParametre(0);
                break;
            case "PARAMETRI2":        // → DO FORM LDPAR2
            case "LDPAR2":
                OtvoriParametre(1);
                break;
            case "REKAPITULACIJA":    // → DO FORM LDREK
            case "LDREK":
                OtvoriRekapitulaciju();
                break;

            // ── Šifarnici ───────────────────────────────────────────────────────
            case "RADNICI":           // → DO FORM LDRAD
            case "LDRAD":
                OtvoriRadnici();
                break;
            case "RADNO_VREME":       // → DO FORM LDRADV
            case "LDRADVRE":
                OtvoriRadnoVreme();
                break;
            case "GRADOVI":           // → DO FORM LDMESTA
            case "LDMESTA":
                OtvoriGradovi();
                break;
            case "KREDITI":           // → DO FORM LDKRED
            case "LDKRED":
                OtvoriKrediti();
                break;
            case "SAMODOPRINOS":      // → DO FORM LDSAMOD
            case "LDSAMOD":
                OtvoriSamodoprinos();
                break;
            case "ZDRAVSTVENE_KNJIZICE": // → pregled LBO/ZK/osiguranje polja iz ldrad.dbf
            case "LDOBRKNJIZICE":
                OtvoriZdravstveneKnjizice();
                break;
            case "BAZNA_KONTA":       // → DO FORM LDAKONT
            case "LDAKONT0":
            case "LDOBRRACUNI":
                OtvoriBaznaKonta();
                break;
            case "TABELA_KONTA":      // → DO FORM LDKON
            case "LDKON":
            case "LDKON00":
            case "LDKON000":
                OtvoriTabelaKonta();
                break;
            case "PARTNERI":          // → DO FORM LDPARTNER (an0.dbf)
            case "LDPARTNER":
            case "AN0":
                OtvoriFormu<PartneriViewModel, Views.Zarade.PartneriView>();
                break;
            case "EVIDENCIJA_RADNIKA": // → pregled ldrad.dbf sa PPP-PD šiframa
            case "LDRADVR":
            case "LDRADVRER":
                OtvoriEvidencijaRadnika();
                break;

            // ── Spiskovi i pregledi ─────────────────────────────────────────────
            case "SPISKOVI":          // → DO FORM LDSPIS
            case "LDSPIS":
                OtvoriSpiskovi();
                break;
            case "PODACI_ZARADE":     // → DO FORM LDPODZ (pregled aktivnog meseca)
            case "LDPOD":
                OtvoriPodaciZarade();
                break;
            case "PODACI_ZARADE_POJEDINACNO": // → DO FORM LDPOD00
            case "LDPOD00":
                OtvoriPodaciZaradePojedinacno();
                break;
            case "ARHIVA_ZARADA":     // → DO FORM LDARHIVA
            case "LDARHIVA":
                OtvoriArhivaZarada();
                break;
            case "TABELA_SVIH_ZARADA": // → DO FORM LDTABSV
            case "LDSVEZARADE":
                OtvoriSveZarade();
                break;
            case "KNJIZENJE_ZARADA":  // → DO FORM LDKNJ
                OtvoriKnjizenjeZarada();
                break;

            // ── Obrasci i virmani ───────────────────────────────────────────────
            case "OPJ":               // → DO FORM LDOPJN
            case "LDOPJN":
                OtvoriFormu<OpjViewModel, Views.Zarade.OpjView>();
                break;
            case "OPJ1_OPJ8":         // → DO FORM LDOPJ1
            case "LDOBROPJ18":
                OtvoriFormu<Opj1ViewModel, Views.Zarade.Opj1View>();
                break;
            case "OD1":               // → DO FORM LDOD1N
            case "LDOD1N":
                OtvoriFormu<Od1ViewModel, Views.Zarade.Od1View>();
                break;
            case "OD_RADNICI":        // → DO FORM LDODN (radnici)
            case "LDODN2":
                OtvoriOdRadnici();
                break;
            case "OD_VLASNIK":        // → DO FORM LDODV (vlasnik)
                OtvoriFormu<OdVlasnikViewModel, Views.Zarade.OdVlasnikView>();
                break;
            case "VIRMANI":           // → DO FORM LDVIR
            case "LDVIRM":
            case "LDNOVIR":
            case "LDNOVIRZ":
                OtvoriVirmane();
                break;
            case "NALOG_ZA_PLACANJE": // → nalpep.dbf
            case "NALPEP":
                OtvoriNalogZaPlacanje();
                break;
            case "ZAHTEV_TRANSFER":   // → DO FORM LDZAHTEV
            case "LDZAHTEV":
                OtvoriFormu<ZahtevTransferViewModel, Views.Zarade.ZahtevTransferView>();
                break;
            case "PODACI_M4":         // → DO FORM LDM4
            case "LDM4":
                OtvoriM4();
                break;
            case "OBRAZAC_PPP":       // → DO FORM LDPPP
            case "LDPPP":
                OtvoriPppMeni();
                break;
            case "OBRAZAC_ZSP":       // → DO FORM LDZSP
            case "LDZSP":
                OtvoriFormu<ZspViewModel, Views.Zarade.ZspView>();
                break;
            case "OBRAZAC_ZSD":       // → DO FORM LDZSD
            case "LDZSD":
                OtvoriFormu<ZsdViewModel, Views.Zarade.ZsdView>();
                break;
            case "OBRAZAC_IOSI":      // → DO FORM LDIOSI
            case "LDOBRIOSI":
                OtvoriFormu<IosiViewModel, Views.Zarade.IosiView>();
                break;
            case "OBRAZAC_OPNR":      // → DO FORM LDOPNR
            case "LDOPNR":
                OtvoriFormu<OpnrViewModel, Views.Zarade.OpnrView>();
                break;
            case "OBRAZAC_INZS":      // → DO FORM LDINZS
            case "LDINZS":
                OtvoriFormu<InzsViewModel, Views.Zarade.InzsView>();
                break;

            // ── Prevoz i putni nalozi ───────────────────────────────────────────
            case "PREVOZ":            // → DO FORM LDPREVOZ
            case "LDPREV":
                OtvoriPrevoz();
                break;
            case "PREVOZ_ARHIVA":     // → Pregled arhiviranih obračuna prevoza
            case "LDPREVARHIVA":
                OtvoriArhivaPrevoza();
                break;
            case "PUTNI_NALOZI":      // → DO FORM LDPUT
            case "LDPUTNAL":
                OtvoriPutneNaloge();
                break;
            case "PUTARINE":          // → DO FORM PUTAR
            case "PUTAR":
                OtvoriPutarine();
                break;

            // ── Potvrde, izjave i posebni slučajevi ────────────────────────────
            case "PORODILJE":         // → DO FORM LDPOROD
            case "LDPORODILJE00":
            case "LDPROSZ":
            case "LDPPODSO":
            case "LDPPODPO":
                OtvoriPorodilje();
                break;
            case "BOLOVANJE":         // → DO FORM LDBOL
            case "LDBOLOVANJE00":
                OtvoriBolovanje();
                break;
            case "INVALIDI2":         // → DO FORM LDINV
            case "LDINV":
                OtvoriInvalidi2();
                break;
            case "PRIPRAVNICI":       // → DO FORM LDPRIP
            case "LDPRIP":
                OtvoriPripravnici();
                break;
            case "POTVRDA_PENZ":      // → DO FORM LDINV2
            case "LDINV2":
                OtvoriPotvrdaPenz();
                break;
            case "POTVRDA_ISPLATA":   // → DO FORM LDINV3
            case "LDINV3":
                OtvoriPotvrdaIsplata();
                break;
            case "POTVRDA_ZARADA":    // → DO FORM LDPOTVR
            case "LDPOTVR":
                OtvoriPotvrdaZarada();
                break;
            case "IZJAVA_DOP":        // → DO FORM LDIZJDOP
            case "LDIZJDOP":
                OtvoriIzjavaDop();
                break;
            case "IZJAVA_RADNOVREME": // → DO FORM LDIZRVRE
            case "LDIZRVRE":
                OtvoriIzjavaRadnoVreme();
                break;
            case "LDOS":
                OtvoriLdOs("ldos.dbf", "OBRAZAC ZSP — UMANJENJE POREZA");
                break;
            case "LDOS1":
                OtvoriLdOs("ldos1.dbf", "OBRAZAC ZSP — UMANJENJE POREZA 1");
                break;
            case "LDIZVJP":
                OtvoriLdIzvjp();
                break;
            case "LDNAKNADE":
                OtvoriLdNaknade();
                break;
            case "EMAIL_LISTICI":
                OtvoriEmailListici();
                break;
            case "LD01":
                OtvoriLd01Evidencija("ld0.dbf", "EVIDENCIJA SVIH OBRAČUNA");
                break;
            case "LD01P":
                OtvoriLd01Evidencija("ld0p.dbf", "EVIDENCIJA SVIH OBRAČUNA PO RADNICIMA");
                break;
            case "RADNICI_OLAKSICE":
                OtvoriRadniciOlaksice();
                break;
            case "ZAHTEV_POVRACAJ":
                OtvoriZahtevPovracaj();
                break;
            case "REGISTAR":
                OtvoriRegistar();
                break;
        }
    }

    private void OtvoriOdRadnici()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new OdRadniciViewModel(folderPath);
        var view = new Algoritam.WPF.Views.Zarade.OdRadniciView { DataContext = vm };
        view.Show();
    }

    private void OtvoriPlatniSpisak()
    {
        var vm = new PlatniSpisakViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.PlatniSpisakView { DataContext = vm };
        view.Show();
    }

    private void OtvoriRekapitulaciju()
    {
        var vm = new RekapitulacijaViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.RekapitulacijaView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriRadnici()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new RadniciViewModel(folderPath);
        var view = new Algoritam.WPF.Views.Zarade.RadniciView { DataContext = vm };
        view.Show();
    }

    private void OtvoriParametre(int tabIndex)
    {
        var vm = new LdParametriViewModel(_appState, tabIndex);
        var view = new Algoritam.WPF.Views.Zarade.LdParametriWindow(vm);
        view.ShowDialog();
    }

    private void OtvoriSpiskovi()
    {
        var vm = new LdSpiskoviViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.LdSpiskoviView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriPodaciZarade()
    {
        var vm = new LdPodaciZaradeViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.LdPodaciZaradeView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriPodaciZaradePojedinacno()
    {
        var vm = new LdPodaciZaradeViewModel(
            _appState,
            "ldpod00.dbf",
            "ZBIRNI PODACI O PLATAMA (LDPOD00)",
            "LDPOD00");
        var view = new Algoritam.WPF.Views.Zarade.LdPodaciZaradeView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriArhivaZarada()
    {
        var vm = new LdArhivaZaradaViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.LdArhivaZaradaView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriSveZarade()
    {
        var vm = new LdSveZaradeViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.LdSveZaradeView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriKnjizenjeZarada()
    {
        var vm = new LdKnjizenjeZaradaViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.LdKnjizenjeZaradaView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriRadnoVreme()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new RadnoVremeViewModel(folderPath);
        var view = new Algoritam.WPF.Views.Zarade.RadnoVremeView { DataContext = vm };
        view.Show();
    }

    private void OtvoriGradovi()
    {
        var firmaFolderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var finRootFolder = string.IsNullOrWhiteSpace(firmaFolderPath)
            ? string.Empty
            : System.IO.Directory.GetParent(firmaFolderPath)?.FullName ?? firmaFolderPath;

        var vm = new GradoviViewModel(finRootFolder, firmaFolderPath);
        var view = new Algoritam.WPF.Views.Zarade.GradoviView { DataContext = vm };
        view.Show();
    }

    private void OtvoriKrediti()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new KreditiViewModel(folderPath);
        var view = new Algoritam.WPF.Views.Zarade.KreditiView { DataContext = vm };
        view.Show();
    }

    private void OtvoriSamodoprinos()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new SamodoprinosViewModel(folderPath);
        var view = new Algoritam.WPF.Views.Zarade.SamodoprinosView { DataContext = vm };
        view.Show();
    }

    private void OtvoriBaznaKonta()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new BaznaKontaViewModel(folderPath);
        var view = new Algoritam.WPF.Views.Zarade.BaznaKontaView { DataContext = vm };
        view.Show();
    }

    private void OtvoriTabelaKonta()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new TabelaKontaViewModel(folderPath);
        var view = new Algoritam.WPF.Views.Zarade.TabelaKontaView { DataContext = vm };
        view.Show();
    }

    private void OtvoriEvidencijaRadnika()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var view = new Views.Zarade.LdRadvrerView(folderPath);
        view.Show();
    }

    private void OtvoriZdravstveneKnjizice()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new ZdravstveneKnjiziceViewModel(folderPath);
        var view = new Algoritam.WPF.Views.Zarade.ZdravstveneKnjiziceView { DataContext = vm };
        view.Show();
    }

    private void OtvoriPrevoz()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new LdPrevozViewModel(folderPath);
        var view = new Algoritam.WPF.Views.Zarade.LdPrevozView { DataContext = vm };
        view.Show();
    }

    private void OtvoriArhivaPrevoza()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new LdPrevozViewModel(folderPath, arhivaRezim: true);
        var view = new Algoritam.WPF.Views.Zarade.LdPrevozView { DataContext = vm };
        view.Show();
    }

    private void OtvoriPutneNaloge()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new LdPutniNaloziViewModel(folderPath);
        var view = new Algoritam.WPF.Views.Zarade.LdPutniNaloziView { DataContext = vm };
        view.Show();
    }

    private void OtvoriPutarine()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new PutarineViewModel(folderPath);
        var view = new Algoritam.WPF.Views.Zarade.PutarineView { DataContext = vm };
        view.Show();
    }

    private void OtvoriPorodilje()
    {
        var vm = new LdPorodiljeViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.LdPorodiljeView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriBolovanje()
    {
        var vm = new LdBolovanjeViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.LdBolovanjeView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriInvalidi2()
    {
        var vm = new LdInvViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.LdInvView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriPripravnici()
    {
        var vm = new LdPripravniciViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.LdPripravniciView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriPotvrdaPenz()
    {
        var vm = LdTekstIzjavaViewModel.CreateInv2(_appState);
        var view = new Algoritam.WPF.Views.Zarade.LdTekstIzjavaView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriPotvrdaIsplata()
    {
        var vm = LdTekstIzjavaViewModel.CreateInv3(_appState);
        var view = new Algoritam.WPF.Views.Zarade.LdTekstIzjavaView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriPotvrdaZarada()
    {
        var vm = new LdPotvrdaZaradaViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.LdPotvrdaZaradaView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriIzjavaDop()
    {
        var vm = LdTekstIzjavaViewModel.CreateIzjavaDop(_appState);
        var view = new Algoritam.WPF.Views.Zarade.LdTekstIzjavaView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriIzjavaRadnoVreme()
    {
        var vm = new LdIzjavaRadnoVremeViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.LdIzjavaRadnoVremeView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriIzvozTabele()
    {
        var vm = new IzvozTabelaViewModel(_appState, _putanjaService);
        var view = new Algoritam.WPF.Views.Zarade.IzvozTabelaView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriFormiranjeTabela()
    {
        var vm = new LdFormiranjeTabelaSafeViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.LdFormiranjeTabelaSafeView { DataContext = vm };
        view.ShowDialog();
    }

    private void OtvoriNalogZaPlacanje()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new NalogZaPlacanjeViewModel(folderPath);
        var view = new Algoritam.WPF.Views.Zarade.NalogZaPlacanjeView { DataContext = vm };
        view.Show();
    }

    private void OtvoriM4()
    {
        var vm = new M4ViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.M4View { DataContext = vm };
        view.Show();
    }

    private void OtvoriPppMeni()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new PppIzborViewModel(folderPath);
        var view = new Algoritam.WPF.Views.Zarade.PppIzborView { DataContext = vm };
        view.ShowDialog();
    }

    /// <summary>
    /// Generički helper — kreira ViewModel(folderPath) i View, povezuje ih i otvara prozor.
    /// </summary>
    private void OtvoriFormu<TVm, TView>()
        where TVm : class
        where TView : System.Windows.Window, new()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = Activator.CreateInstance(typeof(TVm), folderPath) as TVm;
        var view = new TView { DataContext = vm };
        view.Show();
    }

    private void OtvoriVirmane()
    {
        var vm = new VirmaniViewModel(_appState);
        var view = new Views.Zarade.VirmaniView { DataContext = vm };
        vm.ZatvaranjeZahtevano += () => view.Close();
        view.Show();
    }

    private void OtvoriLdOs(string dbfName, string naslov)
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new LdOsViewModel(folderPath, dbfName, naslov);
        var view = new Algoritam.WPF.Views.Zarade.LdOsView { DataContext = vm };
        view.Show();
    }

    private void OtvoriLdIzvjp()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new LdIzvjpViewModel(folderPath);
        var view = new Algoritam.WPF.Views.Zarade.LdIzvjpView { DataContext = vm };
        view.Show();
    }

    private void OtvoriLdNaknade()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new LdNaknadeViewModel(folderPath);
        var view = new Algoritam.WPF.Views.Zarade.LdNaknadeView { DataContext = vm };
        view.Show();
    }

    private void OtvoriEmailListici()
    {
        var vm = new EmailListiciViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.EmailListiciView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.ShowDialog();
    }

    private void OtvoriLd01Evidencija(string dbfName, string naslov)
    {
        var folderPath  = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var firmaNaziv  = _appState.AktivnaFirma?.Naziv      ?? string.Empty;
        var firmaMesto  = _appState.AktivnaFirma?.Mesto       ?? string.Empty;
        var vm = new Ld01EvidencijaViewModel(folderPath, dbfName, naslov, firmaNaziv, firmaMesto);
        var view = new Algoritam.WPF.Views.Zarade.Ld01EvidencijaView { DataContext = vm };
        view.Show();
    }

    private void OtvoriRadniciOlaksice()
    {
        var vm = new RadniciOlaksiceViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.RadniciOlaksiceView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriZahtevPovracaj()
    {
        var vm = new ZahtevPovracajViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.ZahtevPovracajView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriRegistar()
    {
        var vm = new LdRegistarViewModel(_appState);
        var view = new Algoritam.WPF.Views.Zarade.LdRegistarView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static IReadOnlyList<Algoritam.WPF.Views.Zarade.UputstvaIzborWindow.UputstvoOpcija> PronadjiDostupnaUputstva()
    {
        var kandidatiFoldera = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Uputstva"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Uputstva")
        };

        var rezultat = new List<Algoritam.WPF.Views.Zarade.UputstvaIzborWindow.UputstvoOpcija>();
        var vecDodatiNazivi = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kandidatFolder in kandidatiFoldera)
        {
            string folder;
            try
            {
                folder = Path.GetFullPath(kandidatFolder);
            }
            catch
            {
                continue;
            }

            if (!Directory.Exists(folder))
                continue;

            foreach (var ext in new[] { "*.pdf", "*.docx" })
            foreach (var fajl in Directory.GetFiles(folder, ext, SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(fajl);
                if (!vecDodatiNazivi.Add(fileName))
                    continue;

                rezultat.Add(new Algoritam.WPF.Views.Zarade.UputstvaIzborWindow.UputstvoOpcija
                {
                    Naziv = NazivUputstva(fileName),
                    Opis = OpisUputstva(fileName),
                    Putanja = fajl
                });
            }
        }

        return rezultat
            .OrderBy(x => x.Naziv, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static string NazivUputstva(string fileName)
    {
        if (fileName.Equals("FIN_Zarade_Uputstvo_Forme_Redom.pdf", StringComparison.OrdinalIgnoreCase))
            return "Uputstvo po formama (redom)";

        if (fileName.Equals("FIN_Zarade_Uputstvo.pdf", StringComparison.OrdinalIgnoreCase))
            return "Osnovno uputstvo (korak po korak)";

        if (fileName.Equals("FIN_Zarade_Kompletno_Uputstvo_sa_Primerima.pdf", StringComparison.OrdinalIgnoreCase))
            return "Kompletno uputstvo sa primerima (PDF)";

        if (fileName.Equals("FIN_Zarade_Kompletno_Uputstvo_sa_Primerima.docx", StringComparison.OrdinalIgnoreCase))
            return "Kompletno uputstvo sa primerima (Word)";

        return Path.GetFileNameWithoutExtension(fileName).Replace('_', ' ');
    }

    private static string OpisUputstva(string fileName)
    {
        if (fileName.Equals("FIN_Zarade_Uputstvo_Forme_Redom.pdf", StringComparison.OrdinalIgnoreCase))
            return "Svaka forma redom: sta se radi, sta se unosi i koje kontrole proveriti.";

        if (fileName.Equals("FIN_Zarade_Uputstvo.pdf", StringComparison.OrdinalIgnoreCase))
            return "Operativni tok rada kroz mesecni obracun, unos podataka i kontrole.";

        if (fileName.Equals("FIN_Zarade_Kompletno_Uputstvo_sa_Primerima.pdf", StringComparison.OrdinalIgnoreCase))
            return "Detaljno uputstvo sa prakticnim primerima obracuna i najcescim scenarijima.";

        if (fileName.Equals("FIN_Zarade_Kompletno_Uputstvo_sa_Primerima.docx", StringComparison.OrdinalIgnoreCase))
            return "Detaljno uputstvo u Word formatu — editabilno, sa primerima i objasnjenjima.";

        return "Uputstvo za rad.";
    }

    private static void OtvoriPdfUputstvo(string putanja)
    {
        try
        {
            if (!File.Exists(putanja))
            {
                System.Windows.MessageBox.Show(
                    $"Fajl nije pronadjen:\n{putanja}",
                    "Uputstva",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = putanja,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Ne mogu da otvorim PDF.\n\n{ex.Message}",
                "Uputstva",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

}

public record BrzoUlazStavka(string Naziv, string Komanda);
