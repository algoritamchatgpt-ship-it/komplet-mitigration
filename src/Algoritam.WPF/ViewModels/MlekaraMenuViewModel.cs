using Algoritam.Application;
using Algoritam.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Algoritam.WPF.ViewModels;

public partial class MlekaraMenuViewModel : ObservableObject
{
    private readonly AppState _appState;
    private readonly IPutanjaService _putanjaService;

    public ObservableCollection<BrzoUlazStavka> BrzoUlazLista { get; } = [];
    public bool ImaBrzoUlaz => BrzoUlazLista.Count > 0;

    public string FirmaNaziv => _appState.AktivnaFirma?.Naziv ?? "— Nije izabrana firma —";
    public string FirmaMesto => _appState.AktivnaFirma?.Mesto ?? "";
    public string FirmaPib   => _appState.AktivnaFirma?.Pib is { Length: > 0 } p ? $"PIB: {p}" : "";

    public event Action? IzlazTražen;

    public MlekaraMenuViewModel(AppState appState, IPutanjaService putanjaService)
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
        ["MLE_PRIJEM"] = "Prijem i isplata mleka",
        ["MLE_REKAP"]  = "Rekapitulacija po dobavljaču",
        ["HC_PROIZV"]  = "HACCP — dnevni izveštaj proizvodnje",
        ["HC_UTROSAK"] = "HACCP — utrošak sirovina",
        ["HC_MLEKO"]   = "HACCP — prijem mleka (kvalitet)",
        ["HC_SIREVI"]  = "HACCP — dnevnik proizvodnje sireva",
        ["HC_RACUNI"]  = "HACCP — evidencija računa",
        ["HC_KNJPRIJ"] = "HACCP — knjiga prijema",
        ["HC_MAGKART"] = "HACCP — magacinska kartica",
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
            case "MLE_PRIJEM":
                OtvoriDbfPregled(folderPath, "mleko.dbf", "PRIJEM I ISPLATA MLEKA");
                break;
            case "MLE_REKAP":
                OtvoriDbfPregled(folderPath, "mlekor.dbf", "REKAPITULACIJA PO DOBAVLJAČU");
                break;
            case "HC_PROIZV":
                OtvoriDbfPregled(folderPath, "hc01.dbf", "HACCP — DNEVNI IZVEŠTAJ PROIZVODNJE");
                break;
            case "HC_UTROSAK":
                OtvoriDbfPregled(folderPath, "hc02.dbf", "HACCP — UTROŠAK SIROVINA");
                break;
            case "HC_MLEKO":
                OtvoriDbfPregled(folderPath, "hc03.dbf", "HACCP — PRIJEM MLEKA (KVALITET)");
                break;
            case "HC_SIREVI":
                OtvoriDbfPregled(folderPath, "hc04.dbf", "HACCP — DNEVNIK PROIZVODNJE SIREVA");
                break;
            case "HC_RACUNI":
                OtvoriDbfPregled(folderPath, "hc05.dbf", "HACCP — EVIDENCIJA RAČUNA");
                break;
            case "HC_KNJPRIJ":
                OtvoriDbfPregled(folderPath, "hc06.dbf", "HACCP — KNJIGA PRIJEMA");
                break;
            case "HC_MAGKART":
                OtvoriDbfPregled(folderPath, "hc07.dbf", "HACCP — MAGACINSKA KARTICA");
                break;
            default:
                PrikaziUPripremi(komanda);
                break;
        }
    }

    private static void OtvoriDbfPregled(string folderPath, string dbfName, string naslov)
    {
        var vm = new GkDbfPregledViewModel(folderPath, dbfName, naslov, "#F57F17", "#FFF8E1");
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
