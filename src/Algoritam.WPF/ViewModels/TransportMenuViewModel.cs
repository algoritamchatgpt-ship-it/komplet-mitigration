using Algoritam.Application;
using Algoritam.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoritam.WPF.ViewModels;

public partial class TransportMenuViewModel : ObservableObject
{
    private readonly AppState _appState;
    private readonly IPutanjaService _putanjaService;

    public ObservableCollection<BrzoUlazStavka> BrzoUlazLista { get; } = [];
    public bool ImaBrzoUlaz => BrzoUlazLista.Count > 0;

    public string FirmaNaziv => _appState.AktivnaFirma?.Naziv ?? "— Nije izabrana firma —";
    public string FirmaMesto => _appState.AktivnaFirma?.Mesto ?? "";
    public string FirmaPib   => _appState.AktivnaFirma?.Pib is { Length: > 0 } p ? $"PIB: {p}" : "";

    public event Action? IzlazTražen;

    public TransportMenuViewModel(AppState appState, IPutanjaService putanjaService)
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
        ["AAT_GLAVNA"]  = "Prevoznici / agenti (AAT)",
        ["AAT_OTPIS"]   = "Pregled otpisa do dozvoljenog",
        ["AAT_POREZ"]   = "Profitni centri — pregled poreza",
        ["AAT_PROFIT"]  = "Profitni centri — dobit/gubitak",
        ["AAT_SERVIS"]  = "Servisni objekti",
        ["PCE_GLAVNA"]  = "Promet vozila (PCE)",
        ["PCE_PUTANJA"] = "Relacije / putni nalozi",
        ["PCE_ZAGLAVLJE"] = "Zaglavlja promena",
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
            case "AAT_GLAVNA":
                OtvoriDbfPregled(folderPath, "aatv.dbf", "PREVOZNICI / AGENTI (AAT)", true);
                break;
            // Otpis/porez/profit — computed review i finansijski izveštaji, ostaju read-only
            case "AAT_OTPIS":
                OtvoriDbfPregled(folderPath, "aatvotp.dbf", "PREGLED OTPISA DO DOZVOLJENOG");
                break;
            case "AAT_POREZ":
                OtvoriDbfPregled(folderPath, "aatvpor.dbf", "PROFITNI CENTRI — PREGLED POREZA");
                break;
            case "AAT_PROFIT":
                OtvoriDbfPregled(folderPath, "aatvprof.dbf", "PROFITNI CENTRI — DOBIT/GUBITAK");
                break;
            case "AAT_SERVIS":
                OtvoriDbfPregled(folderPath, "aatvserv.dbf", "SERVISNI OBJEKTI", true);
                break;
            case "PCE_GLAVNA":
                OtvoriDbfPregled(folderPath, "pcev.dbf", "PROMET VOZILA (PCE)");
                break;
            case "PCE_PUTANJA":
                OtvoriDbfPregled(folderPath, "pcevput.dbf", "RELACIJE / PUTNI NALOZI");
                break;
            case "PCE_ZAGLAVLJE":
                OtvoriDbfPregled(folderPath, "pcevog.dbf", "ZAGLAVLJA PROMENA");
                break;
            default:
                PrikaziUPripremi(komanda);
                break;
        }
    }

    private static void OtvoriDbfPregled(string folderPath, string dbfName, string naslov, bool dozvoliIzmenu = false)
    {
        var vm = new GkDbfPregledViewModel(folderPath, dbfName, naslov, "#01579B", "#E1F5FE", dozvoliIzmenu);
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
