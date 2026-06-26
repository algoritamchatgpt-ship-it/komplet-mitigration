using Algoritam.Application;
using Algoritam.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoritam.WPF.ViewModels;

public partial class TehMenuViewModel : ObservableObject
{
    private readonly AppState _appState;
    private readonly IPutanjaService _putanjaService;

    public ObservableCollection<BrzoUlazStavka> BrzoUlazLista { get; } = [];
    public bool ImaBrzoUlaz => BrzoUlazLista.Count > 0;

    public string FirmaNaziv => _appState.AktivnaFirma?.Naziv ?? "— Nije izabrana firma —";
    public string FirmaMesto => _appState.AktivnaFirma?.Mesto ?? "";
    public string FirmaPib   => _appState.AktivnaFirma?.Pib is { Length: > 0 } p ? $"PIB: {p}" : "";

    public event Action? IzlazTražen;

    public TehMenuViewModel(AppState appState, IPutanjaService putanjaService)
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
        ["TEH_REGISTAR"] = "Registracija vozila",
        ["TEH_BOJA"]     = "Boja vozila",
        ["TEH_GORIVO"]   = "Gorivo",
        ["TEH_TIP"]      = "Tip / model vozila",
        ["TEH_VRSTA"]    = "Vrsta vozila",
        ["TEH_ZEMLJA"]   = "Zemlja porekla",
        ["TEH_NOS"]      = "Nosivost",
        ["TEH_ISPRAVNO"] = "Ispravnost vozila",
        ["TEH_VRPREG"]   = "Vrsta tehničkog pregleda",
        ["TEH_RAZLOG"]   = "Razlog odjave/promene",
        ["TEH_GRAD"]     = "Gradovi",
        ["TEH_OPST"]     = "Opštine",
        ["TEH_POSTA"]    = "Poštanski brojevi",
        ["TEH_GODEVID"]  = "Godišnja evidencija",
        ["TEH_NAPOMENE"] = "Napomene uz vozilo",
        ["TEH_VIRMAN"]   = "Virman nalozi",
        ["TEH_VIRMSTAV"] = "Stavke virmana",
        ["TEH_VIRMSER"]  = "Serije virmana",
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
            // Registar i prosti šifarnici — genuine master data, CRUD dozvoljen
            case "TEH_REGISTAR":
                OtvoriDbfPregled(folderPath, "otg.dbf", "REGISTRACIJA VOZILA", true);
                break;
            case "TEH_BOJA":
                OtvoriDbfPregled(folderPath, "otosnbo.dbf", "ŠIFARNIK BOJA VOZILA", true);
                break;
            case "TEH_GORIVO":
                OtvoriDbfPregled(folderPath, "otgorivo.dbf", "ŠIFARNIK GORIVA", true);
                break;
            case "TEH_TIP":
                OtvoriDbfPregled(folderPath, "ottip.dbf", "TIP / MODEL VOZILA", true);
                break;
            case "TEH_VRSTA":
                OtvoriDbfPregled(folderPath, "otvrsta.dbf", "VRSTA VOZILA", true);
                break;
            case "TEH_ZEMLJA":
                OtvoriDbfPregled(folderPath, "otzemlj.dbf", "ZEMLJA POREKLA VOZILA", true);
                break;
            case "TEH_NOS":
                OtvoriDbfPregled(folderPath, "otnos.dbf", "NOSIVOST VOZILA", true);
                break;
            case "TEH_ISPRAVNO":
                OtvoriDbfPregled(folderPath, "otisprav.dbf", "ISPRAVNOST VOZILA", true);
                break;
            case "TEH_VRPREG":
                OtvoriDbfPregled(folderPath, "otvrpreg.dbf", "VRSTA TEHNIČKOG PREGLEDA", true);
                break;
            case "TEH_RAZLOG":
                OtvoriDbfPregled(folderPath, "otrazlog.dbf", "RAZLOZI ODJAVE/PROMENE", true);
                break;
            case "TEH_GRAD":
                OtvoriDbfPregled(folderPath, "otgrad.dbf", "ŠIFARNIK GRADOVA", true);
                break;
            case "TEH_OPST":
                OtvoriDbfPregled(folderPath, "otopst.dbf", "ŠIFARNIK OPŠTINA", true);
                break;
            case "TEH_POSTA":
                OtvoriDbfPregled(folderPath, "otposta.dbf", "POŠTANSKI BROJEVI", true);
                break;
            // Godišnja evidencija (transakcioni log po vozilu/godini), napomene (detail tabela
            // bez master-detail konteksta) i virman nalozi (finansijski dokumenti) — ostaju read-only
            case "TEH_GODEVID":
                OtvoriDbfPregled(folderPath, "ot0s.dbf", "GODIŠNJA EVIDENCIJA VOZILA");
                break;
            case "TEH_NAPOMENE":
                OtvoriDbfPregled(folderPath, "otgtxt.dbf", "NAPOMENE UZ VOZILO");
                break;
            case "TEH_VIRMAN":
                OtvoriDbfPregled(folderPath, "otvirm.dbf", "VIRMAN NALOZI ZA PLAĆANJE");
                break;
            case "TEH_VIRMSTAV":
                OtvoriDbfPregled(folderPath, "otvirm0.dbf", "STAVKE VIRMAN NALOGA");
                break;
            case "TEH_VIRMSER":
                OtvoriDbfPregled(folderPath, "otvirs.dbf", "SERIJE VIRMAN NALOGA");
                break;
            default:
                PrikaziUPripremi(komanda);
                break;
        }
    }

    private static void OtvoriDbfPregled(string folderPath, string dbfName, string naslov, bool dozvoliIzmenu = false)
    {
        var vm = new GkDbfPregledViewModel(folderPath, dbfName, naslov, "#00695C", "#E0F2F1", dozvoliIzmenu);
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
