using Algoritam.Application;
using Algoritam.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoritam.WPF.ViewModels;

public partial class BlagajnaMenuViewModel : ObservableObject
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

    public BlagajnaMenuViewModel(AppState appState, IPutanjaService putanjaService)
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
        ["BL_UNOS"]    = "Unos naloga blagajne",
        ["BL_PREGLED"] = "Pregled blagajne",
        ["BL_DNEVNIK"] = "Dnevnik blagajne",
        ["BL_IZVESTAJ"]= "Izveštaj blagajne",
        ["BL_PARAM"]   = "Parametri blagajne",
        ["KAS_LISTA"]  = "Lista kasa",
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
            case "BL_UNOS":
            case "KASUNOS":
                OtvoriUnosNaloga(folderPath);
                break;
            case "BL_PREGLED":
            case "KASP":
            case "KASPREGLED":
                OtvoriBlagajnu(folderPath, "kas.dbf", "BLAGAJNA — PREGLED");
                break;
            case "BL_DNEVNIK":
            case "KASDNEVNIK":
                OtvoriBlagajnu(folderPath, "kasdnev.dbf", "DNEVNIK BLAGAJNE");
                break;
            case "BL_IZVESTAJ":
            case "KASIZVESTAJ":
                OtvoriIzvestaj(folderPath);
                break;
            case "BL_PARAM":
            case "KASPARAM":
                OtvoriDbfPregled(folderPath, "kasparam.dbf", "PARAMETRI BLAGAJNE");
                break;
            case "KAS_LISTA":
            case "KASLISTA":
                OtvoriKasListu(folderPath, "kaslist.dbf", "LISTA KASA");
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

    private static void OtvoriUnosNaloga(string folderPath)
    {
        var vm   = new BlNalogUnosViewModel(folderPath);
        var view = new Views.Blagajna.BlNalogUnosView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriIzvestaj(string folderPath)
    {
        var vm   = new BlIzvestajViewModel(folderPath);
        var view = new Views.Blagajna.BlIzvestajView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriBlagajnu(string folderPath, string dbfName, string naslov)
    {
        var vm = new BlKasViewModel(folderPath, dbfName, naslov);
        var view = new Views.Blagajna.BlKasView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriDbfPregled(string folderPath, string dbfName, string naslov)
    {
        var vm = new GkDbfPregledViewModel(folderPath, dbfName, naslov, "#1B5E20", "#E8F5E9");
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
