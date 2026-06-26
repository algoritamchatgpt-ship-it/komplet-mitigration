using Algoritam.Application;
using Algoritam.Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Algoritam.WPF.Views;

namespace Algoritam.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AppState _appState;
    private readonly IPutanjaService _putanjaService;

    public MainViewModel(AppState appState, IPutanjaService putanjaService)
    {
        _appState = appState;
        _putanjaService = putanjaService;
        AktivniModulPogled = null;
    }

    public string KorisnikInfo =>
        $"Korisnik: {_appState.TrenutniKorisnik?.KorisnikIme ?? "-"}" +
        (_appState.JeSupervizor ? " [Supervizor]" : string.Empty);

    public string FirmaInfo => _appState.AktivnaFirma != null
        ? $"Firma: {_appState.AktivnaFirma.Naziv}"
        : "Firma: -";

    public string GodinaInfo => $"Godina: {_appState.AktivnaGodina}";
    public string DatumInfo  => $"Datum: {DateTime.Today:dd.MM.yyyy}";

    public string PozdravnaPortuka =>
        $"Prijavljeni ste kao: {_appState.TrenutniKorisnik?.KorisnikIme}";

    // ── Korisnička prava ────────────────────────────────────────────────────
    public bool MozeGlavnaKnjiga => _appState.TrenutniKorisnik?.PassGk  ?? false;
    public bool MozeAnalitika    => _appState.TrenutniKorisnik?.PassAn  ?? false;
    public bool MozeBlagajna     => _appState.TrenutniKorisnik?.PassBl  ?? false;
    public bool MozeVeleprodaja  => _appState.TrenutniKorisnik?.PassTv  ?? false;
    public bool MozeMaloprodaja  => _appState.TrenutniKorisnik?.PassTm  ?? false;
    public bool MozeKomunal      => _appState.TrenutniKorisnik?.PassUs  ?? false;
    public bool MozeOS           => _appState.TrenutniKorisnik?.PassOs  ?? false;
    public bool MozeLdo          => _appState.TrenutniKorisnik?.PassLd  ?? false;
    public bool MozeOstalo       => _appState.TrenutniKorisnik?.PassOst ?? false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NemaOtvorenogModula))]
    [NotifyPropertyChangedFor(nameof(ImaOtvorenogModula))]
    [NotifyPropertyChangedFor(nameof(AktivniModulNaziv))]
    private object? _aktivniModulPogled;

    public bool NemaOtvorenogModula => AktivniModulPogled == null;
    public bool ImaOtvorenogModula  => AktivniModulPogled != null;

    public string AktivniModulNaziv => AktivniModulPogled switch
    {
        ZaradeMenuViewModel          => "Zarade i druga lična primanja",
        GlavnaKnjigaMenuViewModel    => "Glavna knjiga",
        AnalitikaMenuViewModel       => "Analitika",
        BlagajnaMenuViewModel        => "Blagajna",
        VeleprodajaMenuViewModel     => "Veleprodaja",
        MaloprodajaMenuViewModel     => "Maloprodaja",
        OsnovnaSredstvaMenuViewModel => "Osnovna sredstva",
        KomunalMenuViewModel         => "Komunalne usluge",
        SifarniciMenuViewModel       => "Šifarnici",
        DelMenuViewModel             => "Delovodnik",
        TehMenuViewModel             => "Registar vozila",
        ProMenuViewModel             => "Proizvodnja",
        TransportMenuViewModel       => "Transport / Špedicija",
        MlekaraMenuViewModel         => "Mlekara",
        _ => string.Empty
    };

    // ── Komande za otvaranje modula ────────────────────────────────────────
    [RelayCommand]
    private void OtvoriModul(string modulKod)
    {
        switch (modulKod.ToUpperInvariant())
        {
            case "LD":
                var ldVm = new ZaradeMenuViewModel(_appState, _putanjaService);
                ldVm.IzlazTražen += () => AktivniModulPogled = null;
                AktivniModulPogled = ldVm;
                break;

            case "GK":
                var gkVm = new GlavnaKnjigaMenuViewModel(_appState, _putanjaService);
                gkVm.IzlazTražen += () => AktivniModulPogled = null;
                AktivniModulPogled = gkVm;
                break;

            case "AN":
                var anVm = new AnalitikaMenuViewModel(_appState, _putanjaService);
                anVm.IzlazTražen += () => AktivniModulPogled = null;
                AktivniModulPogled = anVm;
                break;

            case "BL":
                var blVm = new BlagajnaMenuViewModel(_appState, _putanjaService);
                blVm.IzlazTražen += () => AktivniModulPogled = null;
                AktivniModulPogled = blVm;
                break;

            case "TV":
                var tvVm = new VeleprodajaMenuViewModel(_appState, _putanjaService);
                tvVm.IzlazTražen += () => AktivniModulPogled = null;
                AktivniModulPogled = tvVm;
                break;

            case "TM":
                var tmVm = new MaloprodajaMenuViewModel(_appState, _putanjaService);
                tmVm.IzlazTražen += () => AktivniModulPogled = null;
                AktivniModulPogled = tmVm;
                break;

            case "OS":
                var osVm = new OsnovnaSredstvaMenuViewModel(_appState, _putanjaService);
                osVm.IzlazTražen += () => AktivniModulPogled = null;
                AktivniModulPogled = osVm;
                break;

            case "US":
                var usVm = new KomunalMenuViewModel(_appState, _putanjaService);
                usVm.IzlazTražen += () => AktivniModulPogled = null;
                AktivniModulPogled = usVm;
                break;

            case "SI":
                var siVm = new SifarniciMenuViewModel(_appState, _putanjaService);
                siVm.IzlazTražen += () => AktivniModulPogled = null;
                AktivniModulPogled = siVm;
                break;

            case "DEL":
                var delVm = new DelMenuViewModel(_appState, _putanjaService);
                delVm.IzlazTražen += () => AktivniModulPogled = null;
                AktivniModulPogled = delVm;
                break;

            case "TEH":
                var tehVm = new TehMenuViewModel(_appState, _putanjaService);
                tehVm.IzlazTražen += () => AktivniModulPogled = null;
                AktivniModulPogled = tehVm;
                break;

            case "PRO":
                var proVm = new ProMenuViewModel(_appState, _putanjaService);
                proVm.IzlazTražen += () => AktivniModulPogled = null;
                AktivniModulPogled = proVm;
                break;

            case "TRANSPORT":
                var trVm = new TransportMenuViewModel(_appState, _putanjaService);
                trVm.IzlazTražen += () => AktivniModulPogled = null;
                AktivniModulPogled = trVm;
                break;

            case "MLEKARA":
                var mlVm = new MlekaraMenuViewModel(_appState, _putanjaService);
                mlVm.IzlazTražen += () => AktivniModulPogled = null;
                AktivniModulPogled = mlVm;
                break;
        }
    }

    [RelayCommand]
    private void ZatvoriModul()
    {
        if (ImaOtvorenogModula)
            AktivniModulPogled = null;
    }

    public event Action? OdjavaSeTrazena;
    public event Action? VratiseFirmaIzboru;

    [RelayCommand]
    private void Odjavi()
    {
        _appState.Odjavi();
        OdjavaSeTrazena?.Invoke();
    }

    [RelayCommand]
    private void PromeniFirmu()
    {
        AktivniModulPogled = null;
        VratiseFirmaIzboru?.Invoke();
    }

    [RelayCommand]
    private void OtvoriPodatkeOFirmi()
    {
        var vm  = new FirmaPodaciViewModel(_appState);
        var win = new FirmaPodaciWindow(vm);
        win.ShowDialog();
        OnPropertyChanged(nameof(FirmaInfo));
        if (AktivniModulPogled is ZaradeMenuViewModel zaradeVm)
            zaradeVm.OsveziInfoFirme();
    }

    [RelayCommand]
    private void OtvoriGradove()
    {
        var firmaFolderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var finRootFolder = string.IsNullOrWhiteSpace(firmaFolderPath)
            ? string.Empty
            : System.IO.Directory.GetParent(firmaFolderPath)?.FullName ?? firmaFolderPath;

        var vm   = new GradoviViewModel(finRootFolder, firmaFolderPath);
        var view = new Views.Zarade.GradoviView { DataContext = vm };
        view.Show();
    }

    [RelayCommand]
    private void OtvoriPartnere()
    {
        var folderPath = _appState.AktivnaFirma?.FolderPath ?? string.Empty;
        var vm   = new PartneriViewModel(folderPath);
        var view = new Views.Zarade.PartneriView { DataContext = vm };
        view.Show();
    }

    [RelayCommand]
    private void OtvoriFormiranjeLozinki()
    {
        var vm  = new FormiranjeLozinkiViewModel(_putanjaService);
        var win = new Views.Zarade.FormiranjeLozinkiWindow { DataContext = vm };
        win.ShowDialog();
    }
}
