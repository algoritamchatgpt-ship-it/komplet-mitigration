using Algoritam.Application;
using Algoritam.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoritam.WPF.ViewModels;

public partial class MaloprodajaMenuViewModel : ObservableObject
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

    public MaloprodajaMenuViewModel(AppState appState, IPutanjaService putanjaService)
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
        ["TM_FAKTURA"]  = "Maloprodajna faktura",
        ["TM_PREGLED"]  = "Pregled maloprodaje",
        ["TM_STANJE"]   = "Stanje robe — maloprodaja",
        ["TM_KASA"]     = "Kasa — prodaja",
        ["TM_KAL"]      = "Kalkulacija maloprodaja",
        ["TM_PARAM"]    = "Parametri maloprodaje",
        ["TM_NIV"]      = "Nivelacija — pregled promena cena",
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
            case "TM_FAKTURA":
            case "TMFAK":
                OtvoriFakture(folderPath, "MALOPRODAJNA FAKTURA");
                break;
            case "TM_PREGLED":
            case "TMPREGLED":
                OtvoriFakture(folderPath, "PREGLED MALOPRODAJE");
                break;
            case "TM_STANJE":
            case "TMROB":
            case "TMSTANJE":
                OtvoriRobu(folderPath, "tmrob.dbf", "STANJE ROBE — MALOPRODAJA");
                break;
            case "TM_KASA":
            case "TMKASA":
                OtvoriKasListu(folderPath, "tmkas.dbf", "KASA — MALOPRODAJA");
                break;
            case "TM_KAL":
            case "TMKAL":
                OtvoriKalkulaciju(folderPath);
                break;
            case "TM_PARAM":
            case "TMPARAM":
                OtvoriDbfPregled(folderPath, "tmparam.dbf", "PARAMETRI MALOPRODAJE");
                break;
            case "TM_NIV":
            case "NIVIZV":
                OtvoriDbfPregled(folderPath, "nivizv0.dbf", "NIVELACIJA — PREGLED PROMENA CENA");
                break;
            default:
                PrikaziUPripremi(komanda);
                break;
        }
    }

    private static void OtvoriKasListu(string folderPath, string dbfName, string naslov)
    {
        var vm = new BlKasListaViewModel(folderPath, dbfName, naslov);
        var view = new Views.Blagajna.BlKasListaView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriKalkulaciju(string folderPath)
    {
        var vm = new TmKalkulacijaViewModel(folderPath);
        var view = new Views.Maloprodaja.TmKalkulacijaView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriRobu(string folderPath, string dbfName, string naslov)
    {
        var vm = new TvRobaViewModel(folderPath, dbfName, naslov);
        var view = new Views.Veleprodaja.TvRobaView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriFakture(string folderPath, string naslov)
    {
        var vm = new TvFaktureViewModel(
            folderPath, "tmfak.dbf", "tmfakp.dbf", "BRFAK",
            naslov, "ZAGLAVLJA FAKTURA", "STAVKE FAKTURE");
        var view = new Views.Veleprodaja.TvFaktureView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriDbfPregled(string folderPath, string dbfName, string naslov)
    {
        var vm = new GkDbfPregledViewModel(folderPath, dbfName, naslov, "#E65100", "#FFF3E0");
        var view = new Views.GlavnaKnjiga.GkDbfPregledView { DataContext = vm };
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
