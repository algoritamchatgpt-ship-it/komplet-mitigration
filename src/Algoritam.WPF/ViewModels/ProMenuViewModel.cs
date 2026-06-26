using Algoritam.Application;
using Algoritam.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoritam.WPF.ViewModels;

public partial class ProMenuViewModel : ObservableObject
{
    private readonly AppState _appState;
    private readonly IPutanjaService _putanjaService;

    public ObservableCollection<BrzoUlazStavka> BrzoUlazLista { get; } = [];
    public bool ImaBrzoUlaz => BrzoUlazLista.Count > 0;

    public string FirmaNaziv => _appState.AktivnaFirma?.Naziv ?? "— Nije izabrana firma —";
    public string FirmaMesto => _appState.AktivnaFirma?.Mesto ?? "";
    public string FirmaPib   => _appState.AktivnaFirma?.Pib is { Length: > 0 } p ? $"PIB: {p}" : "";

    public event Action? IzlazTražen;

    public ProMenuViewModel(AppState appState, IPutanjaService putanjaService)
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
        ["PRO_NALOZI"] = "Proizvodni nalozi",
        ["PRO_NORMA"]  = "Norme / recepture (BOM)",
        ["PRO_RAD"]    = "Šifarnik rada",
        ["PRO_OPS"]    = "Šifarnik opreme",
        ["PRO_ROB"]    = "Radne cene",
        ["PRO_ZAH"]    = "Zahtevi za materijal",
        ["PRO_CK"]     = "Kalkulacija proizvoda",
        ["PRO_CKNAL"]  = "Kalkulacija po nalogu",
        ["PRO_NORA"]   = "Cenovna varijanta / profitabilnost",
        ["PRO_VMAT"]   = "Verifikacija materijala",
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
            case "PRO_NALOZI":
                OtvoriNaloge(folderPath);
                break;
            case "PRO_NORMA":
                OtvoriDbfPregled(folderPath, "pronorma.dbf", "NORME / RECEPTURE (BOM)", true);
                break;
            case "PRO_RAD":
                OtvoriDbfPregled(folderPath, "prorad.dbf", "ŠIFARNIK RADA", true);
                break;
            case "PRO_OPS":
                OtvoriDbfPregled(folderPath, "proops.dbf", "ŠIFARNIK OPREME", true);
                break;
            case "PRO_ROB":
                OtvoriDbfPregled(folderPath, "prorob.dbf", "RADNE CENE", true);
                break;
            // Zahtevi za materijal — transakcioni dokument (zahtev), ne šifarnik; ostaje read-only
            case "PRO_ZAH":
                OtvoriDbfPregled(folderPath, "prozah.dbf", "ZAHTEVI ZA MATERIJAL");
                break;
            // Kalkulacije — FoxPro-computed (cost-rollup/BOM-explosion), namerno read-only
            case "PRO_CK":
                OtvoriDbfPregled(folderPath, "prock.dbf", "KALKULACIJA PROIZVODA");
                break;
            case "PRO_CKNAL":
                OtvoriDbfPregled(folderPath, "procknal.dbf", "KALKULACIJA PO NALOGU");
                break;
            case "PRO_NORA":
                OtvoriDbfPregled(folderPath, "pronora.dbf", "CENOVNA VARIJANTA / PROFITABILNOST");
                break;
            case "PRO_VMAT":
                OtvoriDbfPregled(folderPath, "provmat.dbf", "VERIFIKACIJA MATERIJALA");
                break;
            default:
                PrikaziUPripremi(komanda);
                break;
        }
    }

    private static void OtvoriNaloge(string folderPath)
    {
        var vm = new TvFaktureViewModel(
            folderPath, "pronal.dbf", "pronalp.dbf", "RNAL",
            "PROIZVODNI NALOZI", "ZAGLAVLJA NALOGA", "STAVKE NALOGA");
        var view = new Views.Veleprodaja.TvFaktureView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriDbfPregled(string folderPath, string dbfName, string naslov, bool dozvoliIzmenu = false)
    {
        var vm = new GkDbfPregledViewModel(folderPath, dbfName, naslov, "#33691E", "#E8F5E9", dozvoliIzmenu);
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
