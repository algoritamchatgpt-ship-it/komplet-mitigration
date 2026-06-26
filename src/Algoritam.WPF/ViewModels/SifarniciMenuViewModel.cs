using Algoritam.Application;
using Algoritam.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoritam.WPF.ViewModels;

public partial class SifarniciMenuViewModel : ObservableObject
{
    private readonly AppState _appState;
    private readonly IPutanjaService _putanjaService;

    public ObservableCollection<BrzoUlazStavka> BrzoUlazLista { get; } = [];
    public bool ImaBrzoUlaz => BrzoUlazLista.Count > 0;

    public string FirmaNaziv => _appState.AktivnaFirma?.Naziv ?? "— Nije izabrana firma —";
    public string FirmaMesto => _appState.AktivnaFirma?.Mesto ?? "";
    public string FirmaPib   => _appState.AktivnaFirma?.Pib is { Length: > 0 } p ? $"PIB: {p}" : "";

    public event Action? IzlazTražen;

    public SifarniciMenuViewModel(AppState appState, IPutanjaService putanjaService)
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
        ["PARTNERI"] = "Partneri",
        ["MESTA"]    = "Mesta i gradovi",
        ["ROBA"]     = "Roba i artikli",
        ["KONPLAN"]  = "Kontni plan",
        ["KONTI"]    = "Konti",
        ["GKID"]     = "GK identifikatori",
        ["AN0_FIZLI"] = "Fizička lica (kupci)",
        ["AN0_MI"]    = "Mesta isporuke",
        ["AN0_MAG"]   = "Šifarnik magacina",
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
            case "PARTNERI":
                OtvoriPartnere(folderPath);
                break;
            case "MESTA":
            case "GRADOVI":
                OtvoriMesta(folderPath);
                break;
            case "ROBA":
                OtvoriRobu(folderPath);
                break;
            case "KONPLAN":
                OtvoriKontniPlan(folderPath);
                break;
            case "KONTI":
                OtvoriKonti(folderPath);
                break;
            case "GKID":
                OtvoriGkId(folderPath);
                break;
            case "AN0_FIZLI":
                OtvoriDbfPregled(folderPath, "an0fizli.dbf", "FIZIČKA LICA (KUPCI)");
                break;
            case "AN0_MI":
                OtvoriDbfPregled(folderPath, "an0mi.dbf", "MESTA ISPORUKE");
                break;
            case "AN0_MAG":
                OtvoriDbfPregled(folderPath, "an0mag.dbf", "ŠIFARNIK MAGACINA");
                break;
            default:
                PrikaziUPripremi(komanda);
                break;
        }
    }

    private static void OtvoriPartnere(string folderPath)
    {
        var vm   = new PartneriViewModel(folderPath);
        var view = new Views.Zarade.PartneriView { DataContext = vm };
        view.Show();
    }

    private static void OtvoriMesta(string folderPath)
    {
        var finRootFolder = string.IsNullOrWhiteSpace(folderPath)
            ? string.Empty
            : System.IO.Directory.GetParent(folderPath)?.FullName ?? folderPath;
        var vm   = new GradoviViewModel(finRootFolder, folderPath);
        var view = new Views.Zarade.GradoviView { DataContext = vm };
        view.Show();
    }

    private static void OtvoriRobu(string folderPath)
    {
        var vm   = new TvRobaViewModel(folderPath, "roba.dbf", "ŠIFARNIK ROBE");
        var view = new Views.Veleprodaja.TvRobaView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriKontniPlan(string folderPath)
    {
        var vm   = new GkKontniPlanViewModel(folderPath);
        var view = new Views.GlavnaKnjiga.GkKontniPlanView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriKonti(string folderPath)
    {
        var vm   = new GkKontaViewModel(folderPath);
        var view = new Views.GlavnaKnjiga.GkKontaView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriGkId(string folderPath)
    {
        var vm   = new GkIdentViewModel(folderPath);
        var view = new Views.GlavnaKnjiga.GkIdentView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriDbfPregled(string folderPath, string dbfName, string naslov)
    {
        var vm   = new GkDbfPregledViewModel(folderPath, dbfName, naslov, "#1565C0", "#E3F2FD", dozvoliIzmenu: true);
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
