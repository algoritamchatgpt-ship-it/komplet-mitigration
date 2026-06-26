using Algoritam.Application;
using Algoritam.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoritam.WPF.ViewModels;

public partial class KomunalMenuViewModel : ObservableObject
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

    public KomunalMenuViewModel(AppState appState, IPutanjaService putanjaService)
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
        ["US_KORISNICI"]  = "Korisnici usluga",
        ["US_OBRACUN"]    = "Obračun komunalija",
        ["US_UPLATE"]     = "Evidencija uplata",
        ["US_DUGOVANJE"]  = "Dugovanja korisnika",
        ["US_TARIFA"]     = "Tarifnik usluga",
        ["US_PARAM"]      = "Parametri",
        ["US_PREGLED"]    = "Pregled",
        ["VPS_SUD"]       = "Sudski postupci naplate",
        ["VPS_SPORAZUM"]  = "Sporazumi o otplati",
        ["VPS_SPIS"]      = "Spis dužnika",
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
            case "US_KORISNICI":
            case "USKOR":
                OtvoriKorisnike(folderPath);
                break;
            case "US_OBRACUN":
            case "USOBR":
                OtvoriObracun(folderPath);
                break;
            case "US_UPLATE":
            case "USUPLATE":
                OtvoriKorisnike(folderPath, "usuplate.dbf", "EVIDENCIJA UPLATA");
                break;
            case "US_DUGOVANJE":
            case "USDUG":
                OtvoriKorisnike(folderPath, "usdug.dbf", "DUGOVANJA KORISNIKA");
                break;
            case "US_TARIFA":
            case "USTARIFA":
                OtvoriTarifu(folderPath);
                break;
            case "US_PREGLED":
            case "USPREGLED":
                OtvoriKorisnike(folderPath, "us.dbf", "KOMUNALNE USLUGE — PREGLED");
                break;
            case "US_PARAM":
            case "USPARAM":
                OtvoriDbfPregled(folderPath, "usparam.dbf", "PARAMETRI KOMUNALNIH USLUGA");
                break;
            case "VPS_SUD":
                OtvoriDbfPregled(folderPath, "vpsud.dbf", "SUDSKI POSTUPCI NAPLATE");
                break;
            case "VPS_SPORAZUM":
                OtvoriDbfPregled(folderPath, "vpsporaz.dbf", "SPORAZUMI O OTPLATI");
                break;
            case "VPS_SPIS":
                OtvoriDbfPregled(folderPath, "vpspis.dbf", "SPIS DUŽNIKA");
                break;
            default:
                PrikaziUPripremi(komanda);
                break;
        }
    }

    private static void OtvoriTarifu(string folderPath)
    {
        var vm = new UsTarifaViewModel(folderPath);
        var view = new Views.Komunal.UsTarifaView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriObracun(string folderPath)
    {
        var vm = new TvFaktureViewModel(
            folderPath, "uskor.dbf", "usobr.dbf", "SIFRA",
            "OBRAČUN KOMUNALNIH USLUGA", "KORISNICI", "STAVKE OBRAČUNA");
        var view = new Views.Veleprodaja.TvFaktureView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriKorisnike(string folderPath,
        string dbfName = "uskor.dbf", string naslov = "KORISNICI KOMUNALNIH USLUGA")
    {
        var vm = new UsKorisniciViewModel(folderPath, dbfName, naslov);
        var view = new Views.Komunal.UsKorisniciView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriDbfPregled(string folderPath, string dbfName, string naslov)
    {
        var vm = new GkDbfPregledViewModel(folderPath, dbfName, naslov, "#37474F", "#ECEFF1");
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
