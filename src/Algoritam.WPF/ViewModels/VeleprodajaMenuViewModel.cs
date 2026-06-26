using Algoritam.Application;
using Algoritam.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoritam.WPF.ViewModels;

public partial class VeleprodajaMenuViewModel : ObservableObject
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

    public VeleprodajaMenuViewModel(AppState appState, IPutanjaService putanjaService)
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
        ["FAK_UNOS"]    = "Unos fakture",
        ["FAK_PREGLED"] = "Pregled faktura",
        ["ROB_STANJE"]  = "Stanje robe",
        ["ROB_KARTICA"] = "Kartica robe",
        ["KAL_NOVA"]    = "Nova kalkulacija",
        ["KAL_PREGLED"] = "Pregled kalkulacija",
        ["TV_PARAM"]    = "Parametri veleprodaje",
        ["ROBA"]        = "Šifarnik robe",
        ["PARTNERI"]    = "Šifarnik partnera",
        ["PUTNICI"]     = "Putnici (komercijalisti)",
        ["PUT_BONGR"]   = "Bonus po grupama",
        ["PUT_BONUS"]   = "Kumulativni bonus",
        ["PUT_AKONT"]   = "Akontacije putnika",
        ["PUT_FAKT"]    = "Realizacija faktura putnika",
        ["PUT_SVE"]     = "Pregled svih putnika",
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
            case "FAK_UNOS":
            case "FAK":
            case "FAKUNOS":
                OtvoriFakture(folderPath, "UNOS FAKTURE — VELEPRODAJA");
                break;
            case "FAK_PREGLED":
            case "FAKPREGLED":
                OtvoriFakture(folderPath, "PREGLED FAKTURA — VELEPRODAJA");
                break;
            case "FAK_STAVKE":
            case "FAKP":
                OtvoriFakStavke(folderPath);
                break;
            case "ROB_STANJE":
            case "ROBSTANJE":
                OtvoriRobu(folderPath, "rob.dbf", "STANJE ROBE — VELEPRODAJA");
                break;
            case "ROB_KARTICA":
            case "ROBKARTICA":
                OtvoriRobu(folderPath, "rob.dbf", "KARTICA ROBE — VELEPRODAJA");
                break;
            case "ROBA":
            case "SIFROBA":
                OtvoriRobu(folderPath, "roba.dbf", "ŠIFARNIK ROBE");
                break;
            case "KAL_NOVA":
            case "KAL":
            case "KALNOVA":
                OtvoriKalkulacije(folderPath, "KALKULACIJE — VELEPRODAJA");
                break;
            case "KAL_PREGLED":
            case "KALPREGLED":
                OtvoriKalkulacije(folderPath, "PREGLED KALKULACIJA — VELEPRODAJA");
                break;
            case "TV_PARAM":
            case "TVPARAM":
                OtvoriDbfPregled(folderPath, "tvparam.dbf", "PARAMETRI VELEPRODAJE");
                break;
            case "PARTNERI":
                OtvoriRobu(folderPath, "part.dbf", "ŠIFARNIK PARTNERA");
                break;
            case "PUTNICI":
                OtvoriDbfPregled(folderPath, "putnici.dbf", "PUTNICI — KOMERCIJALISTI", true);
                break;
            case "PUT_BONGR":
                OtvoriDbfPregled(folderPath, "putbongr.dbf", "BONUS PO GRUPAMA", true);
                break;
            // Bonus/akontacije/realizacija/pregled — računato iz fakturisane realizacije, read-only
            case "PUT_BONUS":
                OtvoriDbfPregled(folderPath, "putbonus.dbf", "KUMULATIVNI BONUS PUTNIKA");
                break;
            case "PUT_AKONT":
                OtvoriDbfPregled(folderPath, "putnobra.dbf", "AKONTACIJE PUTNIKA");
                break;
            case "PUT_FAKT":
                OtvoriDbfPregled(folderPath, "putnobrs.dbf", "REALIZACIJA FAKTURA PUTNIKA");
                break;
            case "PUT_SVE":
                OtvoriDbfPregled(folderPath, "putsve.dbf", "PREGLED SVIH PUTNIKA");
                break;
            default:
                PrikaziUPripremi(komanda);
                break;
        }
    }

    private static void OtvoriFakStavke(string folderPath)
    {
        var vm = new TvFakStavkeViewModel(folderPath);
        var view = new Views.Veleprodaja.TvFakStavkeView { DataContext = vm };
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

    private static void OtvoriKalkulacije(string folderPath, string naslov)
    {
        var vm = new TvFaktureViewModel(
            folderPath, "kal.dbf", "kalp.dbf", "BRKAL",
            naslov, "ZAGLAVLJA KALKULACIJA", "STAVKE KALKULACIJE");
        var view = new Views.Veleprodaja.TvFaktureView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriFakture(string folderPath, string naslov)
    {
        var vm = new TvFaktureViewModel(
            folderPath, "fak.dbf", "fakp.dbf", "BRFAK",
            naslov, "ZAGLAVLJA FAKTURA", "STAVKE FAKTURE");
        var view = new Views.Veleprodaja.TvFaktureView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriDbfPregled(string folderPath, string dbfName, string naslov, bool dozvoliIzmenu = false)
    {
        var vm = new GkDbfPregledViewModel(folderPath, dbfName, naslov, "#BF360C", "#FBE9E7", dozvoliIzmenu);
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
