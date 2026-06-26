using Algoritam.Application;
using Algoritam.Application.Services;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

/// <summary>
/// ViewModel za glavni meni Glavne knjige — ekvivalent FoxPro forme NAL.SCX i GK menija.
/// Pokriva: unos naloga, kontni plan, PDV, dnevnik, zaključci, završni bilansi.
/// </summary>
public partial class GlavnaKnjigaMenuViewModel : ObservableObject
{
    private readonly AppState _appState;
    private readonly IPutanjaService _putanjaService;

    [ObservableProperty] private string _trenutniPeriod = "";

    public ObservableCollection<BrzoUlazStavka> BrzoUlazLista { get; } = [];
    public bool ImaBrzoUlaz => BrzoUlazLista.Count > 0;

    public string FirmaNaziv => _appState.AktivnaFirma?.Naziv ?? "— Nije izabrana firma —";
    public string FirmaMesto => _appState.AktivnaFirma?.Mesto ?? "";
    public string FirmaPib   => _appState.AktivnaFirma?.Pib is { Length: > 0 } p ? $"PIB: {p}" : "";

    public event Action? IzlazTražen;

    public GlavnaKnjigaMenuViewModel(AppState appState, IPutanjaService putanjaService)
    {
        _appState = appState;
        _putanjaService = putanjaService;
        UcitajPeriod();
    }

    private void UcitajPeriod()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath;
        if (string.IsNullOrWhiteSpace(folderPath)) return;

        try
        {
            var dbfPath = NadjiDbf(folderPath, "nalparam.dbf");
            if (dbfPath is null) return;
            var zapisi = DbfReader.CitajSveZapise(dbfPath);
            var prvi = zapisi.FirstOrDefault();
            if (prvi is null) return;
            string god = prvi.TryGetValue("GODINA", out var g) && g is string gs ? gs.Trim() : "";
            if (!string.IsNullOrWhiteSpace(god))
                TrenutniPeriod = $"Poslovodna godina: {god}";
        }
        catch { }
    }

    private static string? NadjiDbf(string folderPath, string fileName)
    {
        foreach (var dir in new[] { folderPath,
            Path.Combine(folderPath, "data00"),
            Path.Combine(folderPath, "01") })
        {
            if (!Directory.Exists(dir)) continue;
            var f = Path.Combine(dir, fileName);
            if (File.Exists(f)) return f;
            var ci = Directory.GetFiles(dir, "*.dbf", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(x => Path.GetFileName(x).Equals(fileName, StringComparison.OrdinalIgnoreCase));
            if (ci is not null) return ci;
        }
        return null;
    }

    [RelayCommand]
    private void Izlaz() => IzlazTražen?.Invoke();

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
        ["NAL_UNOS"]          = "Unos naloga",
        ["NAL_PREGLED"]       = "Pregled naloga",
        ["NALDNEV"]           = "Dnevnik knjiženja",
        ["NALBU"]             = "Nalog blagajne",
        ["NALBROJ"]           = "Brojevi naloga",
        ["NALBROJK"]          = "Kartica naloga",
        ["KONPLAN"]           = "Kontni plan",
        ["KONTO"]             = "Konti",
        ["KON1"]              = "Analitika konta 1",
        ["KON2"]              = "Analitika konta 2",
        ["KON3"]              = "Analitika konta 3",
        ["KON4"]              = "Analitika konta 4",
        ["KON5"]              = "Analitika konta 5",
        ["KON6"]              = "Analitika konta 6",
        ["PDV_IZLAZ"]         = "PDV izlaz",
        ["PDV_ULAZ"]          = "PDV ulaz",
        ["PDV_PREGLED"]       = "PDV pregled",
        ["PDV_KNJIZENJE"]     = "Knjiženje PDV",
        ["PDV_POPDV"]         = "Obračun PDV (POPDV)",
        ["NALZAKLJ"]          = "Zaključak naloga",
        ["NALZAK10"]          = "Zaključak GK",
        ["ZNBILANS"]          = "Završni bilans",
        ["NALGK10"]           = "GK-10 analiza",
        ["GKID"]              = "GK identifikatori",
        ["XMLBILANSI"]        = "XML bilansi",
        ["XMLFAKTURA"]        = "XML fakture",
        ["NALOG_PARAM"]       = "Parametri naloga",
        ["KONZAM"]            = "Zamena konta",
        ["OBRBU"]             = "Obrazac bilansa uspeha",
        ["OBRERP"]            = "Podaci za e-PDV/ERP obrazac",
        ["OBRIPD"]            = "Porez na dividende — isplate",
        ["OBRIPD1"]           = "Porez na dividende — dodatno",
        ["OBRKONS"]           = "Konsolidovani obračun poreza",
        ["OBROK"]             = "Obrazac OK",
        ["OBRPK"]             = "Obrazac PK — porez na kapitalnu dobit",
        ["OBRPK1"]            = "Obrazac PK1",
        ["OBRPK2"]            = "Obrazac PK2",
        ["OBRPK3"]            = "Obrazac PK3",
        ["OBRSI"]             = "Statistički izveštaj (SI)",
        ["OBRSU"]             = "Statistički izveštaj (SU)",
        ["OBRSU1"]            = "Statistički izveštaj (SU1)",
        ["OBRSU3"]            = "Statistički izveštaj (SU3)",
    };

    private static string NazivZaKomandu(string komanda)
    {
        if (_komandaNazivi.TryGetValue(komanda, out var naziv)) return naziv;
        return komanda.Replace("_", " ").ToLowerInvariant() is { Length: > 0 } s
            ? char.ToUpper(s[0]) + s[1..]
            : komanda;
    }

    [RelayCommand]
    private void Akcija(string parametar)
    {
        if (string.IsNullOrWhiteSpace(parametar)) return;
        var komanda = parametar.Trim().ToUpperInvariant();
        ZabeleziBrzoUlaz(komanda);

        switch (komanda)
        {
            // ── Unos i pregled naloga ──────────────────────────────────────────
            case "NAL_UNOS":
            case "NAL":
                OtvoriNalozi(unosRezim: true);
                break;
            case "NAL_PREGLED":
            case "NALPREGLED":
                OtvoriNalozi(unosRezim: false);
                break;
            case "NALDNEV":
            case "NALDNEVNIK":
                OtvoriDnevnik();
                break;
            case "NALBU":
            case "NALBBLAG":
                OtvoriNalogBlagajne();
                break;
            case "NALBROJ":
                OtvoriNalogBrojevi();
                break;
            case "NALBROJK":
                OtvoriNalogKartica();
                break;

            // ── Kontni plan ────────────────────────────────────────────────────
            case "KONPLAN":
            case "KONPLAN00":
                OtvoriKontniPlan();
                break;
            case "KONTO":
            case "KONTO00":
                OtvoriKonta();
                break;
            case "KON1": OtvoriKon("kon1.dbf", "ANALITIKA KONTA 1"); break;
            case "KON2": OtvoriKon("kon2.dbf", "ANALITIKA KONTA 2"); break;
            case "KON3": OtvoriKon("kon3.dbf", "ANALITIKA KONTA 3"); break;
            case "KON4": OtvoriKon("kon4.dbf", "ANALITIKA KONTA 4"); break;
            case "KON5": OtvoriKon("kon5.dbf", "ANALITIKA KONTA 5"); break;
            case "KON6": OtvoriKon("kon6.dbf", "ANALITIKA KONTA 6"); break;
            case "KONZAM":
                OtvoriDbfPregled("konzam.dbf", "ZAMENA KONTA");
                break;

            // ── PDV ────────────────────────────────────────────────────────────
            case "PDV_IZLAZ":
            case "PDV00I":
            case "PDVI":
                OtvoriPdvIzlaz();
                break;
            case "PDV_ULAZ":
            case "PDV00U":
            case "PDVU":
                OtvoriPdvUlaz();
                break;
            case "PDV_PREGLED":
            case "PDVSVE":
                OtvoriPdvPregled();
                break;
            case "PDV_KNJIZENJE":
            case "PDVSN":
                OtvoriPdvKnjizenje();
                break;
            case "PDVPER":
                OtvoriDbfPregled("pdvper.dbf", "PDV PERIODI");
                break;
            case "PDV_POPDV":
            case "POPDV":
                OtvoriPopdv();
                break;

            // ── Zaključci ──────────────────────────────────────────────────────
            case "NALZAKLJ":
            case "NALZAKLJ00":
                OtvoriNalogZakljucak();
                break;
            case "NALZAK10":
            case "NALZAK1":
                OtvoriNalogZakljucak10();
                break;

            // ── Analize i izveštaji ────────────────────────────────────────────
            case "NALGK10":
            case "NALGK":
                OtvoriGk10();
                break;
            case "ZNBILANS":
            case "ZNBIL":
                OtvoriZavrsnieBilans();
                break;

            // ── XML ────────────────────────────────────────────────────────────
            case "XMLBILANSI":
            case "XMLBIL":
                OtvoriXmlBilansi();
                break;
            case "XMLFAKTURA":
                OtvoriXmlFakture();
                break;

            // ── Parametri ──────────────────────────────────────────────────────
            case "NALOG_PARAM":
            case "NALPARAM":
                OtvoriParametreNaloga();
                break;
            case "GKID":
            case "GKIDENTIFIKATORI":
                OtvoriGkId();
                break;

            // ── Obrasci (statutarni i statistički izveštaji) ─────────────────────
            case "OBRBU":
                OtvoriDbfPregled("obrbu.dbf", "OBRAZAC BILANSA USPEHA");
                break;
            case "OBRERP":
                OtvoriDbfPregled("obrerp.dbf", "PODACI ZA E-PDV/ERP OBRAZAC");
                break;
            case "OBRIPD":
                OtvoriDbfPregled("obripd.dbf", "POREZ NA DIVIDENDE — ISPLATE");
                break;
            case "OBRIPD1":
                OtvoriDbfPregled("obripd1.dbf", "POREZ NA DIVIDENDE — DODATNO");
                break;
            case "OBRKONS":
                OtvoriDbfPregled("obrkons.dbf", "KONSOLIDOVANI OBRAČUN POREZA");
                break;
            case "OBROK":
                OtvoriDbfPregled("obrok.dbf", "OBRAZAC OK");
                break;
            case "OBRPK":
                OtvoriDbfPregled("obrpk.dbf", "OBRAZAC PK — POREZ NA KAPITALNU DOBIT");
                break;
            case "OBRPK1":
                OtvoriDbfPregled("obrpk1.dbf", "OBRAZAC PK1");
                break;
            case "OBRPK2":
                OtvoriDbfPregled("obrpk2.dbf", "OBRAZAC PK2");
                break;
            case "OBRPK3":
                OtvoriDbfPregled("obrpk3.dbf", "OBRAZAC PK3");
                break;
            case "OBRSI":
                OtvoriDbfPregled("obrsi.dbf", "STATISTIČKI IZVEŠTAJ (SI)");
                break;
            case "OBRSU":
                OtvoriDbfPregled("obrsu.dbf", "STATISTIČKI IZVEŠTAJ (SU)");
                break;
            case "OBRSU1":
                OtvoriDbfPregled("obrsu1.dbf", "STATISTIČKI IZVEŠTAJ (SU1)");
                break;
            case "OBRSU3":
                OtvoriDbfPregled("obrsu3.dbf", "STATISTIČKI IZVEŠTAJ (SU3)");
                break;

            default:
                PrikaziUPripremi(komanda);
                break;
        }
    }

    private void OtvoriNalozi(bool unosRezim)
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        if (unosRezim)
        {
            var vm = new GkNalogUnosViewModel(folderPath);
            var view = new Views.GlavnaKnjiga.GkNalogUnosView { DataContext = vm };
            view.ShowDialog();
        }
        else
        {
            var vm = new GkNaloziViewModel(_appState);
            var view = new Views.GlavnaKnjiga.GkNaloziView { DataContext = vm };
            vm.ZatvaranjeZahtevano += view.Close;
            view.Show();
        }
    }

    private void OtvoriDnevnik()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkDnevnikViewModel(folderPath);
        var view = new Views.GlavnaKnjiga.GkDnevnikView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriNalogBlagajne()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkNalogBlagajneViewModel(folderPath);
        var view = new Views.GlavnaKnjiga.GkNalogBlagajneView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriNalogBrojevi()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkNalBrojViewModel(folderPath);
        var view = new Views.GlavnaKnjiga.GkNalBrojView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriNalogKartica()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkDnevnikViewModel(folderPath, "nalbrojk.dbf", "KARTICA NALOGA");
        var view = new Views.GlavnaKnjiga.GkDnevnikView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriKontniPlan()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkKontniPlanViewModel(folderPath);
        var view = new Views.GlavnaKnjiga.GkKontniPlanView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriKonta()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkKontaViewModel(folderPath);
        var view = new Views.GlavnaKnjiga.GkKontaView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriKon(string dbfName, string naslov)
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkKontaViewModel(folderPath, dbfName, naslov);
        var view = new Views.GlavnaKnjiga.GkKontaView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriDbfPregled(string dbfName, string naslov)
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkDbfPregledViewModel(folderPath, dbfName, naslov);
        var view = new Views.GlavnaKnjiga.GkDbfPregledView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriPdvIzlaz()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkPdvViewModel(folderPath, pdvTip: "I");
        var view = new Views.GlavnaKnjiga.GkPdvView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriPdvUlaz()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkPdvViewModel(folderPath, pdvTip: "U");
        var view = new Views.GlavnaKnjiga.GkPdvView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriPdvPregled()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkPdvViewModel(folderPath, "I",
            dbfNameOverride: "pdvsve.dbf",
            naslovOverride: "PDV — PREGLED SVIH STAVKI");
        var view = new Views.GlavnaKnjiga.GkPdvView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriPdvKnjizenje()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new PdvKnjizenjeViewModel(folderPath);
        var view = new Views.Pdv.PdvKnjizenjeView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriPopdv()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new PdvPopdvViewModel(folderPath);
        var view = new Views.Pdv.PdvPopdvView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriNalogZakljucak()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkZakljucakViewModel(folderPath, "nalzaklj.dbf", "ZAKLJUČAK NALOGA — ZAKLJUČNI LIST");
        var view = new Views.GlavnaKnjiga.GkZakljucakView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriNalogZakljucak10()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkZakljucakViewModel(folderPath, "nalgk10.dbf", "ZAKLJUČAK GK — GK10");
        var view = new Views.GlavnaKnjiga.GkZakljucakView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriGk10()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkZakljucakViewModel(folderPath, "nalgk10.dbf", "GK-10 ANALIZA");
        var view = new Views.GlavnaKnjiga.GkZakljucakView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriZavrsnieBilans()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkZnBilansViewModel(folderPath);
        var view = new Views.GlavnaKnjiga.GkZnBilansView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriXmlBilansi()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkXmlBilansViewModel(folderPath, "XML BILANSI — E-BILANS");
        var view = new Views.GlavnaKnjiga.GkXmlBilansView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriXmlFakture()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkDbfPregledViewModel(folderPath, "xmlpdvp.dbf",
            "XML FAKTURE — PARAMETRI E-FAKTURE");
        var view = new Views.GlavnaKnjiga.GkDbfPregledView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriParametreNaloga()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkDbfPregledViewModel(folderPath, "nalparam.dbf", "PARAMETRI NALOGA");
        var view = new Views.GlavnaKnjiga.GkDbfPregledView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriGkId()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm = new GkIdentViewModel(folderPath);
        var view = new Views.GlavnaKnjiga.GkIdentView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void PrikaziUPripremi(string komanda)
    {
        System.Windows.MessageBox.Show(
            $"Forma '{komanda}' je u pripremi.",
            "U pripremi",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }
}
