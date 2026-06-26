using Algoritam.Application;
using Algoritam.Application.Services;
using Algoritam.Domain.Entities;
using Algoritam.Infrastructure.Dbf;
using Algoritam.Infrastructure.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows;

namespace Algoritam.WPF.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private const string LokalniModMarkerFajl = ".algoritam-local-no-password";

    private readonly IAuthService _authService;
    private readonly ILoginSessionService _loginSessionService;
    private readonly IPutanjaService _putanjaService;
    private readonly AppState _appState;
    private readonly bool _lokalniModBezLozinke;

    public LoginViewModel(
        IAuthService authService,
        ILoginSessionService loginSessionService,
        IPutanjaService putanjaService,
        AppState appState)
    {
        _authService = authService;
        _loginSessionService = loginSessionService;
        _putanjaService = putanjaService;
        _appState = appState;

        _lokalniModBezLozinke = JeLokalniModBezLozinke();
        if (_lokalniModBezLozinke)
            KorisnikIme = "admin";
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrijavaCommand))]
    private string _korisnikIme = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrijavaCommand))]
    private string _lozinka = string.Empty;

    [ObservableProperty]
    private string _poruka = string.Empty;

    [ObservableProperty]
    private bool _ucitava;

    public bool LozinkaNijeObavezna => _lokalniModBezLozinke;

    public event Action? PrijavaUspela;

    private bool MozePrijava() =>
        !string.IsNullOrWhiteSpace(KorisnikIme) &&
        (_lokalniModBezLozinke || !string.IsNullOrWhiteSpace(Lozinka)) &&
        !Ucitava;

    [RelayCommand(CanExecute = nameof(MozePrijava))]
    private async Task PrijavaAsync()
    {
        Ucitava = true;
        Poruka = string.Empty;

        try
        {
            if (_lokalniModBezLozinke && string.IsNullOrWhiteSpace(Lozinka))
            {
                _appState.Prijavi(KreirajLokalnogKorisnika(KorisnikIme.Trim()));
                PrijavaUspela?.Invoke();
                return;
            }

            var korisnik = await _authService.PrijavaAsync(KorisnikIme.Trim(), Lozinka);

            if (korisnik != null)
            {
                if (!_loginSessionService.TryAcquireSession(korisnik.KorisnikIme, out var lockHandle, out var lockMessage))
                {
                    Poruka = string.IsNullOrWhiteSpace(lockMessage)
                        ? $"Korisnik '{korisnik.KorisnikIme}' je vec prijavljen."
                        : lockMessage;
                    Lozinka = string.Empty;
                    return;
                }

                _appState.PostaviSesijaLock(lockHandle);
                _appState.Prijavi(korisnik);
                PrijavaUspela?.Invoke();
            }
            else
            {
                Poruka = "Pogresan korisnik ili lozinka.";
                Lozinka = string.Empty;
            }
        }
        finally
        {
            Ucitava = false;
        }
    }

    [RelayCommand]
    private void PrikaziDijagnostiku()
    {
        try
        {
            var finPutanja = _putanjaService.DajFinPutanju();
            if (string.IsNullOrEmpty(finPutanja))
            {
                MessageBox.Show("FIN putanja nije postavljena.", "Dijagnostika",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var lozinkeFajl = FinDbfAuthService.PronadjiLozinkeFajl(finPutanja);

            if (lozinkeFajl == null)
            {
                var listaFajlova = "";
                try
                {
                    var data00 = Path.Combine(finPutanja, "data00");
                    if (Directory.Exists(data00))
                        listaFajlova = string.Join("\n", Directory.GetFiles(data00, "*.DBF")
                            .Select(Path.GetFileName));
                    else
                        listaFajlova = $"Folder data00 ne postoji u: {finPutanja}";
                }
                catch (Exception ex2)
                {
                    listaFajlova = ex2.Message;
                }

                MessageBox.Show(
                    $"LOZINKE.DBF nije pronađen.\n\nRoot putanja:\n{finPutanja}\n\nDBF fajlovi u data00:\n{listaFajlova}",
                    "Dijagnostika", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var izvestaj = DbfDijagnostika.AnalizirajFajl(lozinkeFajl);
            MessageBox.Show(izvestaj, $"Dijagnostika - {Path.GetFileName(lozinkeFajl)}",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greska pri dijagnostici:\n\n{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
                "Greska", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool JeLokalniModBezLozinke()
    {
        var finPutanja = _putanjaService.DajFinPutanju();
        if (string.IsNullOrWhiteSpace(finPutanja))
            return false;

        var marker = Path.Combine(finPutanja, LokalniModMarkerFajl);
        if (!File.Exists(marker))
            return false;

        // Ako postoji LOZINKE.DBF, lokalni no-password mod nije dozvoljen.
        return FinDbfAuthService.PronadjiLozinkeFajl(finPutanja) is null;
    }

    private static Korisnik KreirajLokalnogKorisnika(string korisnickoIme)
    {
        var ime = string.IsNullOrWhiteSpace(korisnickoIme) ? "admin" : korisnickoIme;

        return new Korisnik
        {
            Pas = "0",
            KorisnikIme = ime,
            KorisnikIme2 = "Lokalni korisnik",
            Lozinka = string.Empty,
            Aktivan = true,
            JeSupervizor = true,
            PravaNivo = 1,
            PassGk = true,
            PassAn = true,
            PassBl = true,
            PassTv = true,
            PassTm = true,
            PassUs = true,
            PassLd = true,
            PassOst = true,
            PassPrn = true,
            PassPro = true,
            PassOs = true,
            PassProf = true,
            PassDel = true
        };
    }
}
