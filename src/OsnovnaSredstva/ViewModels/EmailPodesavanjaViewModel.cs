using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MailKit.Net.Smtp;
using MailKit.Security;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;

namespace OsnovnaSredstva.ViewModels;

public partial class EmailPodesavanjaViewModel : ObservableObject
{
    private readonly EmailPodesavanjaService _service;

    [ObservableProperty] private string _smtpServer = string.Empty;
    [ObservableProperty] private int _smtpPort = 587;
    [ObservableProperty] private bool _koristiTls = true;
    [ObservableProperty] private string _korisnickoIme = string.Empty;
    public string Lozinka { get; set; } = string.Empty;
    [ObservableProperty] private string _posiljaoceEmail = string.Empty;
    [ObservableProperty] private string _posiljaoceIme = string.Empty;
    [ObservableProperty] private string _poruka = string.Empty;
    [ObservableProperty] private bool _radi;

    public Action? ZatvoriAction { get; set; }

    public EmailPodesavanjaViewModel(EmailPodesavanjaService service)
    {
        _service = service;
        var p = service.DajPodesavanja();
        SmtpServer = p.SmtpServer;
        SmtpPort = p.SmtpPort;
        KoristiTls = p.KoristiTls;
        KorisnickoIme = p.KorisnickoIme;
        Lozinka = p.Lozinka;
        PosiljaoceEmail = p.PosiljaoceEmail;
        PosiljaoceIme = p.PosiljaoceIme;
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        if (string.IsNullOrWhiteSpace(SmtpServer))
        {
            Poruka = "SMTP server je obavezan.";
            return;
        }
        if (string.IsNullOrWhiteSpace(PosiljaoceEmail))
        {
            Poruka = "E-mail posiljaoca je obavezan.";
            return;
        }

        var pod = new EmailPodesavanja
        {
            SmtpServer = SmtpServer.Trim(),
            SmtpPort = SmtpPort,
            KoristiTls = KoristiTls,
            KorisnickoIme = KorisnickoIme.Trim(),
            Lozinka = Lozinka,
            PosiljaoceEmail = PosiljaoceEmail.Trim(),
            PosiljaoceIme = PosiljaoceIme.Trim()
        };

        if (_service.Snimi(pod))
        {
            Poruka = "Podesavanja su sacuvana.";
            ZatvoriAction?.Invoke();
        }
        else
        {
            Poruka = "Greska pri cuvanju podesavanja.";
        }
    }

    [RelayCommand(CanExecute = nameof(MozeTestirati))]
    private async Task TestirajAsync()
    {
        if (string.IsNullOrWhiteSpace(SmtpServer))
        {
            Poruka = "Unesite SMTP server pre testiranja.";
            return;
        }

        Radi = true;
        Poruka = "Testiranje veze...";
        try
        {
            var secOpt = KoristiTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;
            using var client = new SmtpClient();
            await client.ConnectAsync(SmtpServer.Trim(), SmtpPort, secOpt);
            if (!string.IsNullOrWhiteSpace(KorisnickoIme))
                await client.AuthenticateAsync(KorisnickoIme.Trim(), Lozinka);
            await client.DisconnectAsync(true);
            Poruka = "Veza uspesna! SMTP server je dostupan.";
        }
        catch (Exception ex)
        {
            Poruka = $"Greska: {ex.Message}";
        }
        finally { Radi = false; }
    }

    private bool MozeTestirati() => !Radi;

    partial void OnRadiChanged(bool value) => TestirajCommand.NotifyCanExecuteChanged();
}
