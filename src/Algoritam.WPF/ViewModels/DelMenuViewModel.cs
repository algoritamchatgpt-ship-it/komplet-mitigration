using Algoritam.Application;
using Algoritam.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoritam.WPF.ViewModels;

public partial class DelMenuViewModel : ObservableObject
{
    private readonly AppState _appState;
    private readonly IPutanjaService _putanjaService;

    public ObservableCollection<BrzoUlazStavka> BrzoUlazLista { get; } = [];
    public bool ImaBrzoUlaz => BrzoUlazLista.Count > 0;

    public string FirmaNaziv => _appState.AktivnaFirma?.Naziv ?? "— Nije izabrana firma —";
    public string FirmaMesto => _appState.AktivnaFirma?.Mesto ?? "";
    public string FirmaPib   => _appState.AktivnaFirma?.Pib is { Length: > 0 } p ? $"PIB: {p}" : "";

    public event Action? IzlazTražen;

    public DelMenuViewModel(AppState appState, IPutanjaService putanjaService)
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
        ["DELOVODNIK"] = "Delovodnik",
        ["DEL_VRSTA"]  = "Vrsta delovodnika",
        ["DEL_DOK"]    = "Način dostave",
        ["DEL_LOKAC"]  = "Lokacije",
        ["DEL_ORGAN"]  = "Organizacione jedinice",
        ["DEL_PRIM"]   = "Primaoci",
        ["DEL_STAT"]   = "Statusi",
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
            case "DELOVODNIK":
                OtvoriDelovodnik(folderPath);
                break;
            case "DEL_VRSTA":
                OtvoriDbfPregled(folderPath, "delvrsta.dbf", "VRSTA DELOVODNIKA");
                break;
            case "DEL_DOK":
                OtvoriDbfPregled(folderPath, "deldok.dbf", "NAČIN DOSTAVE DOKUMENTA");
                break;
            case "DEL_LOKAC":
                OtvoriDbfPregled(folderPath, "dellokac.dbf", "LOKACIJE");
                break;
            case "DEL_ORGAN":
                OtvoriDbfPregled(folderPath, "delorgan.dbf", "ORGANIZACIONE JEDINICE");
                break;
            case "DEL_PRIM":
                OtvoriDbfPregled(folderPath, "delprim.dbf", "PRIMAOCI");
                break;
            case "DEL_STAT":
                OtvoriDbfPregled(folderPath, "delstat.dbf", "STATUSI DOKUMENATA");
                break;
            default:
                PrikaziUPripremi(komanda);
                break;
        }
    }

    private static void OtvoriDelovodnik(string folderPath)
    {
        var vm = new DelovodnikViewModel(folderPath);
        var view = new Views.Delovodnik.DelovodnikView { DataContext = vm };
        vm.ZatvaranjeZahtevano += view.Close;
        view.Show();
    }

    private static void OtvoriDbfPregled(string folderPath, string dbfName, string naslov)
    {
        var vm = new GkDbfPregledViewModel(folderPath, dbfName, naslov, "#5D4037", "#EFEBE9", dozvoliIzmenu: true);
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
