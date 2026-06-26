using Algoritam.Application;
using Algoritam.Application.Services;
using Algoritam.Infrastructure.Dbf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace Algoritam.WPF.ViewModels;

public partial class AnalitikaMenuViewModel : ObservableObject
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

    public AnalitikaMenuViewModel(AppState appState, IPutanjaService putanjaService)
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
        ["AN_KARTICA"]    = "Kartica partnera",
        ["AN_PROMET"]     = "Promet po kontu",
        ["AN_MESECNA"]    = "Kartica po mesecima",
        ["AN_BRUTO"]      = "Bruto bilans analitike",
        ["AN_PREGLED"]    = "Pregled analitike",
        ["AN_SALDO"]      = "Saldo analitike",
        ["AN_STARENJE"]   = "Starenje potraživanja",
        ["AN_IOS"]        = "IOS — Izjava o stanju",
        ["ANA_PREGLED"]   = "Analitika pregled",
        ["AN_PARAM"]      = "Parametri analitike",
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
            case "AN_KARTICA":
            case "ANAKARTICA":
                OtvoriKarticuPartnera(folderPath);
                break;
            case "AN_PROMET":
            case "ANAPROMET":
                OtvoriKarticuPartnera(folderPath, "anpromet.dbf", "PROMET PO KONTU");
                break;
            case "AN_MESECNA":
            case "ANMES":
            case "ANALKPOMES":
                OtvoriMesecnuKarticu(folderPath);
                break;
            case "AN_BRUTO":
            case "ANBRUTO":
                OtvoriBruto(folderPath);
                break;
            case "AN_PREGLED":
            case "ANAPREGLED":
            case "ANA_PREGLED":
                OtvoriKarticuPartnera(folderPath, "ana.dbf", "ANALITIKA — PREGLED");
                break;
            case "AN_SALDO":
            case "ANSALDO":
                OtvoriSaldo(folderPath);
                break;
            case "AN_STARENJE":
            case "ANSTARENJE":
                OtvoriStarenje(folderPath);
                break;
            case "AN_IOS":
            case "ANIOS":
                OtvoriIos(folderPath);
                break;
            case "AN_PARAM":
            case "ANPARAM":
                OtvoriDbfPregled(folderPath, "anparam.dbf", "PARAMETRI ANALITIKE");
                break;
            default:
                PrikaziUPripremi(komanda);
                break;
        }
    }

    private static void OtvoriIos(string folderPath)
    {
        var vm = new AnIosViewModel(folderPath);
        var view = new Views.Analitika.AnIosView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriStarenje(string folderPath)
    {
        var vm = new AnStarenjeViewModel(folderPath);
        var view = new Views.Analitika.AnStarenjeView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriSaldo(string folderPath)
    {
        var vm = new AnSaldoViewModel(folderPath);
        var view = new Views.Analitika.AnSaldoView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriBruto(string folderPath)
    {
        var vm = new AnBrutoViewModel(folderPath);
        var view = new Views.Analitika.AnBrutoView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriMesecnuKarticu(string folderPath)
    {
        var vm = new AnMesecnaKarticaViewModel(folderPath);
        var view = new Views.Analitika.AnMesecnaKarticaView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriKarticuPartnera(string folderPath,
        string dbfName = "ana.dbf", string naslov = "KARTICA PARTNERA — ANALITIKA")
    {
        var vm = new AnKarticaViewModel(folderPath, dbfName, naslov);
        var view = new Views.Analitika.AnKarticaView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private void OtvoriDbfPregled(string folderPath, string dbfName, string naslov)
    {
        var vm = new GkDbfPregledViewModel(folderPath, dbfName, naslov, "#006064", "#E0F2F1");
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
