using Algoritam.Application;
using Algoritam.Application.Services;
using Algoritam.Infrastructure.Dbf;
using Algoritam.WPF.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class OsnovnaSredstvaMenuViewModel : ObservableObject
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

    public OsnovnaSredstvaMenuViewModel(AppState appState, IPutanjaService putanjaService)
    {
        _appState = appState;
        _putanjaService = putanjaService;
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
        ["OS_KOMPLETAN"]    = "Kompletan OS modul",
        ["OS_DASHBOARD"]    = "OS dashboard",
        ["OS_KARTICE_PREPIS"] = "Kartice OS",
        ["OS_EVIDENCIJA_PREPIS"] = "Evidencija OS",
        ["OS_OBRAZAC_OA"]   = "Obrazac OA",
        ["OS_SIFARNICI"]    = "Sifarnici OS",
        ["OS_ARHIVA"]       = "Arhiva OS",
        ["OS_PRENOS_GODINE"] = "Prenos u novu godinu",
        ["OS_PARTNERI"]     = "Partneri",
        ["OS_MESTA"]        = "Mesta",
        ["OS_IZVOZ"]        = "Izvoz tabela",
        ["OS_PODACI_FIRME"] = "Podaci o firmi",
        ["OS_GRUPE_PREPIS"] = "Grupe amortizacije",
        ["OS_PODACI_PERIOD"] = "Podaci OS",
        ["OS_PREGLEDI_PREPIS"] = "Pregledi OS",
        ["OS_PREGLED"]      = "Pregled OS",
        ["OS_AMORTIZACIJA"] = "Obračun amortizacije",
        ["OS_KARTICA"]      = "Kartica OS",
        ["OS_INVENTAR"]     = "Inventar OS",
        ["OS_UNOS"]         = "Unos OS",
        ["OS_OTPIS"]        = "Otpis OS",
        ["OS_PRENOS"]       = "Prenos OS",
        ["OS_PARAM"]        = "Parametri OS",
        ["OS_GRUPE"]        = "Grupe OS",
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
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;

        switch (komanda)
        {
            case "OS_KOMPLETAN":
            case "OS_MENU":
            case "OSNOVNA_SREDSTVA":
                OtvoriKompletanOsModul();
                break;
            case "OS_DASHBOARD":
                PokreniPrepisaniOs(vm => vm.OtvoriDashboardCommand.Execute(null));
                break;
            case "OS_KARTICE_PREPIS":
                PokreniPrepisaniOs(vm => vm.OtvoriKarticeCommand.Execute(null));
                break;
            case "OS_EVIDENCIJA_PREPIS":
                PokreniPrepisaniOs(vm => vm.OtvoriEvidencijuCommand.Execute(null));
                break;
            case "OS_OBRAZAC_OA":
                PokreniPrepisaniOs(vm => vm.OtvoriStampaCommand.Execute(null));
                break;
            case "OS_SIFARNICI":
                PokreniPrepisaniOs(vm => vm.OtvoriSifarNiciCommand.Execute(null));
                break;
            case "OS_ARHIVA":
                PokreniPrepisaniOs(vm => vm.OtvoriArhivuCommand.Execute(null));
                break;
            case "OS_PRENOS_GODINE":
                PokreniPrepisaniOs(vm => vm.OtvoriPrenosuCommand.Execute(null));
                break;
            case "OS_PARTNERI":
                PokreniPrepisaniOs(vm => vm.OtvoriPartnereCommand.Execute(null));
                break;
            case "OS_MESTA":
                PokreniPrepisaniOs(vm => vm.OtvoriMestaCommand.Execute(null));
                break;
            case "OS_IZVOZ":
                PokreniPrepisaniOs(vm => vm.OtvoriIzvozCommand.Execute(null));
                break;
            case "OS_PODACI_FIRME":
                PokreniPrepisaniOs(vm => vm.OtvoriPodatkeOFirmiCommand.Execute(null));
                break;
            case "OS_GRUPE_PREPIS":
                PokreniPrepisaniOs(vm => vm.OtvoriGrupeAmortizacijeCommand.Execute(null));
                break;
            case "OS_PODACI_PERIOD":
                PokreniPrepisaniOs(vm => vm.OtvoriPodatkeOsCommand.Execute(null));
                break;
            case "OS_PREGLEDI_PREPIS":
                PokreniPrepisaniOs(vm => vm.OtvoriPregledeCommand.Execute(null));
                break;
            case "OS_PREGLED":
            case "OSP":
            case "OSPREGLED":
                OtvoriOsPregled(folderPath, "os.dbf", "OSNOVNA SREDSTVA — PREGLED");
                break;
            case "OS_KARTICA":
            case "OSKARTICA":
                OtvoriOsPregled(folderPath, "oskartica.dbf", "KARTICA OSNOVNOG SREDSTVA");
                break;
            case "OS_INVENTAR":
            case "OSINVENTAR":
                OtvoriOsPregled(folderPath, "osinv.dbf", "INVENTAR OSNOVNIH SREDSTAVA");
                break;
            case "OS_AMORTIZACIJA":
            case "OSAMORT":
                OtvoriAmortizaciju(folderPath);
                break;
            case "OS_GRUPE":
            case "OSGRUPE":
                OtvoriOsPregled(folderPath, "osgrupe.dbf", "GRUPE OSNOVNIH SREDSTAVA");
                break;
            case "OS_PARAM":
            case "OSPARAM":
                OtvoriDbfPregled(folderPath, "osparam.dbf", "PARAMETRI OSNOVNIH SREDSTAVA");
                break;
            case "OS_UNOS":
            case "OSUNOS":
                OtvoriOsUnos(folderPath);
                break;
            case "OS_OTPIS":
            case "OSOTPIS":
                OtvoriOsOtpis(folderPath);
                break;
            case "OS_PRENOS":
            case "OSPRENOS":
                OtvoriOsPrenosSredstva(folderPath);
                break;
            default:
                PrikaziUPripremi(komanda);
                break;
        }
    }

    private void OtvoriKompletanOsModul()
    {
        var vm = KreirajPrepisaniOsMenuViewModel();
        if (vm is null) return;

        var view = new OsnovnaSredstva.Views.OsMenuWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        vm.OdjavaSeTrazena += view.Close;
        vm.VratiseFirmaIzboru += view.Close;
        view.Show();
    }

    private void PokreniPrepisaniOs(Action<OsnovnaSredstva.ViewModels.OsMenuViewModel> akcija)
    {
        var vm = KreirajPrepisaniOsMenuViewModel();
        if (vm is null) return;

        akcija(vm);
    }

    private OsnovnaSredstva.ViewModels.OsMenuViewModel? KreirajPrepisaniOsMenuViewModel()
    {
        if (_appState.AktivnaFirma is null)
        {
            System.Windows.MessageBox.Show(
                "Nema aktivne firme.",
                "Osnovna sredstva",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return null;
        }

        var osAppState = OsnovnaSredstvaIntegration.CreateAppState(_appState);
        var osPutanjaService = OsnovnaSredstvaIntegration.CreatePutanjaService(_putanjaService);
        return new OsnovnaSredstva.ViewModels.OsMenuViewModel(osAppState, osPutanjaService);
    }

    private static void OtvoriOsPregled(string folderPath, string dbfName, string naslov)
    {
        var vm = new OsPregledViewModel(folderPath, dbfName, naslov);
        var view = new Views.OsnovnaSredstva.OsPregledView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriAmortizaciju(string folderPath)
    {
        var vm = new OsAmortizacijaViewModel(folderPath);
        var view = new Views.OsnovnaSredstva.OsAmortizacijaView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriDbfPregled(string folderPath, string dbfName, string naslov)
    {
        var vm = new GkDbfPregledViewModel(folderPath, dbfName, naslov, "#4527A0", "#EDE7F6");
        var view = new Views.GlavnaKnjiga.GkDbfPregledView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriOsUnos(string folderPath)
    {
        var vm = new OsUnosViewModel(folderPath);
        var view = new Views.OsnovnaSredstva.OsUnosView { DataContext = vm };
        view.ShowDialog();
    }

    private static void OtvoriOsOtpis(string folderPath)
    {
        var vm = new OsPregledViewModel(folderPath, "os.dbf",
            "OTPIS OSNOVNIH SREDSTAVA", OsRezim.Otpis);
        var view = new Views.OsnovnaSredstva.OsPregledView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriOsPrenosSredstva(string folderPath)
    {
        var vm = new OsPregledViewModel(folderPath, "os.dbf",
            "PRENOS OSNOVNIH SREDSTAVA", OsRezim.Prenos);
        var view = new Views.OsnovnaSredstva.OsPregledView { DataContext = vm };
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
